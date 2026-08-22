using FluentValidation;
using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Demandas;

public sealed record FalhaEdicaoEmMassa(Guid DemandaId, string Motivo);

public sealed record ResultadoEdicaoEmMassa(int TotalSucesso, IReadOnlyList<FalhaEdicaoEmMassa> Falhas);

/// <summary>Melhor-esforço, não tudo-ou-nada (PROJ-59, requisitos módulo 7.2): aplica o que for válido em cada demanda selecionada e reporta o que falhou, em vez de abortar o lote inteiro por causa de uma demanda em estado incompatível.</summary>
public sealed record AtualizarDemandasEmMassaCommand(
    IReadOnlyList<Guid> DemandaIds, Guid? NovoResponsavelId, Prioridade? NovaPrioridade, bool Cancelar)
    : IRequest<Result<ResultadoEdicaoEmMassa>>;

public sealed class AtualizarDemandasEmMassaCommandValidator : AbstractValidator<AtualizarDemandasEmMassaCommand>
{
    public AtualizarDemandasEmMassaCommandValidator()
    {
        RuleFor(x => x.DemandaIds).NotEmpty().WithMessage("Selecione ao menos uma demanda.");
        RuleFor(x => x).Must(x => x.NovoResponsavelId is not null || x.NovaPrioridade is not null || x.Cancelar)
            .WithMessage("Informe ao menos uma alteração: responsável, prioridade ou cancelamento.");
    }
}

public sealed class AtualizarDemandasEmMassaCommandHandler(
    IDemandaRepository demandaRepository,
    ITipoProcessoRepository tipoProcessoRepository,
    IVerificadorUsuario verificadorUsuario,
    IKanbanNotificador kanbanNotificador,
    IUnitOfWork unitOfWork) : IRequestHandler<AtualizarDemandasEmMassaCommand, Result<ResultadoEdicaoEmMassa>>
{
    public async Task<Result<ResultadoEdicaoEmMassa>> Handle(AtualizarDemandasEmMassaCommand request, CancellationToken ct)
    {
        if (request.NovoResponsavelId is { } responsavelId && !await verificadorUsuario.ExisteAsync(responsavelId, ct))
            return Result.Failure<ResultadoEdicaoEmMassa>("Usuário não encontrado.");

        var demandas = await demandaRepository.ListarPorIdsAsync(request.DemandaIds, ct);
        var encontradas = demandas.ToDictionary(d => d.Id);
        var falhas = new List<FalhaEdicaoEmMassa>();
        var tipoProcessoIdsAlterados = new HashSet<Guid>();
        var sucesso = 0;

        foreach (var demandaId in request.DemandaIds)
        {
            if (!encontradas.TryGetValue(demandaId, out var demanda))
            {
                falhas.Add(new FalhaEdicaoEmMassa(demandaId, "Demanda não encontrada."));
                continue;
            }

            var erro = AplicarAlteracoes(demanda, request);
            if (erro is not null)
            {
                falhas.Add(new FalhaEdicaoEmMassa(demandaId, erro));
            }
            else
            {
                sucesso++;
                tipoProcessoIdsAlterados.Add(demanda.TipoProcessoId);
            }
        }

        await unitOfWork.SalvarAsync(ct);
        await NotificarKanbanAsync(tipoProcessoIdsAlterados, ct);

        return Result.Success(new ResultadoEdicaoEmMassa(sucesso, falhas));
    }

    private async Task NotificarKanbanAsync(IReadOnlySet<Guid> tipoProcessoIds, CancellationToken ct)
    {
        if (tipoProcessoIds.Count == 0)
            return;

        var tiposProcesso = await tipoProcessoRepository.ListarPorIdsAsync(tipoProcessoIds, ct);
        foreach (var tipoProcesso in tiposProcesso)
            await kanbanNotificador.NotificarQuadroAlteradoAsync(tipoProcesso.EquipeId, tipoProcesso.Id, ct);
    }

    private static string? AplicarAlteracoes(Demanda demanda, AtualizarDemandasEmMassaCommand request)
    {
        if (request.NovoResponsavelId is { } responsavelId)
        {
            var resultado = demanda.AtribuirResponsavel(responsavelId);
            if (resultado.IsFailure)
                return resultado.Error;
        }

        if (request.NovaPrioridade is { } prioridade)
            demanda.AlterarPrioridade(prioridade);

        if (request.Cancelar)
        {
            var resultado = demanda.Cancelar();
            if (resultado.IsFailure)
                return resultado.Error;
        }

        return null;
    }
}
