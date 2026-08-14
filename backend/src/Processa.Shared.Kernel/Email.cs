using System.Text.RegularExpressions;

namespace Processa.Shared.Kernel;

/// <summary>
/// Vive em Shared.Kernel pelo mesmo motivo de <see cref="Cnpj"/> — Usuario
/// (Identidade) e ContatoCliente (Clientes) precisam da mesma validação de e-mail.
/// </summary>
public sealed partial class Email : ValueObject
{
    public string Valor { get; }

    private Email(string valor) => Valor = valor;

    public static Result<Email> Criar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return Result.Failure<Email>("O e-mail é obrigatório.");

        var normalizado = valor.Trim().ToLowerInvariant();

        if (!EmailRegex().IsMatch(normalizado))
            return Result.Failure<Email>("O e-mail informado não é válido.");

        return Result.Success(new Email(normalizado));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }

    public override string ToString() => Valor;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
