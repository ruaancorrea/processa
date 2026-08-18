using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Etapas;

public sealed record ConfiguracaoAcessoEtapaResumo(List<Perfil> PodeAlterar, List<Guid> UsuarioIdsPodeAlterar);

public sealed record EtapaDetalhe(
    Guid Id, Guid FluxoId, string Nome, string? Descricao, TipoEtapa Tipo, int Ordem,
    ConfiguracaoEtapa? Configuracao, ConfiguracaoAcessoEtapaResumo ConfiguracaoAcesso);

public sealed record ObterEtapaPorIdQuery(Guid EtapaId) : IRequest<Result<EtapaDetalhe>>;

public sealed class ObterEtapaPorIdQueryHandler(IEtapaRepository etapaRepository) : IRequestHandler<ObterEtapaPorIdQuery, Result<EtapaDetalhe>>
{
    public async Task<Result<EtapaDetalhe>> Handle(ObterEtapaPorIdQuery request, CancellationToken cancellationToken)
    {
        var etapa = await etapaRepository.ObterPorIdAsync(request.EtapaId, cancellationToken);
        if (etapa is null)
            return Result.Failure<EtapaDetalhe>("Etapa não encontrada.");

        return Result.Success(new EtapaDetalhe(
            etapa.Id, etapa.FluxoId, etapa.Nome, etapa.Descricao, etapa.Tipo, etapa.Ordem, etapa.Configuracao,
            new ConfiguracaoAcessoEtapaResumo(etapa.ConfiguracaoAcesso.PodeAlterar.ToList(), etapa.ConfiguracaoAcesso.UsuarioIdsPodeAlterar.ToList())));
    }
}
