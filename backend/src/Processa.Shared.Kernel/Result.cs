namespace Processa.Shared.Kernel;

/// <summary>
/// Resultado de uma operação de domínio/aplicação sem valor de retorno.
/// Falhas de regra de negócio são valores, não exceções — exceções ficam
/// reservadas para violações de invariante (bug) e falhas de infraestrutura.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("Um resultado de sucesso não pode carregar erro.");
        if (!isSuccess && error is null)
            throw new InvalidOperationException("Um resultado de falha precisa de uma mensagem de erro.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Não é possível acessar o valor de um resultado de falha.");

    protected internal Result(T? value, bool isSuccess, string? error) : base(isSuccess, error) => _value = value;

    public static implicit operator Result<T>(T value) => Success(value);
}
