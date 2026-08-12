namespace Processa.IntegrationTests.Identidade;

/// <summary>Gera CNPJs válidos (dígito verificador correto) e únicos para isolar testes.</summary>
public static class CnpjTestHelper
{
    private static int _contador = 1;

    public static string GerarValido()
    {
        var sequencial = Interlocked.Increment(ref _contador);
        var baseNumero = (10000000 + sequencial).ToString()[..8] + "0001";

        int[] pesos1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] pesos2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var dv1 = CalcularDigito(baseNumero, pesos1);
        var dv2 = CalcularDigito(baseNumero + dv1, pesos2);

        return $"{baseNumero}{dv1}{dv2}";
    }

    private static char CalcularDigito(string baseNumero, int[] pesos)
    {
        var soma = baseNumero.Select((c, i) => (c - '0') * pesos[i]).Sum();
        var resto = soma % 11;
        return resto < 2 ? '0' : (char)('0' + (11 - resto));
    }
}
