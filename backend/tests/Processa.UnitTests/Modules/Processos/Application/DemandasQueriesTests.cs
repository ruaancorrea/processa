using System.Text;
using FluentAssertions;
using NSubstitute;
using Processa.Modules.Processos.Application;
using Processa.Modules.Processos.Application.Demandas;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Application;

public class DemandasQueriesTests
{
    private readonly IDemandaRepository _demandaRepository = Substitute.For<IDemandaRepository>();
    private readonly IExecucaoEtapaRepository _execucaoEtapaRepository = Substitute.For<IExecucaoEtapaRepository>();
    private readonly IHistoricoExecucaoEtapaRepository _historicoRepository = Substitute.For<IHistoricoExecucaoEtapaRepository>();
    private readonly IComentarioExecucaoRepository _comentarioRepository = Substitute.For<IComentarioExecucaoRepository>();
    private readonly IAnexoExecucaoRepository _anexoRepository = Substitute.For<IAnexoExecucaoRepository>();
    private readonly IArmazenamentoArquivo _armazenamentoArquivo = Substitute.For<IArmazenamentoArquivo>();

    private static Demanda CriarDemanda() =>
        Demanda.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Prioridade.Media, null).Value;

    // -------- ListarDemandasQuery --------

    [Fact]
    public async Task ListarDemandas_ProjetaResumoDeCadaDemanda()
    {
        var demanda = CriarDemanda();
        _demandaRepository.ListarAsync().Returns([demanda]);

        var resultado = await new ListarDemandasQueryHandler(_demandaRepository).Handle(new ListarDemandasQuery(), CancellationToken.None);

        resultado.Should().ContainSingle(d => d.Id == demanda.Id && d.Status == demanda.Status);
    }

    // -------- ObterDemandaPorIdQuery --------

    [Fact]
    public async Task ObterDemandaPorId_Inexistente_RetornaFalha()
    {
        _demandaRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Demanda?)null);

        var resultado = await new ObterDemandaPorIdQueryHandler(_demandaRepository, _execucaoEtapaRepository)
            .Handle(new ObterDemandaPorIdQuery(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ObterDemandaPorId_CasoValido_IncluiExecucoesOrdenadasPorCriacao()
    {
        var demanda = CriarDemanda();
        var execucao = ExecucaoEtapa.Criar(demanda.TenantId, demanda.Id, Guid.NewGuid(), demanda.ResponsavelId).Value;
        _demandaRepository.ObterPorIdAsync(demanda.Id).Returns(demanda);
        _execucaoEtapaRepository.ListarPorDemandaAsync(demanda.Id).Returns([execucao]);

        var resultado = await new ObterDemandaPorIdQueryHandler(_demandaRepository, _execucaoEtapaRepository)
            .Handle(new ObterDemandaPorIdQuery(demanda.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().Be(demanda.Id);
        resultado.Value.Execucoes.Should().ContainSingle(e => e.Id == execucao.Id);
    }

    // -------- ListarHistoricoExecucaoQuery --------

    [Fact]
    public async Task ListarHistorico_ProjetaEventosDaExecucao()
    {
        var execucaoId = Guid.NewGuid();
        var historico = HistoricoExecucaoEtapa.Registrar(Guid.NewGuid(), execucaoId, TipoEventoHistorico.StatusAlterado, "{}", Guid.NewGuid()).Value;
        _historicoRepository.ListarPorExecucaoAsync(execucaoId).Returns([historico]);

        var resultado = await new ListarHistoricoExecucaoQueryHandler(_historicoRepository)
            .Handle(new ListarHistoricoExecucaoQuery(execucaoId), CancellationToken.None);

        resultado.Should().ContainSingle(h => h.Id == historico.Id && h.TipoEvento == TipoEventoHistorico.StatusAlterado);
    }

    // -------- ListarComentariosQuery --------

    [Fact]
    public async Task ListarComentarios_ProjetaComentariosDaExecucao()
    {
        var execucaoId = Guid.NewGuid();
        var comentario = ComentarioExecucao.Criar(Guid.NewGuid(), execucaoId, Guid.NewGuid(), "Texto").Value;
        _comentarioRepository.ListarPorExecucaoAsync(execucaoId).Returns([comentario]);

        var resultado = await new ListarComentariosQueryHandler(_comentarioRepository)
            .Handle(new ListarComentariosQuery(execucaoId), CancellationToken.None);

        resultado.Should().ContainSingle(c => c.Id == comentario.Id && c.Texto == "Texto");
    }

    // -------- ListarAnexosQuery --------

    [Fact]
    public async Task ListarAnexos_ProjetaAnexosDaExecucao()
    {
        var execucaoId = Guid.NewGuid();
        var anexo = AnexoExecucao.Criar(Guid.NewGuid(), execucaoId, Guid.NewGuid(), "arquivo.pdf", "uuid-arquivo.pdf", "caminho/arquivo.pdf", 100, "application/pdf").Value;
        _anexoRepository.ListarPorExecucaoAsync(execucaoId).Returns([anexo]);

        var resultado = await new ListarAnexosQueryHandler(_anexoRepository)
            .Handle(new ListarAnexosQuery(execucaoId), CancellationToken.None);

        resultado.Should().ContainSingle(a => a.Id == anexo.Id && a.NomeOriginal == "arquivo.pdf");
    }

    // -------- ObterConteudoAnexoQuery --------

    [Fact]
    public async Task ObterConteudoAnexo_Inexistente_RetornaFalha()
    {
        _anexoRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((AnexoExecucao?)null);

        var resultado = await new ObterConteudoAnexoQueryHandler(_anexoRepository, _armazenamentoArquivo)
            .Handle(new ObterConteudoAnexoQuery(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ObterConteudoAnexo_CasoValido_AbreConteudoNoCaminhoCorreto()
    {
        var anexo = AnexoExecucao.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "arquivo.pdf", "uuid-arquivo.pdf", "caminho/arquivo.pdf", 100, "application/pdf").Value;
        _anexoRepository.ObterPorIdAsync(anexo.Id).Returns(anexo);
        using var conteudo = new MemoryStream(Encoding.UTF8.GetBytes("dados"));
        _armazenamentoArquivo.AbrirAsync(anexo.CaminhoStorage).Returns(conteudo);

        var resultado = await new ObterConteudoAnexoQueryHandler(_anexoRepository, _armazenamentoArquivo)
            .Handle(new ObterConteudoAnexoQuery(anexo.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.NomeOriginal.Should().Be("arquivo.pdf");
        resultado.Value.MimeType.Should().Be("application/pdf");
    }
}
