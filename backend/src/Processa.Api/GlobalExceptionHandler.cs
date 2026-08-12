using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Processa.Shared.Kernel;

namespace Processa.Api;

/// <summary>
/// Traduz exceções não tratadas para o contrato RFC 9457 documentado em
/// docs/04-api/convencoes-api.md#4 — nunca deixa o Kestrel devolver o 500 cru padrão.
/// Resolve a pendência registrada em .faf/pendencias.faf no Sprint 0.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var problemDetails = exception switch
        {
            ValidationException validationException => new ProblemDetails
            {
                Type = "https://processa.app/erros/validacao",
                Title = "Erro de validação",
                Status = StatusCodes.Status422UnprocessableEntity,
                Detail = "Um ou mais campos são inválidos.",
                Extensions = { ["errors"] = validationException.Errors, ["traceId"] = traceId },
            },
            // Corrida entre a checagem de unicidade da Application layer (ex.: CNPJ/e-mail
            // já cadastrado) e o commit no banco: duas requisições concorrentes podem passar
            // pela checagem antes de qualquer uma commitar. A constraint UNIQUE do Postgres
            // é o backstop real; sem este case aqui, essa corrida vazava como 500 genérico
            // em vez do 422 "já existe" que o usuário veria numa tentativa não-concorrente.
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                new ProblemDetails
                {
                    Type = "https://processa.app/erros/conflito",
                    Title = "Registro em conflito.",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Detail = "Um registro com esses dados já existe. Atualize a página e tente novamente.",
                    Extensions = { ["traceId"] = traceId },
                },
            _ => CriarProblemaGenerico(traceId),
        };

        if (problemDetails.Status is null or >= 500)
            logger.LogError(exception, "Exceção não tratada. traceId={TraceId}", traceId);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static ProblemDetails CriarProblemaGenerico(string traceId) => new()
    {
        Type = "https://processa.app/erros/interno",
        Title = "Erro interno do servidor",
        Status = StatusCodes.Status500InternalServerError,
        Detail = "Ocorreu um erro inesperado. Se persistir, informe o traceId ao suporte.",
        Extensions = { ["traceId"] = traceId },
    };
}
