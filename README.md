# Quiniela

Aplicación de consola desarrollada con .NET 10 que compara el resultado real de un partido con el pronóstico de un usuario.

## Estructura

- `Quiniela.BusinessLogic`: contiene `QuinielaScorer`, la clase responsable de calcular los puntos.
- `Quiniela.AppConsole`: recibe los marcadores por línea de comandos y utiliza `QuinielaScorer`.
- `Quiniela.Mvc`: interfaz web MVC para consultar el saludo y calcular los puntos.
- `Quiniela.BusinessLogic.Tests`: pruebas unitarias xUnit para las reglas de puntuación.

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

```powershell
dotnet test .\Quiniela.slnx
```
