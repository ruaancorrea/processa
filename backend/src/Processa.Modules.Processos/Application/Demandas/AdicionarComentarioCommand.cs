using FluentValidation;
using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Demandas;

public sealed record AdicionarComentarioCommand(Guid ExecucaoEtapaId, string? Texto) : IRequest<Result<Guid>>;

public sealed class AdicionarComentarioCommandValidator : AbstractValidator<AdicionarComentarioCommand>
{
    public AdicionarComentarioCommandValidator()
    {
        RuleFor(x => x.ExecucaoEtapaId).NotEmpty();
        RuleFor(x => x.Texto).NotEmpty().MaximumLength(4000);
    }
}

public sealed class AdicionarComentarioCommandHandler(
    IExecucaoEtapaRepository execucaoEtapaRepository,
    IComentarioExecucaoRepository comentarioRepository,
    IHistoricoExecucaoEtapaRepository historicoRepository,
    IUsuarioContext usuarioContext,
    IUnitOfWork unitOfWork) : IRequestHandler<AdicionarComentarioCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AdicionarComentarioCommand request, CancellationToken cancellationToken)
    {
        var execucao = await execucaoEtapaRepository.ObterPorIdAsync(request.ExecucaoEtapaId, cancellationToken);
        if (execucao is null)
            return Result.Failure<Guid>("Execução não encontrada.");

        if (!AutorizacaoExecucao.PodeInteragir(usuarioContext, execucao))
            return Result.Failure<Guid>("Só o responsável, um gestor ou um admin pode comentar aqui.");

        if (!execucao.AceitaNovaInteracao())
            return Result.Failure<Guid>("Só é possível comentar enquanto a execução está pendente, em andamento ou aguardando.");

        var comentarioResult = ComentarioExecucao.Criar(execucao.TenantId, execucao.Id, usuarioContext.UsuarioId, request.Texto);
        if (comentarioResult.IsFailure)
            return Result.Failure<Guid>(comentarioResult.Error!);

        await comentarioRepository.AddAsync(comentarioResult.Value, cancellationToken);

        var historico = HistoricoExecucaoEtapa.Registrar(
            execucao.TenantId, execucao.Id, TipoEventoHistorico.ComentarioAdicionado, null, usuarioContext.UsuarioId);
        if (historico.IsSuccess)
            await historicoRepository.AddAsync(historico.Value, cancellationToken);

        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success(comentarioResult.Value.Id);
    }
}
