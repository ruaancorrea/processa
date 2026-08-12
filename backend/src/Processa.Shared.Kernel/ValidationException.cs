namespace Processa.Shared.Kernel;

/// <summary>
/// Falha de validação de entrada, capturada pelo middleware global de exceção e
/// traduzida para 422 no formato RFC 9457 — ver docs/04-api/convencoes-api.md#4.
/// </summary>
public sealed class ValidationException(IDictionary<string, string[]> errors) : Exception("Um ou mais campos são inválidos.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}
