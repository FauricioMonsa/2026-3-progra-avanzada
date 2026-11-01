(() => {
    "use strict";

    const root = document.querySelector(".matches-js");
    if (!root) {
        return;
    }

    const matchesUrl = root.dataset.matchesUrl;
    const forecastsUrl = root.dataset.forecastsUrl;
    const form = document.getElementById("match-filters");
    const tournamentFilter = document.getElementById("tournament-filter");
    const teamFilter = document.getElementById("team-filter");
    const pageSizeFilter = document.getElementById("page-size");
    const matchesGrid = document.getElementById("matches-grid");
    const forecastsGrid = document.getElementById("forecasts-grid");
    const matchesMessage = document.getElementById("matches-message");
    const forecastsMessage = document.getElementById("forecasts-message");
    const matchesCount = document.getElementById("matches-count");
    const selectedMatchLabel = document.getElementById("selected-match");
    const pageIndicator = document.getElementById("page-indicator");
    const previousButton = document.getElementById("previous-page");
    const nextButton = document.getElementById("next-page");

    let pageNumber = 1;
    let totalCount = 0;
    let selectedMatchId = null;
    let matchesRequest = null;
    let forecastsRequest = null;

    const kinds = ["Competition", "Friendly"];
    const statuses = ["Scheduled", "Live", "Finished"];
    const sports = ["Soccer", "Basketball", "Baseball", "Volleyball", "Rugby", "Hockey", "American football"];

    form.addEventListener("submit", event => {
        event.preventDefault();
        pageNumber = 1;
        selectedMatchId = null;
        clearForecasts("Select a match to view its forecasts.");
        refreshMatches();
    });

    tournamentFilter.addEventListener("change", () => {
        if (tournamentFilter.value) {
            teamFilter.value = "";
        }
    });

    teamFilter.addEventListener("change", () => {
        if (teamFilter.value) {
            tournamentFilter.value = "";
        }
    });

    previousButton.addEventListener("click", () => {
        if (pageNumber > 1) {
            pageNumber -= 1;
            refreshMatches();
        }
    });

    nextButton.addEventListener("click", () => {
        if (pageNumber * Number(pageSizeFilter.value) < totalCount) {
            pageNumber += 1;
            refreshMatches();
        }
    });

    matchesGrid.addEventListener("click", event => {
        const row = event.target.closest("tr[data-match-id]");
        if (!row) {
            return;
        }
        selectMatchRow(row);
    });

    matchesGrid.addEventListener("keydown", event => {
        if (event.key !== "Enter" && event.key !== " ") {
            return;
        }
        const row = event.target.closest("tr[data-match-id]");
        if (!row) {
            return;
        }
        event.preventDefault();
        selectMatchRow(row);
    });

    function selectMatchRow(row) {
        selectedMatchId = row.dataset.matchId;
        matchesGrid.querySelectorAll("tr[aria-selected='true']")
            .forEach(selected => selected.setAttribute("aria-selected", "false"));
        row.setAttribute("aria-selected", "true");
        refreshForecasts(selectedMatchId, row.dataset.matchLabel);
    }

    async function refreshMatches() {
        if (matchesRequest) {
            matchesRequest.abort();
        }
        matchesRequest = new AbortController();
        matchesMessage.textContent = "Refreshing matches…";
        previousButton.disabled = true;
        nextButton.disabled = true;

        const query = new URLSearchParams({
            pageNumber: String(pageNumber),
            pageSize: pageSizeFilter.value
        });
        if (tournamentFilter.value) {
            query.set("tournamentId", tournamentFilter.value);
        } else if (teamFilter.value) {
            query.set("teamId", teamFilter.value);
        }

        try {
            const response = await fetch(`${matchesUrl}?${query}`, {
                headers: { Accept: "application/json" },
                signal: matchesRequest.signal
            });
            if (!response.ok) {
                throw new Error(await readError(response));
            }

            const page = await response.json();
            totalCount = page.totalCount;
            renderMatches(page.matches);
            matchesCount.textContent = `${totalCount} match(es)`;
            pageIndicator.textContent = `Page ${page.pageNumber}`;
            previousButton.disabled = pageNumber <= 1;
            nextButton.disabled = pageNumber * pageSizeFilter.value >= totalCount;
            matchesMessage.textContent = page.matches.length
                ? "Select a row to refresh the forecast grid."
                : "No matches found for these filters.";

            if (selectedMatchId && page.matches.some(match => String(match.matchId) === selectedMatchId)) {
                const selectedRow = matchesGrid.querySelector(`tr[data-match-id="${selectedMatchId}"]`);
                if (selectedRow) {
                    selectedRow.setAttribute("aria-selected", "true");
                    refreshForecasts(selectedMatchId, selectedRow.dataset.matchLabel);
                }
            } else if (selectedMatchId) {
                selectedMatchId = null;
                clearForecasts("The selected match is not on this page.");
            }
        } catch (error) {
            if (error.name !== "AbortError") {
                matchesGrid.replaceChildren(messageRow(9, error.message));
                matchesMessage.textContent = "Unable to refresh matches.";
                matchesCount.textContent = "";
                pageIndicator.textContent = "";
            }
        }
    }

    async function refreshForecasts(matchId, matchLabel) {
        if (forecastsRequest) {
            forecastsRequest.abort();
        }
        forecastsRequest = new AbortController();
        selectedMatchLabel.textContent = matchLabel;
        forecastsMessage.textContent = "Refreshing forecasts…";
        forecastsGrid.replaceChildren(messageRow(4, "Loading forecasts…"));

        try {
            const query = new URLSearchParams({ matchId });
            const response = await fetch(`${forecastsUrl}?${query}`, {
                headers: { Accept: "application/json" },
                signal: forecastsRequest.signal
            });
            if (!response.ok) {
                throw new Error(await readError(response));
            }

            const forecasts = await response.json();
            if (String(selectedMatchId) !== String(matchId)) {
                return;
            }
            renderForecasts(forecasts);
            forecastsMessage.textContent = forecasts.length
                ? `${forecasts.length} forecast(s)`
                : "No forecasts have been submitted for this match.";
        } catch (error) {
            if (error.name !== "AbortError") {
                forecastsGrid.replaceChildren(messageRow(4, error.message));
                forecastsMessage.textContent = "Unable to refresh forecasts.";
            }
        }
    }

    function renderMatches(matches) {
        if (!matches.length) {
            matchesGrid.replaceChildren(messageRow(9, "No matches found."));
            return;
        }

        const rows = matches.map(match => {
            const row = document.createElement("tr");
            row.dataset.matchId = match.matchId;
            row.dataset.matchLabel = `${match.homeTeamName} vs ${match.awayTeamName} · Match ${match.matchId}`;
            row.tabIndex = 0;
            row.setAttribute("role", "button");
            row.setAttribute("aria-label", `Show forecasts for ${row.dataset.matchLabel}`);
            row.append(
                cell(match.matchId),
                cell(match.tournamentName || "Friendly"),
                cell(enumLabel(kinds, match.matchKind)),
                cell(match.sportName || enumLabel(sports, match.sport)),
                cell(match.homeTeamName),
                cell(match.awayTeamName),
                cell(formatDate(match.datePlayed)),
                cell(enumLabel(statuses, match.statusName)),
                cell(score(match.homeScore, match.awayScore))
            );
            return row;
        });
        matchesGrid.replaceChildren(...rows);
    }

    function renderForecasts(forecasts) {
        if (!forecasts.length) {
            forecastsGrid.replaceChildren(messageRow(4, "No forecasts found."));
            return;
        }
        forecastsGrid.replaceChildren(...forecasts.map(forecast => {
            const row = document.createElement("tr");
            row.append(
                cell(forecast.userId),
                cell(forecast.homeScore),
                cell(forecast.awayScore),
                cell(formatDate(forecast.submittedAt))
            );
            return row;
        }));
    }

    function clearForecasts(message) {
        selectedMatchLabel.textContent = "Select a match above.";
        forecastsMessage.textContent = message;
        forecastsGrid.replaceChildren(messageRow(4, "No match selected."));
        if (forecastsRequest) {
            forecastsRequest.abort();
        }
    }

    function cell(value) {
        const element = document.createElement("td");
        element.textContent = value == null ? "—" : String(value);
        return element;
    }

    function messageRow(columnCount, message) {
        const row = document.createElement("tr");
        const cellElement = document.createElement("td");
        cellElement.colSpan = columnCount;
        cellElement.textContent = message;
        row.append(cellElement);
        return row;
    }

    function score(home, away) {
        return home == null || away == null ? "—" : `${home} – ${away}`;
    }

    function enumLabel(values, value) {
        const index = Number(value);
        return Number.isInteger(index) && values[index] ? values[index] : String(value ?? "—");
    }

    function formatDate(value) {
        if (!value) {
            return "—";
        }
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleString();
    }

    async function readError(response) {
        const body = await response.text();
        return body || `Request failed (${response.status}).`;
    }

    refreshMatches();
})();
