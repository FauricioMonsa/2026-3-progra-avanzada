BEGIN;

-- Enumera los deportes, estados y tipos de partido.
-- Los bloques hacen que se puedan ejecutar de nuevo si los tipos ya existen.
DO $$
BEGIN
    CREATE TYPE sport_type AS ENUM (
        'soccer',
        'basketball',
        'baseball',
        'volleyball',
        'rugby',
        'hockey'
    );
EXCEPTION
    WHEN duplicate_object THEN NULL;
END;
$$;

DO $$
BEGIN
    CREATE TYPE match_status AS ENUM (
        'scheduled',
        'live',
        'finished'
    );
EXCEPTION
    WHEN duplicate_object THEN NULL;
END;
$$;

DO $$
BEGIN
    CREATE TYPE match_kind AS ENUM (
        'competition',
        'friendly'
    );
EXCEPTION
    WHEN duplicate_object THEN NULL;
END;
$$;


-- Catálogo de equipos y torneos.
CREATE TABLE IF NOT EXISTS teams (
    team_id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name text NOT NULL,
    sport sport_type NOT NULL,

    UNIQUE (name, sport),
    UNIQUE (team_id, sport)
);

CREATE TABLE IF NOT EXISTS tournaments (
    tournament_id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name text NOT NULL,
    season_year smallint NOT NULL
        CHECK (season_year BETWEEN 1900 AND 2200),
    sport sport_type NOT NULL,
    time_zone text NOT NULL DEFAULT 'Europe/Madrid',

    UNIQUE (name, season_year, sport),
    UNIQUE (tournament_id, sport)
);

-- Inscripción de equipos en torneos.
-- El deporte se repite deliberadamente para que las FK compuestas
-- impidan inscribir equipos en torneos de otro deporte.
CREATE TABLE IF NOT EXISTS tournament_teams (
    tournament_id integer NOT NULL,
    team_id integer NOT NULL,
    sport sport_type NOT NULL,

    PRIMARY KEY (tournament_id, team_id),
    UNIQUE (tournament_id, team_id, sport),

    FOREIGN KEY (tournament_id, sport)
        REFERENCES tournaments (tournament_id, sport),
    FOREIGN KEY (team_id, sport)
        REFERENCES teams (team_id, sport)
);

CREATE TABLE IF NOT EXISTS users (
    user_id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    username text NOT NULL UNIQUE,
    email text NOT NULL UNIQUE,
    display_name text NOT NULL
);


-- Un partido puede ser parte de un torneo o ser amistoso.
-- Los amistosos no tienen tournament_id ni round_name.
-- match_day se calcula según la zona horaria correspondiente.
CREATE TABLE IF NOT EXISTS matches (
    match_id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    kind match_kind NOT NULL,
    tournament_id integer,
    round_name text,

    sport sport_type NOT NULL,
    home_team_id integer NOT NULL,
    away_team_id integer NOT NULL,

    kickoff_at timestamptz NOT NULL,
    time_zone text NOT NULL DEFAULT 'UTC',
    match_day date NOT NULL,

    status match_status NOT NULL DEFAULT 'scheduled',
    home_score integer CHECK (home_score >= 0),
    away_score integer CHECK (away_score >= 0),

    -- Datos específicos del deporte, por ejemplo sets, períodos o carreras.
    stats jsonb NOT NULL DEFAULT '{}'::jsonb,

    CHECK (home_team_id <> away_team_id),

    CHECK (
        (
            kind = 'friendly'
            AND tournament_id IS NULL
            AND round_name IS NULL
        )
        OR
        (
            kind = 'competition'
            AND tournament_id IS NOT NULL
        )
    ),

    CHECK (
        (
            status IN ('scheduled', 'live')
            AND home_score IS NULL
            AND away_score IS NULL
        )
        OR
        (
            status = 'finished'
            AND home_score IS NOT NULL
            AND away_score IS NOT NULL
        )
    ),

    -- Ambos equipos deben ser del deporte registrado en el partido.
    FOREIGN KEY (home_team_id, sport)
        REFERENCES teams (team_id, sport),
    FOREIGN KEY (away_team_id, sport)
        REFERENCES teams (team_id, sport),

    -- Para un torneo, el deporte también debe coincidir.
    FOREIGN KEY (tournament_id, sport)
        REFERENCES tournaments (tournament_id, sport),

    -- Un partido de torneo solo puede enfrentar equipos inscritos en él.
    -- En los amistosos tournament_id es NULL y estas FK no aplican.
    FOREIGN KEY (tournament_id, home_team_id, sport)
        REFERENCES tournament_teams (tournament_id, team_id, sport),
    FOREIGN KEY (tournament_id, away_team_id, sport)
        REFERENCES tournament_teams (tournament_id, team_id, sport)
);


-- Registro auxiliar para imponer que un equipo juegue como máximo
-- un partido por día local. Se llena automáticamente al insertar un partido.
CREATE TABLE IF NOT EXISTS match_participants (
    match_id integer NOT NULL
        REFERENCES matches (match_id) ON DELETE CASCADE,
    team_id integer NOT NULL
        REFERENCES teams (team_id),
    match_day date NOT NULL,

    PRIMARY KEY (match_id, team_id),
    UNIQUE (team_id, match_day)
);


