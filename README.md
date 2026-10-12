# Quiniela

Aplicación desarrollada con .NET 10 y Entity Framework Core que gestiona pronósticos deportivos, modelado relacional y consultas de partidos.

## Estructura

- `Quiniela.Model`: entidades y contexto de base de datos (`QuinielaDbContext`) con soporte para PostgreSQL.
- `Quiniela.SqlModel.Postgres`: scripts SQL (`tables.sql`) con esquemas, tablas, tipos ENUM y restricciones de integridad.
- `Quiniela.BusinessLogic`: contiene la lógica de negocio del sistema:
  - `QuinielaScorer`: cálculo de puntos de pronósticos deportivos.
  - Servicios CRUD (`CrudService<T>`): operaciones genéricas de inserción, actualización, eliminación y consulta por llave primaria para las entidades del modelo.
  - Consultas de partidos (`MatchQueryService`, `IMatchQueryService`): consultas especializadas de partidos ordenadas cronológicamente ascendente.
- `Quiniela.AppConsole`: aplicación de consola para interactuar con `QuinielaScorer`.
- `Quiniela.Mvc`: interfaz web MVC para calcular puntos e interactuar con el sistema.
- `Quiniela.BusinessLogic.Tests`: pruebas unitarias con xUnit para las reglas de puntuación, los servicios CRUD y las consultas de partidos.

## Consultas de Partidos (Queries)

El proyecto `Quiniela.BusinessLogic` incorpora consultas especializadas disponibles a través de `MatchQueryService` (e integradas en `MatchCrudService`, `TeamCrudService` y `TournamentCrudService`):

1. **Partidos jugados por un equipo en un intervalo de tiempo**:
   - Método: `GetMatchesByTeamAsync(teamId, startDate, endDate)` (incluye sobrecarga para `DateOnly`).
   - Retorna todos los partidos donde el equipo participó (ya sea como local o como visitante) dentro del rango de fechas especificado.
   - Ordenados cronológicamente en orden ascendente por fecha y hora de inicio (`KickoffAt`).

2. **Partidos de un torneo**:
   - Método: `GetMatchesByTournamentAsync(tournamentId)`.
   - Retorna todos los partidos correspondientes al torneo indicado.
   - Ordenados cronológicamente en orden ascendente por fecha y hora de inicio (`KickoffAt`).

### Tipo de Retorno Unificado (`MatchDetails`)

Ambas consultas retornan una colección del mismo tipo de registro unificado (`MatchDetails`, con alias `MatchSummary`), el cual contiene los siguientes campos:

- **Tournament name** (`TournamentName`): Nombre del torneo (o `null` para partidos amistosos).
- **Tournament season** (`TournamentSeason`): Temporada o año del torneo (o `null` para amistosos).
- **Tournament sport Name** (`TournamentSportName`): Nombre del deporte del torneo (o `null` para amistosos).
- **Match id** (`MatchId`): Identificador único del partido.
- **Match kind** (`MatchKind`): Tipo de partido (`Competition` o `Friendly`).
- **Home team name** (`HomeTeamName`): Nombre del equipo local.
- **Away team name** (`AwayTeamName`): Nombre del equipo visitante.
- **Date played** (`DatePlayed`): Fecha y hora del partido (`DateTime`).
- **Status name** (`StatusName`): Nombre del estado del partido (`Scheduled`, `Live`, `Finished`).
- **Home score** (`HomeScore`): Marcador del equipo local.
- **Away score** (`AwayScore`): Marcador del equipo visitante.

## Uso

Desde la raíz del repositorio:

```powershell
dotnet run --project .\Quiniela.AppConsole -- <realA> <realB> <usuarioA> <usuarioB>
```

Los cuatro valores deben ser marcadores enteros mayores o iguales que cero.

Para ejecutar la interfaz web:

```powershell
dotnet run --project .\Quiniela.Mvc
```

Después, abre `/Quiniela/WebUnitTesting` en la URL indicada por ASP.NET Core.

## Puntuación

- Marcador exacto: 5 puntos.
- Resultado correcto (gana A, gana B o empate): 2 puntos.
- Resultado correcto y uno de los dos marcadores acertado: 3 puntos.
- Resultado incorrecto: 0 puntos.

## Pruebas

Para ejecutar la suite completa de pruebas unitarias:

```powershell
dotnet test .\Quiniela.slnx
```
