using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.TiposProcesso;

public sealed record CampoPersonalizadoResumo(Guid Id, string Nome, TipoCampoPersonalizado Tipo, List<string> Opcoes, bool Obrigatorio, int Ordem);

public sealed record FluxoResumo(Guid Id, string Nome, string? Descricao, bool FluxoPadrao);

public sealed record PermissoesInicioResumo(List<Perfil> Perfis, List<Guid> UsuarioIds);

public sealed record TipoProcessoDetalhe(
    Guid Id,
    Guid EquipeId,
    string Nome,
    string? Descricao,
    bool ResponsavelObrigatorio,
    ModoAtribuicao ModoAtribuicao,
    Guid? ResponsavelFixoId,
    PermissoesInicioResumo PermissoesInicio,
    bool Ativo,
    List<CampoPersonalizadoResumo> Campos,
    List<FluxoResumo> Fluxos);

public sealed record ObterTipoProcessoPorIdQuery(Guid TipoProcessoId) : IRequest<Result<TipoProcessoDetalhe>>;

public sealed class ObterTipoProcessoPorIdQueryHandler(
    ITipoProcessoRepository tipoProcessoRepository,
    ICampoPersonalizadoRepository campoPersonalizadoRepository,
    IFluxoRepository fluxoRepository) : IRequestHandler<ObterTipoProcessoPorIdQuery, Result<TipoProcessoDetalhe>>
{
    public async Task<Result<TipoProcessoDetalhe>> Handle(ObterTipoProcessoPorIdQuery request, CancellationToken cancellationToken)
    {
        var tipoProcesso = await tipoProcessoRepository.ObterPorIdAsync(request.TipoProcessoId, cancellationToken);
        if (tipoProcesso is null)
            return Result.Failure<TipoProcessoDetalhe>("Tipo de processo não encontrado.");

        var campos = await campoPersonalizadoRepository.ListarPorTipoProcessoAsync(request.TipoProcessoId, cancellationToken);
        var fluxos = await fluxoRepository.ListarPorTipoProcessoAsync(request.TipoProcessoId, cancellationToken);

        return Result.Success(new TipoProcessoDetalhe(
            tipoProcesso.Id, tipoProcesso.EquipeId, tipoProcesso.Nome, tipoProcesso.Descricao,
            tipoProcesso.ResponsavelObrigatorio, tipoProcesso.ModoAtribuicao, tipoProcesso.ResponsavelFixoId,
            new PermissoesInicioResumo(tipoProcesso.PermissoesInicio.Perfis.ToList(), tipoProcesso.PermissoesInicio.UsuarioIds.ToList()),
            tipoProcesso.Ativo,
            campos.OrderBy(c => c.Ordem)
                .Select(c => new CampoPersonalizadoResumo(c.Id, c.Nome, c.Tipo, c.Opcoes.ToList(), c.Obrigatorio, c.Ordem)).ToList(),
            fluxos.Select(f => new FluxoResumo(f.Id, f.Nome, f.Descricao, f.FluxoPadrao)).ToList()));
    }
}