CREATE TABLE IF NOT EXISTS forecasts (
    forecast_id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    match_id integer NOT NULL
        REFERENCES matches (match_id),
    user_id integer NOT NULL
        REFERENCES users (user_id),

    home_score integer NOT NULL CHECK (home_score >= 0),
    away_score integer NOT NULL CHECK (away_score >= 0),
    submitted_at timestamptz NOT NULL DEFAULT clock_timestamp(),

    -- Un usuario solo puede pronosticar una vez cada partido.
    UNIQUE (match_id, user_id)
);


CREATE INDEX IF NOT EXISTS ix_matches_tournament
    ON matches (tournament_id);

CREATE INDEX IF NOT EXISTS ix_forecasts_user
    ON forecasts (user_id);


-- La misma pareja puede enfrentarse en distintos partidos/rondas,
-- pero no dos veces en la misma ronda del mismo torneo.
-- round_name puede ser NULL para partidos sin ronda etiquetada.
CREATE UNIQUE INDEX IF NOT EXISTS uq_competition_pair_per_round
    ON matches (
        tournament_id,
        round_name,
        LEAST(home_team_id, away_team_id),
        GREATEST(home_team_id, away_team_id)
    )
    WHERE kind = 'competition'
      AND round_name IS NOT NULL;


-- Solo se puede cambiar la zona horaria de un torneo antes de inscribir
-- equipos o crear partidos. El deporte queda protegido por las FK.
CREATE OR REPLACE FUNCTION prevent_tournament_timezone_change()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.time_zone IS DISTINCT FROM OLD.time_zone
       AND (
           EXISTS (
               SELECT 1
               FROM tournament_teams
               WHERE tournament_id = OLD.tournament_id
           )
           OR EXISTS (
               SELECT 1
               FROM matches
               WHERE tournament_id = OLD.tournament_id
           )
       ) THEN
        RAISE EXCEPTION
            'Tournament time zone cannot change after registration or scheduling';
    END IF;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tournaments_timezone_immutable ON tournaments;
CREATE TRIGGER tournaments_timezone_immutable
BEFORE UPDATE OF time_zone ON tournaments
FOR EACH ROW
EXECUTE FUNCTION prevent_tournament_timezone_change();


-- Reglas de inserción y transición de estado de los partidos.
CREATE OR REPLACE FUNCTION enforce_match_rules()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_now timestamptz := clock_timestamp();
    v_tournament_time_zone text;
BEGIN
    IF TG_OP = 'INSERT' THEN
        IF NEW.status <> 'scheduled'
           OR NEW.home_score IS NOT NULL
           OR NEW.away_score IS NOT NULL THEN
            RAISE EXCEPTION
                'A new match must be scheduled with null scores';
        END IF;

        IF NEW.kickoff_at < v_now
           OR NEW.kickoff_at > v_now + interval '2 hours' THEN
            RAISE EXCEPTION
                'Match kickoff must be between now and two hours from now';
        END IF;

        IF NEW.kind = 'competition' THEN
            SELECT time_zone
            INTO v_tournament_time_zone
            FROM tournaments
            WHERE tournament_id = NEW.tournament_id;

            NEW.time_zone := v_tournament_time_zone;
        END IF;

        -- Esto también valida que time_zone sea una zona reconocida por PostgreSQL.
        NEW.match_day :=
            (NEW.kickoff_at AT TIME ZONE NEW.time_zone)::date;

        RETURN NEW;
    END IF;

    -- No se puede modificar ninguna columna de un partido finalizado.
    IF OLD.status = 'finished' THEN
        RAISE EXCEPTION 'A finished match cannot be updated';
    END IF;

    -- Identidad, equipos, tipo, torneo e inicio son inmutables.
    IF NEW.match_id IS DISTINCT FROM OLD.match_id
       OR NEW.kind IS DISTINCT FROM OLD.kind
       OR NEW.tournament_id IS DISTINCT FROM OLD.tournament_id
       OR NEW.round_name IS DISTINCT FROM OLD.round_name
       OR NEW.sport IS DISTINCT FROM OLD.sport
       OR NEW.home_team_id IS DISTINCT FROM OLD.home_team_id
       OR NEW.away_team_id IS DISTINCT FROM OLD.away_team_id
       OR NEW.kickoff_at IS DISTINCT FROM OLD.kickoff_at
       OR NEW.time_zone IS DISTINCT FROM OLD.time_zone
       OR NEW.match_day IS DISTINCT FROM OLD.match_day THEN
        RAISE EXCEPTION
            'Match identity, teams, tournament and kickoff are immutable';
    END IF;

    -- Al iniciar: solo cambia scheduled -> live.
    -- Los marcadores y las estadísticas no se pueden cambiar aquí.
    IF OLD.status = 'scheduled' AND NEW.status = 'live' THEN
        IF NEW.home_score IS DISTINCT FROM OLD.home_score
           OR NEW.away_score IS DISTINCT FROM OLD.away_score
           OR NEW.stats IS DISTINCT FROM OLD.stats THEN
            RAISE EXCEPTION
                'Only status can change when a match goes live';
        END IF;

    -- Al terminar: live -> finished, se pueden fijar los marcadores
    -- y completar las estadísticas finales.
    ELSIF OLD.status = 'live' AND NEW.status = 'finished' THEN
        IF NEW.home_score IS NULL OR NEW.away_score IS NULL THEN
            RAISE EXCEPTION 'Both final scores are required';
        END IF;

    ELSE
        RAISE EXCEPTION
            'Invalid match status transition: % -> %',
            OLD.status,
            NEW.status;
    END IF;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS matches_enforce_rules ON matches;
