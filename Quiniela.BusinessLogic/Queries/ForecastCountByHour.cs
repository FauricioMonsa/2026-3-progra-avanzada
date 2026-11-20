namespace Quiniela.BusinessLogic;

public sealed record ForecastCountByHour(
    string Date,
    string Hour,
    int ResultType,
    int Count);
