using FluentValidation;
using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Demandas;

public sealed record ValorCampoPersonalizadoRequest(Guid CampoPersonalizadoId, string Valor);

/// <summary>Formulário e API usam o mesmo comando (ver .faf/decisions.faf) — a diferença é só a origem da chamada HTTP, não a regra de negócio (PROJ-52/PROJ-53).</summary>
public sealed record CriarDemandaCommand(
    Guid TipoProcessoId,
    Guid ClienteId,
    Guid? ResponsavelId,
    Prioridade Prioridade,
    DateTimeOffset? DataFimPrevista,
    List<ValorCampoPersonalizadoRequest>? CamposPersonalizados,
    Guid? DemandaPaiId = null) : IRequest<Result<Guid>>;

public sealed class CriarDemandaCommandValidator : AbstractValidator<CriarDemandaCommand>
{
    public CriarDemandaCommandValidator()
    {
        RuleFor(x => x.TipoProcessoId).NotEmpty();
        RuleFor(x => x.ClienteId).NotEmpty();
        RuleFor(x => x.Prioridade).IsInEnum();
    }
}

public sealed class CriarDemandaCommandHandler(
    IDemandaRepository demandaRepository,
    ITipoProcessoRepository tipoProcessoRepository,
    IFluxoRepository fluxoRepository,
    IExecucaoEtapaRepository execucaoEtapaRepository,
    IVerificadorCliente verificadorCliente,
    IVerificadorMembroEquipe verificadorMembroEquipe,
    IListadorMembrosEquipe listadorMembrosEquipe,
    OrquestradorExecucao orquestrador,
    ITenantContext tenantContext,
    IUsuarioContext usuarioContext,
    IUnitOfWork unitOfWork) : IRequestHandler<CriarDemandaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CriarDemandaCommand request, CancellationToken cancellationToken)
    {
        var tipoProcesso = await tipoProcessoRepository.ObterPorIdAsync(request.TipoProcessoId, cancellationToken);
        if (tipoProcesso is null || !tipoProcesso.Ativo)
            return Result.Failure<Guid>("Tipo de processo não encontrado ou inativo.");

        if (!PodeIniciar(tipoProcesso.PermissoesInicio, usuarioContext))
            return Result.Failure<Guid>("Você não tem permissão para abrir demandas deste tipo de processo.");

        if (!await verificadorCliente.ExisteAtivoAsync(request.ClienteId, cancellationToken))
            return Result.Failure<Guid>("Cliente não encontrado ou inativo.");

        var fluxoPadrao = await fluxoRepository.ObterPadraoAsync(request.TipoProcessoId, cancellationToken);
        if (fluxoPadrao is null)
            return Result.Failure<Guid>("Tipo de processo não tem um fluxo padrão configurado.");

        var responsavelResult = await ResolverResponsavelAsync(tipoProcesso, request.ResponsavelId, cancellationToken);
        if (responsavelResult.IsFailure)
            return Result.Failure<Guid>(responsavelResult.Error!);

        var demandaResult = Demanda.Criar(
            tenantContext.TenantId, request.TipoProcessoId, fluxoPadrao.Id, request.ClienteId, responsavelResult.Value,
            request.Prioridade, request.DataFimPrevista, request.DemandaPaiId);
        if (demandaResult.IsFailure)
            return Result.Failure<Guid>(demandaResult.Error!);

        var demanda = demandaResult.Value;
        await demandaRepository.AddAsync(demanda, cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        await orquestrador.IniciarAsync(demanda, cancellationToken);

        if (request.CamposPersonalizados is { Count: > 0 })
        {
            var primeiraExecucao = (await execucaoEtapaRepository.ListarPorDemandaAsync(demanda.Id, cancellationToken))
                .OrderBy(e => e.CreatedAt)
                .FirstOrDefault();

            if (primeiraExecucao is not null)
            {
                foreach (var campo in request.CamposPersonalizados)
                    primeiraExecucao.DefinirValorCampo(campo.CampoPersonalizadoId, campo.Valor);

                await unitOfWork.SalvarAsync(cancellationToken);
            }
        }

        return Result.Success(demanda.Id);
    }

    /// <summary>Lista vazia em ambos = sem restrição configurada (ver PermissoesInicio). Pendência do Sprint 3 fechada aqui: era só dado configurável até o Sprint 5 ligar a execução de verdade.</summary>
    private static bool PodeIniciar(PermissoesInicio permissoes, IUsuarioContext usuarioContext)
    {
        if (permissoes.Perfis.Count == 0 && permissoes.UsuarioIds.Count == 0)
            return true;

        return permissoes.Perfis.Contains(usuarioContext.Perfil) || permissoes.UsuarioIds.Contains(usuarioContext.UsuarioId);
    }

    /// <summary>Os 3 modos de atribuição (PROJ-54) — ver requisitos, módulo 10.2.</summary>
    private async Task<Result<Guid?>> ResolverResponsavelAsync(TipoProcesso tipoProcesso, Guid? responsavelInformado, CancellationToken ct)
    {
        switch (tipoProcesso.ModoAtribuicao)
        {
            case ModoAtribuicao.Fixo:
                return Result.Success(tipoProcesso.ResponsavelFixoId);

            case ModoAtribuicao.Dinamico:
                var membros = await listadorMembrosEquipe.ListarUsuarioIdsAsync(tipoProcesso.EquipeId, ct);
                if (membros.Count == 0)
                    return Result.Failure<Guid?>("A equipe responsável não tem membros para atribuição dinâmica.");

                Guid? menosCarregado = null;
                var menorContagem = int.MaxValue;
                foreach (var usuarioId in membros)
                {
                    var ativas = await demandaRepository.ContarAtivasPorResponsavelAsync(usuarioId, ct);
                    if (ativas < menorContagem)
                    {
                        menorContagem = ativas;
                        menosCarregado = usuarioId;
                    }
                }

                return Result.Success(menosCarregado);

            case ModoAtribuicao.Manual:
                if (!tipoProcesso.ResponsavelObrigatorio)
                    return Result.Success<Guid?>(null);

                if (responsavelInformado is not { } responsavelId)
                    return Result.Failure<Guid?>("Este tipo de processo exige que você escolha um responsável ao abrir a demanda.");

                if (!await verificadorMembroEquipe.EhMembroAsync(tipoProcesso.EquipeId, responsavelId, ct))
                    return Result.Failure<Guid?>("O responsável precisa ser membro da equipe responsável pelo tipo de processo.");

                return Result.Success<Guid?>(responsavelId);

            default:
                return Result.Failure<Guid?>("Modo de atribuição desconhecido.");
        }
    }
}