CREATE TRIGGER matches_enforce_rules
BEFORE INSERT OR UPDATE ON matches
FOR EACH ROW
EXECUTE FUNCTION enforce_match_rules();


-- Reserva los dos equipos para el día local del partido.
-- La restricción UNIQUE impide que cualquiera de ellos tenga otro partido
-- ese mismo día, sea amistoso o de torneo.
CREATE OR REPLACE FUNCTION register_match_participants()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO match_participants (match_id, team_id, match_day)
    VALUES
        (NEW.match_id, NEW.home_team_id, NEW.match_day),
        (NEW.match_id, NEW.away_team_id, NEW.match_day);

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS matches_register_participants ON matches;
CREATE TRIGGER matches_register_participants
AFTER INSERT ON matches
FOR EACH ROW
EXECUTE FUNCTION register_match_participants();


-- Los pronósticos se aceptan únicamente en la hora anterior al inicio.
-- submitted_at siempre lo determina el servidor y no se puede modificar
-- después de insertar el pronóstico.
CREATE OR REPLACE FUNCTION enforce_forecast_window()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_now timestamptz := clock_timestamp();
    v_kickoff timestamptz;
    v_status match_status;
BEGIN
    IF TG_OP = 'UPDATE' THEN
        RAISE EXCEPTION 'Forecasts are immutable once submitted';
    END IF;

    SELECT kickoff_at, status
    INTO v_kickoff, v_status
    FROM matches
    WHERE match_id = NEW.match_id;

    IF v_status IS DISTINCT FROM 'scheduled' THEN
        RAISE EXCEPTION
            'Forecasts can only be submitted for scheduled matches';
    END IF;

    IF v_now < v_kickoff - interval '1 hour'
       OR v_now >= v_kickoff THEN
        RAISE EXCEPTION
            'Forecasts are accepted only in the final hour before kickoff';
    END IF;

    NEW.submitted_at := v_now;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS forecasts_enforce_window ON forecasts;
CREATE TRIGGER forecasts_enforce_window
BEFORE INSERT OR UPDATE ON forecasts
FOR EACH ROW
EXECUTE FUNCTION enforce_forecast_window();


-- Datos base. Se pueden volver a insertar sin duplicarlos.
INSERT INTO teams (name, sport) VALUES
    ('Real Madrid', 'soccer'),
    ('Barcelona', 'soccer'),
    ('Osasuna', 'soccer'),
    ('Atlético Madrid', 'soccer'),
    ('Los Angeles Lakers', 'basketball'),
    ('Sydney Kings', 'basketball')
ON CONFLICT (name, sport) DO NOTHING;

INSERT INTO tournaments (name, season_year, sport, time_zone) VALUES
    ('Spain 2026 Winter Championship', 2026, 'soccer', 'Europe/Madrid'),
    ('NBL', 2026, 'basketball', 'Australia/Sydney')
ON CONFLICT (name, season_year, sport) DO NOTHING;

INSERT INTO tournament_teams (tournament_id, team_id, sport)
SELECT
    t.tournament_id,
    tm.team_id,
    t.sport
FROM (
    VALUES
        ('Spain 2026 Winter Championship', 2026,
            'soccer'::sport_type, 'Real Madrid'),
        ('Spain 2026 Winter Championship', 2026,
            'soccer'::sport_type, 'Barcelona'),
        ('Spain 2026 Winter Championship', 2026,
            'soccer'::sport_type, 'Osasuna'),
        ('Spain 2026 Winter Championship', 2026,
            'soccer'::sport_type, 'Atlético Madrid'),
        ('NBL', 2026,
            'basketball'::sport_type, 'Sydney Kings')
) AS seed(tournament_name, season_year, tournament_sport, team_name)
JOIN tournaments AS t
    ON t.name = seed.tournament_name
   AND t.season_year = seed.season_year
   AND t.sport = seed.tournament_sport
JOIN teams AS tm
    ON tm.name = seed.team_name
   AND tm.sport = seed.tournament_sport
ON CONFLICT (tournament_id, team_id) DO NOTHING;

INSERT INTO users (username, email, display_name) VALUES
    ('ana', 'ana@example.test', 'Ana García'),
    ('luis', 'luis@example.test', 'Luis Pérez')
ON CONFLICT (username) DO NOTHING;

COMMIT;