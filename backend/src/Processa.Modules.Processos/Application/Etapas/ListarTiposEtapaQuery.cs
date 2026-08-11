using MediatR;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Etapas;

/// <summary>
/// Caso de uso trivial (lista os tipos de etapa suportados) usado como fio-terra
/// para validar, desde o Sprint 0, que a cadeia Presentation → Application → Domain
/// funciona ponta a ponta antes de o domínio real ser implementado (Sprint 3-5).
/// </summary>
public sealed record ListarTiposEtapaQuery : IRequest<IReadOnlyCollection<TipoEtapa>>;

public sealed class ListarTiposEtapaQueryHandler : IRequestHandler<ListarTiposEtapaQuery, IReadOnlyCollection<TipoEtapa>>
{
    public Task<IReadOnlyCollection<TipoEtapa>> Handle(ListarTiposEtapaQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TipoEtapa> tipos = Enum.GetValues<TipoEtapa>();
        return Task.FromResult(tipos);
    }
}
