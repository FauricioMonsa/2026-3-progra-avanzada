(() => {
    "use strict";

    const root = document.querySelector(".forecasts-chart");
    if (!root) {
        return;
    }

    const sportFilter = document.getElementById("sport-filter");
    const teamFilter = document.getElementById("team-filter");
    const matchFilter = document.getElementById("match-filter");
    const dateFilter = document.getElementById("date-filter");
    const chartMessage = document.getElementById("chart-message");
    const realtimeMessage = document.getElementById("realtime-message");
    const chartDefinitions = [
        { resultType: 1, canvasId: "home-wins-chart", label: "Home wins", color: "#d95d39" },
        { resultType: 0, canvasId: "ties-chart", label: "Ties", color: "#477a6b" },
        { resultType: -1, canvasId: "away-wins-chart", label: "Away wins", color: "#4b68a8" }
    ];

    let teamsRequest = null;
    let matchesRequest = null;
    let chartRequest = null;
    let selectedMatch = null;
    let charts = new Map();
    let joinedMatchId = null;
    let hubStarting = false;
    let hubRetryTimer = null;

    const hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(root.dataset.hubUrl)
        .withAutomaticReconnect()
        .build();

    sportFilter.addEventListener("change", () => {
        resetTeamAndMatchFilters();
        if (sportFilter.value) {
            void loadTeams(sportFilter.value);
        }
    });

    teamFilter.addEventListener("change", () => {
        resetMatchFilter();
        if (teamFilter.value) {
            void loadMatches(teamFilter.value);
        }
    });

    matchFilter.addEventListener("change", () => {
        const previousMatchId = selectedMatch ? selectedMatch.matchId : null;
        selectedMatch = matchFilter.value
            ? JSON.parse(matchFilter.selectedOptions[0].dataset.match)
            : null;

        if (!selectedMatch) {
            dateFilter.value = "";
            dateFilter.disabled = true;
            clearCharts("Choose a sport, team, and match to view forecast counts.");
            void changeHubMatch(previousMatchId, null);
            return;
        }

        dateFilter.disabled = false;
        dateFilter.value = dateInTimeZone(new Date(), selectedMatch.timeZone);
        void changeHubMatch(previousMatchId, selectedMatch.matchId);
        void refreshCharts();
    });

    dateFilter.addEventListener("change", () => {
        void refreshCharts();
    });

    hubConnection.on("ForecastsChanged", matchId => {
        if (selectedMatch
            && String(selectedMatch.matchId) === String(matchId)
            && isSelectedDateToday()) {
            void refreshCharts();
        }
    });

    hubConnection.onreconnecting(() => {
        realtimeMessage.textContent = "Reconnecting to live forecast updates…";
    });

    hubConnection.onreconnected(async () => {
        joinedMatchId = null;
        try {
            await joinSelectedMatch();
            updateRealtimeMessage();
            if (isSelectedDateToday()) {
                await refreshCharts();
            }
        } catch (error) {
            realtimeMessage.textContent = `Unable to resume live updates: ${error.message}`;
            scheduleHubRecovery();
        }
    });

    hubConnection.onclose(() => {
        joinedMatchId = null;
        realtimeMessage.textContent = "Live updates disconnected; retrying…";
        scheduleHubRecovery();
    });

    async function loadTeams(sport) {
        if (teamsRequest) {
            teamsRequest.abort();
        }
        const request = new AbortController();
        teamsRequest = request;
        teamFilter.disabled = true;
        setOptions(teamFilter, "Loading teams…");

        try {
            const teams = await fetchJson(
                `${root.dataset.teamsUrl}?sport=${encodeURIComponent(sport)}`,
                request.signal);
            if (request.signal.aborted || sportFilter.value !== sport) {
                return;
            }
            setOptions(teamFilter, "Choose a team", teams.map(team => ({
                value: team.teamId,
                label: team.name
            })));
            teamFilter.disabled = teams.length === 0;
            if (!teams.length) {
                chartMessage.textContent = "No teams are available for this sport.";
            }
        } catch (error) {
            if (request.signal.aborted || sportFilter.value !== sport) {
                return;
            }
            teamFilter.disabled = true;
            setOptions(teamFilter, "Unable to load teams");
            showRequestError(error, "Unable to load teams.");
        }
    }

    async function loadMatches(teamId) {
        if (matchesRequest) {
            matchesRequest.abort();
        }
        const request = new AbortController();
        matchesRequest = request;
        matchFilter.disabled = true;
        setOptions(matchFilter, "Loading matches…");

        try {
            const matches = await fetchJson(
                `${root.dataset.matchesUrl}?teamId=${encodeURIComponent(teamId)}`,
                request.signal);
            if (request.signal.aborted || teamFilter.value !== teamId) {
                return;
            }
            setOptions(matchFilter, "Choose a match", matches.map(match => ({
                value: match.matchId,
                label: `${match.label} (${match.status})`,
                data: match
            })));
            matchFilter.disabled = matches.length === 0;
            if (!matches.length) {
                chartMessage.textContent = "No matches are available for this team.";
            }
        } catch (error) {
            if (request.signal.aborted || teamFilter.value !== teamId) {
                return;
            }
            matchFilter.disabled = true;
            setOptions(matchFilter, "Unable to load matches");
            showRequestError(error, "Unable to load matches.");
        }
    }

    async function refreshCharts() {
        if (!selectedMatch || !dateFilter.value) {
            return;
        }
        if (chartRequest) {
            chartRequest.abort();
        }
        chartRequest = new AbortController();
        const matchId = String(selectedMatch.matchId);
        const selectedDate = dateFilter.value;
        chartMessage.textContent = "Loading forecast counts…";
        updateRealtimeMessage();

        try {
            const rows = await fetchJson(
                `${root.dataset.chartDataUrl}?matchId=${encodeURIComponent(matchId)}`,
                chartRequest.signal);
            if (!selectedMatch
                || String(selectedMatch.matchId) !== matchId
                || dateFilter.value !== selectedDate) {
                return;
            }

            const apiDate = selectedDate.replaceAll("-", "/");
            renderCharts(rows.filter(row => row.date === apiDate));
            chartMessage.textContent = `Forecast counts for ${selectedDate}.`;
            updateRealtimeMessage();
        } catch (error) {
            showRequestError(error, "Unable to load forecast counts.");
        }
    }

    function renderCharts(rows) {
        const hours = Array.from({ length: 24 }, (_, hour) => String(hour).padStart(2, "0"));

        for (const definition of chartDefinitions) {
            const countsByHour = new Map(
                rows
                    .filter(row => row.resultType === definition.resultType)
                    .map(row => [row.hour, row.count]));
            const data = hours.map(hour => countsByHour.get(hour) || 0);
            const existing = charts.get(definition.canvasId);

            if (existing) {
                existing.data.labels = hours;
                existing.data.datasets[0].data = data;
                existing.update();
                continue;
            }

            charts.set(definition.canvasId, new Chart(
                document.getElementById(definition.canvasId),
                {
                    type: "line",
                    data: {
                        labels: hours,
                        datasets: [{
                            label: definition.label,
                            data,
                            borderColor: definition.color,
                            backgroundColor: definition.color,
                            borderWidth: 2,
                            pointRadius: 2,
                            tension: 0.2
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        scales: {
                            x: { title: { display: true, text: "Hour" } },
                            y: {
                                beginAtZero: true,
                                title: { display: true, text: "Forecast count" },
                                ticks: { precision: 0 }
                            }
                        }
                    }
                }));
        }
    }

    function clearCharts(message) {
        if (chartRequest) {
            chartRequest.abort();
        }
        for (const chart of charts.values()) {
            chart.destroy();
        }
        charts = new Map();
        chartMessage.textContent = message;
        realtimeMessage.textContent = "";
    }

    async function changeHubMatch(previousMatchId, nextMatchId) {
        try {
            if (hubConnection.state === signalR.HubConnectionState.Connected
                && previousMatchId
                && String(previousMatchId) === String(joinedMatchId)) {
                await hubConnection.invoke("LeaveMatch", Number(previousMatchId));
                joinedMatchId = null;
            }

            if (nextMatchId) {
                await startHub();
                await joinSelectedMatch();
                updateRealtimeMessage();
            }
        } catch (error) {
            realtimeMessage.textContent = `Unable to join live updates: ${error.message}`;
            scheduleHubRecovery();
        }
    }

    async function startHub() {
        if (hubConnection.state !== signalR.HubConnectionState.Disconnected || hubStarting) {
            return;
        }
        hubStarting = true;
        try {
            await hubConnection.start();
            if (hubRetryTimer) {
                clearTimeout(hubRetryTimer);
                hubRetryTimer = null;
            }
            await joinSelectedMatch();
            updateRealtimeMessage();
        } catch (error) {
            realtimeMessage.textContent = `Live updates unavailable: ${error.message}`;
            scheduleHubRecovery();
        } finally {
            hubStarting = false;
        }
    }

    function scheduleHubRecovery() {
        if (hubRetryTimer) {
            return;
        }
        hubRetryTimer = setTimeout(async () => {
            hubRetryTimer = null;
            try {
                if (hubConnection.state === signalR.HubConnectionState.Connected) {
                    await joinSelectedMatch();
                    updateRealtimeMessage();
                } else {
                    await startHub();
                }
            } catch (error) {
                realtimeMessage.textContent = `Unable to resume live updates: ${error.message}`;
                scheduleHubRecovery();
            }
        }, 5000);
    }

    async function joinSelectedMatch() {
        if (!selectedMatch || hubConnection.state !== signalR.HubConnectionState.Connected) {
            return;
        }
        const matchId = String(selectedMatch.matchId);
        if (String(joinedMatchId) === matchId) {
            return;
        }
        await hubConnection.invoke("JoinMatch", Number(matchId));
        joinedMatchId = matchId;
    }

    function updateRealtimeMessage() {
        if (!selectedMatch) {
            realtimeMessage.textContent = "";
        } else if (!isSelectedDateToday()) {
            realtimeMessage.textContent = "Historical date — chart data is static.";
        } else if (hubConnection.state === signalR.HubConnectionState.Connected) {
            realtimeMessage.textContent = "Today’s chart updates as forecasts change.";
        } else {
            realtimeMessage.textContent = "Connecting to live forecast updates…";
        }
    }

    function isSelectedDateToday() {
        return selectedMatch
            && dateFilter.value
            && dateFilter.value === dateInTimeZone(new Date(), selectedMatch.timeZone);
    }

    function dateInTimeZone(date, timeZone) {
        const parts = new Intl.DateTimeFormat("en-US", {
            timeZone,
            year: "numeric",
            month: "2-digit",
            day: "2-digit"
        }).formatToParts(date);
        const values = Object.fromEntries(parts.map(part => [part.type, part.value]));
        return `${values.year}-${values.month}-${values.day}`;
    }

    function resetTeamAndMatchFilters() {
        if (teamsRequest) {
            teamsRequest.abort();
        }
        if (matchesRequest) {
            matchesRequest.abort();
        }
        teamFilter.disabled = true;
        matchFilter.disabled = true;
        setOptions(teamFilter, "Choose a sport first");
        resetMatchFilter();
    }

    function resetMatchFilter() {
        if (matchesRequest) {
            matchesRequest.abort();
        }
        const previousMatchId = selectedMatch ? selectedMatch.matchId : null;
        selectedMatch = null;
        matchFilter.disabled = true;
        setOptions(matchFilter, "Choose a team first");
        dateFilter.value = "";
        dateFilter.disabled = true;
        clearCharts("Choose a sport, team, and match to view forecast counts.");
        if (previousMatchId) {
            void changeHubMatch(previousMatchId, null);
        }
    }

    function setOptions(select, placeholder, options = []) {
        select.replaceChildren(new Option(placeholder, ""));
        for (const item of options) {
            const option = new Option(item.label, item.value);
            if (item.data) {
                option.dataset.match = JSON.stringify(item.data);
            }
            select.add(option);
        }
    }

    async function fetchJson(url, signal) {
        const response = await fetch(url, {
            headers: { Accept: "application/json" },
            cache: "no-store",
            signal
        });
        if (!response.ok) {
            const message = await response.text();
            throw new Error(message || `Request failed (${response.status}).`);
        }
        return response.json();
    }

    function showRequestError(error, fallback) {
        if (error.name !== "AbortError") {
            chartMessage.textContent = `${fallback} ${error.message}`;
        }
    }
})();
