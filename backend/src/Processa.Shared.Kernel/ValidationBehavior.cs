using FluentValidation;
using MediatR;

namespace Processa.Shared.Kernel;

/// <summary>
/// Roda todos os IValidator&lt;TRequest&gt; registrados antes do handler. Falha ->
/// lança ValidationException (nunca deixa a requisição inválida chegar ao domínio).
/// Registrado uma vez por módulo via AddOpenBehavior(typeof(ValidationBehavior&lt;,&gt;)).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var falhas = new Dictionary<string, List<string>>();

        foreach (var validador in validators)
        {
            var resultado = await validador.ValidateAsync(request, cancellationToken);
            foreach (var erro in resultado.Errors)
            {
                if (!falhas.TryGetValue(erro.PropertyName, out var lista))
                {
                    lista = [];
                    falhas[erro.PropertyName] = lista;
                }

                lista.Add(erro.ErrorMessage);
            }
        }

        if (falhas.Count > 0)
            throw new ValidationException(falhas.ToDictionary(f => f.Key, f => f.Value.ToArray()));

        return await next();
    }
}
