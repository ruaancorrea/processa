using FluentAssertions;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Domain;

public class ConfiguracaoEtapaCondicionalTests
{
    [Fact]
    public void Validar_SemRamos_RetornaFalha()
    {
        var configuracao = new ConfiguracaoEtapaCondicional([], Guid.NewGuid());

        configuracao.Validar().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Validar_SemEtapaPadrao_RetornaFalha()
    {
        var ramo = new RamoCondicional(Guid.NewGuid(), OperadorCondicional.Igual, "x", Guid.NewGuid());
        var configuracao = new ConfiguracaoEtapaCondicional([ramo], Guid.Empty);

        configuracao.Validar().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Validar_RamosEEtapaPadraoValidos_RetornaSucesso()
    {
        var ramo = new RamoCondicional(Guid.NewGuid(), OperadorCondicional.Igual, "x", Guid.NewGuid());
        var configuracao = new ConfiguracaoEtapaCondicional([ramo], Guid.NewGuid());

        configuracao.Validar().IsSuccess.Should().BeTrue();
    }
}

public class ConfiguracaoEtapaAutomatizadaTests
{
    [Theory]
    [InlineData("não-é-uma-url")]
    [InlineData("")]
    [InlineData("/caminho/relativo")]
    public void Validar_UrlInvalida_RetornaFalha(string url)
    {
        var configuracao = new ConfiguracaoEtapaAutomatizada(url, MetodoHttp.Get, null, 200);

        configuracao.Validar().IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(99)]
    [InlineData(600)]
    public void Validar_StatusForaDoIntervaloHttp_RetornaFalha(int status)
    {
        var configuracao = new ConfiguracaoEtapaAutomatizada("https://exemplo.com/api", MetodoHttp.Post, null, status);

        configuracao.Validar().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Validar_UrlEStatusValidos_RetornaSucesso()
    {
        var configuracao = new ConfiguracaoEtapaAutomatizada("https://exemplo.com/api", MetodoHttp.Post, "{}", 200);

        configuracao.Validar().IsSuccess.Should().BeTrue();
    }
}

public class ConfiguracaoEtapaNotificacaoTests
{
    [Fact]
    public void Validar_MensagemVazia_RetornaFalha()
    {
        var configuracao = new ConfiguracaoEtapaNotificacao(DestinatarioNotificacao.Responsavel, CanalNotificacao.Email, "");

        configuracao.Validar().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Validar_MensagemPreenchida_RetornaSucesso()
    {
        var configuracao = new ConfiguracaoEtapaNotificacao(DestinatarioNotificacao.Equipe, CanalNotificacao.Interno, "Prazo vencendo");

        configuracao.Validar().IsSuccess.Should().BeTrue();
    }
}

public class ConfiguracaoEtapaAgendamentoTests
{
    [Fact]
    public void Validar_DiasNegativo_RetornaFalha()
    {
        new ConfiguracaoEtapaAgendamento(-1).Validar().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Validar_DiasZeroOuPositivo_RetornaSucesso()
    {
        new ConfiguracaoEtapaAgendamento(0).Validar().IsSuccess.Should().BeTrue();
    }
}

public class ConfiguracaoEtapaSubprocessoTests
{
    [Fact]
    public void Validar_TipoProcessoFilhoVazio_RetornaFalha()
    {
        new ConfiguracaoEtapaSubprocesso(Guid.Empty, true).Validar().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Validar_TipoProcessoFilhoValido_RetornaSucesso()
    {
        new ConfiguracaoEtapaSubprocesso(Guid.NewGuid(), false).Validar().IsSuccess.Should().BeTrue();
    }
}

public class ConfiguracaoEtapaUniaoTests
{
    [Fact]
    public void Validar_MenosDeDuasEtapas_RetornaFalha()
    {
        new ConfiguracaoEtapaUniao([Guid.NewGuid()]).Validar().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Validar_ComGuidVazioNaLista_RetornaFalha()
    {
        new ConfiguracaoEtapaUniao([Guid.NewGuid(), Guid.Empty]).Validar().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Validar_DuasOuMaisEtapasValidas_RetornaSucesso()
    {
        new ConfiguracaoEtapaUniao([Guid.NewGuid(), Guid.NewGuid()]).Validar().IsSuccess.Should().BeTrue();
    }
}

public class ConfiguracaoAcessoEtapaTests
{
    [Fact]
    public void Criar_ComDuplicatas_RemoveDuplicatas()
    {
        var usuarioId = Guid.NewGuid();

        var configuracao = ConfiguracaoAcessoEtapa.Criar([Perfil.Gestor, Perfil.Gestor], [usuarioId, usuarioId]);

        configuracao.PodeAlterar.Should().BeEquivalentTo([Perfil.Gestor]);
        configuracao.UsuarioIdsPodeAlterar.Should().BeEquivalentTo([usuarioId]);
    }

    [Fact]
    public void Criar_ComNulos_RetornaVazia()
    {
        ConfiguracaoAcessoEtapa.Criar(null, null).Should().Be(ConfiguracaoAcessoEtapa.Vazia);
    }
}
