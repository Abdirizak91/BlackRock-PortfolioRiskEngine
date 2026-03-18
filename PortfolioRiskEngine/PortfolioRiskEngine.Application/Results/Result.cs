namespace PortfolioRiskEngine.Application.Results;

public class Result
{
    private Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<Error> Errors { get; }

    public static Result Success() => new(true, []);

    public static Result Failure(params Error[] errors) => Failure((IEnumerable<Error>)errors);

    public static Result Failure(IEnumerable<Error> errors)
    {
        var errorList = errors.ToList();

        if (errorList.Count == 0)
            throw new ArgumentException("At least one error is required for failure results.", nameof(errors));

        return new Result(false, errorList);
    }
}

public class Result<T>
{
    private Result(T? value, bool isSuccess, IReadOnlyList<Error> errors)
    {
        Value = value;
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public T? Value { get; }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<Error> Errors { get; }

    public static Result<T> Success(T value) => new(value, true, []);

    public static Result<T> Failure(params Error[] errors) => Failure((IEnumerable<Error>)errors);

    public static Result<T> Failure(IEnumerable<Error> errors)
    {
        var errorList = errors.ToList();

        if (errorList.Count == 0)
            throw new ArgumentException("At least one error is required for failure results.", nameof(errors));

        return new Result<T>(default, false, errorList);
    }
}

