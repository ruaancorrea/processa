using System.Text;
using FluentAssertions;
using NSubstitute;
using Processa.Modules.Processos.Application;
using Processa.Modules.Processos.Application.Demandas;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Application;

public class ExecucaoEtapaCommandsTests
{
    private readonly IExecucaoEtapaRepository _execucaoEtapaRepository = Substitute.For<IExecucaoEtapaRepository>();
    private readonly IDemandaRepository _demandaRepository = Substitute.For<IDemandaRepository>();
    private readonly IHistoricoExecucaoEtapaRepository _historicoRepository = Substitute.For<IHistoricoExecucaoEtapaRepository>();
    private readonly IComentarioExecucaoRepository _comentarioRepository = Substitute.For<IComentarioExecucaoRepository>();
    private readonly IAnexoExecucaoRepository _anexoRepository = Substitute.For<IAnexoExecucaoRepository>();
    private readonly IArmazenamentoArquivo _armazenamentoArquivo = Substitute.For<IArmazenamentoArquivo>();
    private readonly IUsuarioContext _usuarioContext = Substitute.For<IUsuarioContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Guid _responsavelId = Guid.NewGuid();

    public ExecucaoEtapaCommandsTests() =>
        _usuarioContext.UsuarioId.Returns(_responsavelId);

