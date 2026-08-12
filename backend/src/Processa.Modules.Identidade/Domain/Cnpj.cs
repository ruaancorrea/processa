using System.Text.RegularExpressions;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Domain;

/// <summary>
/// CNPJ com validação de dígito verificador (módulo 11) — não só formato.
/// Armazenado só com dígitos (sem máscara); formatação é responsabilidade da apresentação.
/// </summary>
public sealed partial class Cnpj : ValueObject
{
    public string Numero { get; }

    private Cnpj(string numero) => Numero = numero;

    /// <summary>Usado por validadores de comando (FluentValidation) para reportar o erro
    /// no formato padrão de campo da API, sem duplicar a regra de dígito verificador.</summary>
    public static bool EhValido(string? valor) => Criar(valor).IsSuccess;

    public static Result<Cnpj> Criar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return Result.Failure<Cnpj>("O CNPJ é obrigatório.");

        var digitos = SomenteDigitos().Replace(valor, string.Empty);

        if (digitos.Length != 14)
            return Result.Failure<Cnpj>("O CNPJ deve ter 14 dígitos.");

        if (digitos.Distinct().Count() == 1)
            return Result.Failure<Cnpj>("O CNPJ informado não é válido.");

        if (!DigitosVerificadoresSaoValidos(digitos))
            return Result.Failure<Cnpj>("O CNPJ informado não é válido.");

        return Result.Success(new Cnpj(digitos));
    }

    private static bool DigitosVerificadoresSaoValidos(string digitos)
    {
        int[] pesos1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] pesos2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var dv1 = CalcularDigito(digitos[..12], pesos1);
        var dv2 = CalcularDigito(digitos[..12] + dv1, pesos2);

        return digitos[12] == dv1 && digitos[13] == dv2;
    }

    private static char CalcularDigito(string baseNumero, int[] pesos)
    {
        var soma = baseNumero.Select((c, i) => (c - '0') * pesos[i]).Sum();
        var resto = soma % 11;
        return resto < 2 ? '0' : (char)('0' + (11 - resto));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Numero;
    }

    public override string ToString() => Numero;

    [GeneratedRegex(@"\D")]
    private static partial Regex SomenteDigitos();
}
