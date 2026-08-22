using FluentAssertions;
using NSubstitute;
using Processa.Modules.Processos.Application;
using Processa.Modules.Processos.Application.Demandas;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Application;

public class CriarDemandaCommandHandlerTests
{
    private readonly IDemandaRepository _demandaRepository = Substitute.For<IDemandaRepository>();
    private readonly ITipoProcessoRepository _tipoProcessoRepository = Substitute.For<ITipoProcessoRepository>();
    private readonly IFluxoRepository _fluxoRepository = Substitute.For<IFluxoRepository>();
    private readonly IExecucaoEtapaRepository _execucaoEtapaRepository = Substitute.For<IExecucaoEtapaRepository>();
    private readonly IEtapaRepository _etapaRepository = Substitute.For<IEtapaRepository>();
    private readonly IVerificadorCliente _verificadorCliente = Substitute.For<IVerificadorCliente>();
    private readonly IVerificadorMembroEquipe _verificadorMembroEquipe = Substitute.For<IVerificadorMembroEquipe>();
    private readonly IListadorMembrosEquipe _listadorMembrosEquipe = Substitute.For<IListadorMembrosEquipe>();
    private readonly OrquestradorExecucao _orquestrador;
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUsuarioContext _usuarioContext = Substitute.For<IUsuarioContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public CriarDemandaCommandHandlerTests()
    {
        // OrquestradorExecucao é uma classe concreta com dependências próprias — como
        // este teste foca só na RESOLUÇÃO DE RESPONSÁVEL (a parte nova/arriscada de
        // CriarDemandaCommand), passamos repositórios vazios reais em vez de mockar
        // a classe inteira; ListarPorFluxoAsync retornando [] faz de IniciarAsync um no-op seguro
        // (sem configurar, o NSubstitute devolve null pra Task<List<T>>, não uma lista vazia).
        _etapaRepository.ListarPorFluxoAsync(Arg.Any<Guid>()).Returns([]);
        _orquestrador = new OrquestradorExecucao(
            _etapaRepository, _execucaoEtapaRepository, Substitute.For<IDesdobramentoAguardadoRepository>(),
            _demandaRepository, Substitute.For<ITipoProcessoRepository>(), new EtapaHandlerFactory([]),
            Substitute.For<IKanbanNotificador>(), _unitOfWork);
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _verificadorCliente.ExisteAtivoAsync(Arg.Any<Guid>()).Returns(true);
        _execucaoEtapaRepository.ListarPorDemandaAsync(Arg.Any<Guid>()).Returns([]);
    }

    private CriarDemandaCommandHandler CriarHandler() => new(
        _demandaRepository, _tipoProcessoRepository, _fluxoRepository, _execucaoEtapaRepository, _verificadorCliente,
        _verificadorMembroEquipe, _listadorMembrosEquipe, _orquestrador, _tenantContext, _usuarioContext, _unitOfWork);

    private static TipoProcesso CriarTipoProcesso(ModoAtribuicao modo, bool responsavelObrigatorio = true, Guid? responsavelFixoId = null) =>
        TipoProcesso.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome", null, responsavelObrigatorio, modo, responsavelFixoId).Value;

    private void ConfigurarFluxoPadrao(Guid tipoProcessoId, Fluxo fluxo) =>
        _fluxoRepository.ObterPadraoAsync(tipoProcessoId).Returns(fluxo);