    private ExecucaoEtapa CriarExecucaoAutorizada()
    {
        var execucao = ExecucaoEtapa.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _responsavelId).Value;
        _execucaoEtapaRepository.ObterPorIdAsync(execucao.Id).Returns(execucao);
        return execucao;
    }

    // -------- ConcluirExecucaoEtapaCommand --------

    private ConcluirExecucaoEtapaCommandHandler CriarConcluirHandler()
    {
        var orquestrador = new OrquestradorExecucao(
            Substitute.For<IEtapaRepository>(), _execucaoEtapaRepository, Substitute.For<IDesdobramentoAguardadoRepository>(),
            _demandaRepository, new EtapaHandlerFactory([]), _unitOfWork);
        return new ConcluirExecucaoEtapaCommandHandler(_execucaoEtapaRepository, _demandaRepository, _historicoRepository, orquestrador, _usuarioContext, _unitOfWork);
    }

    [Fact]
    public async Task Concluir_ExecucaoInexistente_RetornaFalha()
    {
        _execucaoEtapaRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((ExecucaoEtapa?)null);

        var resultado = await CriarConcluirHandler().Handle(new ConcluirExecucaoEtapaCommand(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Concluir_UsuarioNaoAutorizado_RetornaFalha()
    {
        var execucao = ExecucaoEtapa.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Value;
        _execucaoEtapaRepository.ObterPorIdAsync(execucao.Id).Returns(execucao);
        _usuarioContext.Perfil.Returns(Perfil.Analista);

        var resultado = await CriarConcluirHandler().Handle(new ConcluirExecucaoEtapaCommand(execucao.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Concluir_DemandaNaoEncontrada_RetornaFalha()
    {
        var execucao = CriarExecucaoAutorizada();
        _demandaRepository.ObterPorIdAsync(execucao.DemandaId).Returns((Demanda?)null);

        var resultado = await CriarConcluirHandler().Handle(new ConcluirExecucaoEtapaCommand(execucao.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Concluir_JaConcluida_RetornaFalha()
    {
        var execucao = CriarExecucaoAutorizada();
        execucao.Concluir();
        var demanda = Demanda.Criar(execucao.TenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _responsavelId, Prioridade.Media, null).Value;
        _demandaRepository.ObterPorIdAsync(execucao.DemandaId).Returns(demanda);

        var resultado = await CriarConcluirHandler().Handle(new ConcluirExecucaoEtapaCommand(execucao.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Concluir_CasoValido_ConcluiERegistraHistorico()
    {
        var execucao = CriarExecucaoAutorizada();
        var demanda = Demanda.Criar(execucao.TenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _responsavelId, Prioridade.Media, null).Value;
        _demandaRepository.ObterPorIdAsync(execucao.DemandaId).Returns(demanda);

        var resultado = await CriarConcluirHandler().Handle(new ConcluirExecucaoEtapaCommand(execucao.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        execucao.Status.Should().Be(StatusExecucaoEtapa.Concluida);
        await _historicoRepository.Received(1).AddAsync(Arg.Any<HistoricoExecucaoEtapa>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received().SalvarAsync(Arg.Any<CancellationToken>());
    }

    // -------- AdicionarComentarioCommand --------

    private AdicionarComentarioCommandHandler CriarComentarioHandler() =>
        new(_execucaoEtapaRepository, _comentarioRepository, _historicoRepository, _usuarioContext, _unitOfWork);

    [Fact]
    public async Task AdicionarComentario_ExecucaoInexistente_RetornaFalha()
    {
        _execucaoEtapaRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((ExecucaoEtapa?)null);

        var resultado = await CriarComentarioHandler().Handle(new AdicionarComentarioCommand(Guid.NewGuid(), "Texto"), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AdicionarComentario_ExecucaoJaConcluida_RetornaFalha()
    {
        var execucao = CriarExecucaoAutorizada();
        execucao.Concluir();

        var resultado = await CriarComentarioHandler().Handle(new AdicionarComentarioCommand(execucao.Id, "Texto"), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AdicionarComentario_TextoVazio_RetornaFalha()
    {
        var execucao = CriarExecucaoAutorizada();

        var resultado = await CriarComentarioHandler().Handle(new AdicionarComentarioCommand(execucao.Id, "   "), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AdicionarComentario_CasoValido_RetornaIdERegistraHistorico()
    {
        var execucao = CriarExecucaoAutorizada();

        var resultado = await CriarComentarioHandler().Handle(new AdicionarComentarioCommand(execucao.Id, "Comentário válido"), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _comentarioRepository.Received(1).AddAsync(Arg.Any<ComentarioExecucao>(), Arg.Any<CancellationToken>());
        await _historicoRepository.Received(1).AddAsync(Arg.Any<HistoricoExecucaoEtapa>(), Arg.Any<CancellationToken>());
    }

    // -------- AdicionarAnexoCommand --------

    private AdicionarAnexoCommandHandler CriarAnexoHandler() =>
        new(_execucaoEtapaRepository, _anexoRepository, _historicoRepository, _armazenamentoArquivo, _usuarioContext, _unitOfWork);

    private static MemoryStream CriarConteudo() => new(Encoding.UTF8.GetBytes("conteudo-de-teste"));

    [Fact]
    public async Task AdicionarAnexo_ExecucaoInexistente_RetornaFalha()
    {
        _execucaoEtapaRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((ExecucaoEtapa?)null);

        var resultado = await CriarAnexoHandler().Handle(
            new AdicionarAnexoCommand(Guid.NewGuid(), "arquivo.pdf", CriarConteudo(), 10, "application/pdf"), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AdicionarAnexo_ExecucaoJaConcluida_RetornaFalha()
    {
        var execucao = CriarExecucaoAutorizada();
        execucao.Concluir();

        var resultado = await CriarAnexoHandler().Handle(
            new AdicionarAnexoCommand(execucao.Id, "arquivo.pdf", CriarConteudo(), 10, "application/pdf"), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AdicionarAnexo_CasoValido_SalvaNoArmazenamentoERetornaId()
    {
        var execucao = CriarExecucaoAutorizada();

        var resultado = await CriarAnexoHandler().Handle(
            new AdicionarAnexoCommand(execucao.Id, "arquivo.pdf", CriarConteudo(), 10, "application/pdf"), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _armazenamentoArquivo.Received(1).SalvarAsync(
            Arg.Is<string>(caminho => caminho.Contains(execucao.TenantId.ToString()) && caminho.Contains(execucao.Id.ToString())),
            Arg.Any<Stream>(), "application/pdf", Arg.Any<CancellationToken>());
        await _anexoRepository.Received(1).AddAsync(Arg.Any<AnexoExecucao>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdicionarAnexo_MimeTypeAusente_UsaOctetStreamComoPadrao()
    {
        var execucao = CriarExecucaoAutorizada();

        var resultado = await CriarAnexoHandler().Handle(
            new AdicionarAnexoCommand(execucao.Id, "arquivo.bin", CriarConteudo(), 10, null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _armazenamentoArquivo.Received(1).SalvarAsync(
            Arg.Any<string>(), Arg.Any<Stream>(), "application/octet-stream", Arg.Any<CancellationToken>());
    }
}
