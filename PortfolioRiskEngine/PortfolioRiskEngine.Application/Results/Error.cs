namespace PortfolioRiskEngine.Application.Results;

public abstract record Error(string Code, string Message);

public sealed record ValidationError(string Code, string Message) : Error(Code, Message);

public sealed record DependencyError(string Code, string Message) : Error(Code, Message);

public sealed record UnexpectedError(string Code, string Message) : Error(Code, Message);