    [Fact]
    public async Task Handle_TipoProcessoInexistente_RetornaFalha()
    {
        _tipoProcessoRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((TipoProcesso?)null);

        var resultado = await CriarHandler().Handle(
            new CriarDemandaCommand(Guid.NewGuid(), Guid.NewGuid(), null, Prioridade.Media, null, null), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ClienteInativoOuInexistente_RetornaFalha()
    {
        var tipoProcesso = CriarTipoProcesso(ModoAtribuicao.Manual, responsavelObrigatorio: false);
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        _verificadorCliente.ExisteAtivoAsync(Arg.Any<Guid>()).Returns(false);

        var resultado = await CriarHandler().Handle(
            new CriarDemandaCommand(tipoProcesso.Id, Guid.NewGuid(), null, Prioridade.Media, null, null), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PermissoesInicioRestritaAPerfilQueUsuarioNaoTem_RetornaFalha()
    {
        var tipoProcesso = CriarTipoProcesso(ModoAtribuicao.Manual, responsavelObrigatorio: false);
        tipoProcesso.DefinirPermissoesInicio(PermissoesInicio.Criar([Perfil.Gestor], null));
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        _usuarioContext.Perfil.Returns(Perfil.Analista);
        _usuarioContext.UsuarioId.Returns(Guid.NewGuid());

        var resultado = await CriarHandler().Handle(
            new CriarDemandaCommand(tipoProcesso.Id, Guid.NewGuid(), null, Prioridade.Media, null, null), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PermissoesInicioRestritaAPerfilQueUsuarioTem_PermiteAbrir()
    {
        var tipoProcesso = CriarTipoProcesso(ModoAtribuicao.Manual, responsavelObrigatorio: false);
        tipoProcesso.DefinirPermissoesInicio(PermissoesInicio.Criar([Perfil.Gestor], null));
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        _usuarioContext.Perfil.Returns(Perfil.Gestor);
        var fluxo = Fluxo.Criar(tipoProcesso.TenantId, tipoProcesso.Id, "Fluxo", null, true).Value;
        ConfigurarFluxoPadrao(tipoProcesso.Id, fluxo);

        var resultado = await CriarHandler().Handle(
            new CriarDemandaCommand(tipoProcesso.Id, Guid.NewGuid(), null, Prioridade.Media, null, null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PermissoesInicioRestritaAUsuarioEspecifico_PermiteMesmoComPerfilForaDaLista()
    {
        var tipoProcesso = CriarTipoProcesso(ModoAtribuicao.Manual, responsavelObrigatorio: false);
        var usuarioAutorizadoId = Guid.NewGuid();
        tipoProcesso.DefinirPermissoesInicio(PermissoesInicio.Criar([Perfil.Gestor], [usuarioAutorizadoId]));
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        _usuarioContext.Perfil.Returns(Perfil.Analista);
        _usuarioContext.UsuarioId.Returns(usuarioAutorizadoId);
        var fluxo = Fluxo.Criar(tipoProcesso.TenantId, tipoProcesso.Id, "Fluxo", null, true).Value;
        ConfigurarFluxoPadrao(tipoProcesso.Id, fluxo);

        var resultado = await CriarHandler().Handle(
            new CriarDemandaCommand(tipoProcesso.Id, Guid.NewGuid(), null, Prioridade.Media, null, null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ModoFixo_AtribuiResponsavelFixoAutomaticamente()
    {
        var responsavelFixoId = Guid.NewGuid();
        var tipoProcesso = CriarTipoProcesso(ModoAtribuicao.Fixo, responsavelFixoId: responsavelFixoId);
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        var fluxo = Fluxo.Criar(tipoProcesso.TenantId, tipoProcesso.Id, "Fluxo", null, true).Value;
        ConfigurarFluxoPadrao(tipoProcesso.Id, fluxo);

        var resultado = await CriarHandler().Handle(
            new CriarDemandaCommand(tipoProcesso.Id, Guid.NewGuid(), null, Prioridade.Media, null, null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _demandaRepository.Received(1).AddAsync(
            Arg.Is<Demanda>(d => d.ResponsavelId == responsavelFixoId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ModoDinamico_AtribuiMembroComMenosDemandasAtivas()
    {
        var tipoProcesso = CriarTipoProcesso(ModoAtribuicao.Dinamico);
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        var fluxo = Fluxo.Criar(tipoProcesso.TenantId, tipoProcesso.Id, "Fluxo", null, true).Value;
        ConfigurarFluxoPadrao(tipoProcesso.Id, fluxo);
        var membroOcupado = Guid.NewGuid();
        var membroLivre = Guid.NewGuid();
        _listadorMembrosEquipe.ListarUsuarioIdsAsync(tipoProcesso.EquipeId).Returns([membroOcupado, membroLivre]);
        _demandaRepository.ContarAtivasPorResponsavelAsync(membroOcupado).Returns(5);
        _demandaRepository.ContarAtivasPorResponsavelAsync(membroLivre).Returns(0);

        var resultado = await CriarHandler().Handle(
            new CriarDemandaCommand(tipoProcesso.Id, Guid.NewGuid(), null, Prioridade.Media, null, null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _demandaRepository.Received(1).AddAsync(Arg.Is<Demanda>(d => d.ResponsavelId == membroLivre), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ModoManualResponsavelObrigatorioSemEscolha_RetornaFalha()
    {
        var tipoProcesso = CriarTipoProcesso(ModoAtribuicao.Manual, responsavelObrigatorio: true);
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        var fluxo = Fluxo.Criar(tipoProcesso.TenantId, tipoProcesso.Id, "Fluxo", null, true).Value;
        ConfigurarFluxoPadrao(tipoProcesso.Id, fluxo);

        var resultado = await CriarHandler().Handle(
            new CriarDemandaCommand(tipoProcesso.Id, Guid.NewGuid(), null, Prioridade.Media, null, null), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ModoManualResponsavelNaoObrigatorioSemEscolha_NasceSemResponsavel()
    {
        var tipoProcesso = CriarTipoProcesso(ModoAtribuicao.Manual, responsavelObrigatorio: false);
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        var fluxo = Fluxo.Criar(tipoProcesso.TenantId, tipoProcesso.Id, "Fluxo", null, true).Value;
        ConfigurarFluxoPadrao(tipoProcesso.Id, fluxo);

        var resultado = await CriarHandler().Handle(
            new CriarDemandaCommand(tipoProcesso.Id, Guid.NewGuid(), null, Prioridade.Media, null, null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _demandaRepository.Received(1).AddAsync(Arg.Is<Demanda>(d => d.ResponsavelId == null), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ModoManualResponsavelEscolhidoNaoEhMembro_RetornaFalha()
    {
        var tipoProcesso = CriarTipoProcesso(ModoAtribuicao.Manual, responsavelObrigatorio: true);
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        var fluxo = Fluxo.Criar(tipoProcesso.TenantId, tipoProcesso.Id, "Fluxo", null, true).Value;
        ConfigurarFluxoPadrao(tipoProcesso.Id, fluxo);
        _verificadorMembroEquipe.EhMembroAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);

        var resultado = await CriarHandler().Handle(
            new CriarDemandaCommand(tipoProcesso.Id, Guid.NewGuid(), Guid.NewGuid(), Prioridade.Media, null, null), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SemFluxoPadrao_RetornaFalha()
    {
        var tipoProcesso = CriarTipoProcesso(ModoAtribuicao.Manual, responsavelObrigatorio: false);
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        _fluxoRepository.ObterPadraoAsync(tipoProcesso.Id).Returns((Fluxo?)null);

        var resultado = await CriarHandler().Handle(
            new CriarDemandaCommand(tipoProcesso.Id, Guid.NewGuid(), null, Prioridade.Media, null, null), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}
