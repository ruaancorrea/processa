using FluentValidation;
using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Demandas;

/// <summary>Conteudo é um Stream porque tudo roda in-process via MediatR (não atravessa serialização) — Application ainda não conhece MinIO, só IArmazenamentoArquivo.</summary>
public sealed record AdicionarAnexoCommand(Guid ExecucaoEtapaId, string? NomeOriginal, Stream Conteudo, long TamanhoBytes, string? MimeType)
    : IRequest<Result<Guid>>;

public sealed class AdicionarAnexoCommandValidator : AbstractValidator<AdicionarAnexoCommand>
{
    public AdicionarAnexoCommandValidator()
    {
        RuleFor(x => x.ExecucaoEtapaId).NotEmpty();
        RuleFor(x => x.NomeOriginal).NotEmpty().MaximumLength(255);
        RuleFor(x => x.TamanhoBytes).GreaterThan(0).LessThanOrEqualTo(25 * 1024 * 1024).WithMessage("O arquivo não pode passar de 25 MB.");
    }
}

public sealed class AdicionarAnexoCommandHandler(
    IExecucaoEtapaRepository execucaoEtapaRepository,
    IAnexoExecucaoRepository anexoRepository,
    IHistoricoExecucaoEtapaRepository historicoRepository,
    IArmazenamentoArquivo armazenamentoArquivo,
    IUsuarioContext usuarioContext,
    IUnitOfWork unitOfWork) : IRequestHandler<AdicionarAnexoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AdicionarAnexoCommand request, CancellationToken cancellationToken)
    {
        var execucao = await execucaoEtapaRepository.ObterPorIdAsync(request.ExecucaoEtapaId, cancellationToken);
        if (execucao is null)
            return Result.Failure<Guid>("Execução não encontrada.");

        if (!AutorizacaoExecucao.PodeInteragir(usuarioContext, execucao))
            return Result.Failure<Guid>("Só o responsável, um gestor ou um admin pode anexar arquivo aqui.");

        if (!execucao.AceitaNovaInteracao())
            return Result.Failure<Guid>("Só é possível anexar enquanto a execução está pendente, em andamento ou aguardando.");

        var nomeArmazenado = $"{Guid.NewGuid()}-{request.NomeOriginal}";
        var caminho = $"{execucao.TenantId}/{execucao.Id}/{nomeArmazenado}";
        var mimeType = string.IsNullOrWhiteSpace(request.MimeType) ? "application/octet-stream" : request.MimeType;
        await armazenamentoArquivo.SalvarAsync(caminho, request.Conteudo, mimeType, cancellationToken);

        var anexoResult = AnexoExecucao.Criar(
            execucao.TenantId, execucao.Id, usuarioContext.UsuarioId, request.NomeOriginal, nomeArmazenado, caminho,
            request.TamanhoBytes, mimeType);
        if (anexoResult.IsFailure)
            return Result.Failure<Guid>(anexoResult.Error!);

        await anexoRepository.AddAsync(anexoResult.Value, cancellationToken);

        var historico = HistoricoExecucaoEtapa.Registrar(
            execucao.TenantId, execucao.Id, TipoEventoHistorico.AnexoAdicionado, null, usuarioContext.UsuarioId);
        if (historico.IsSuccess)
            await historicoRepository.AddAsync(historico.Value, cancellationToken);

        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success(anexoResult.Value.Id);
    }
}
