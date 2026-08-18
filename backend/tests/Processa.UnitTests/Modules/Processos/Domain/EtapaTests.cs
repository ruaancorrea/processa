using FluentAssertions;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Domain;

public class EtapaTests
{
    [Fact]
    public void Criar_TipoComumSemConfiguracao_RetornaSucesso()
    {
        var resultado = Etapa.Criar(Guid.NewGuid(), Guid.NewGuid(), "Revisão manual", null, TipoEtapa.Comum, 0, null);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.ConfiguracaoAcesso.Should().Be(ConfiguracaoAcessoEtapa.Vazia);
    }

    [Fact]
    public void Criar_TipoComumComConfiguracao_RetornaFalha()
    {
        var resultado = Etapa.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Nome", null, TipoEtapa.Comum, 0, new ConfiguracaoEtapaAgendamento(3));

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_TipoAutomatizadaSemConfiguracao_RetornaFalha()
    {
        var resultado = Etapa.Criar(Guid.NewGuid(), Guid.NewGuid(), "Chamada externa", null, TipoEtapa.Automatizada, 0, null);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_ConfiguracaoDeTipoErrado_RetornaFalha()
    {
        var resultado = Etapa.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Nome", null, TipoEtapa.Automatizada, 0, new ConfiguracaoEtapaAgendamento(3));

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_ConfiguracaoDoTipoCertoMasInvalida_PropagaFalhaDaConfiguracao()
    {
        var resultado = Etapa.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Nome", null, TipoEtapa.Agendamento, 0, new ConfiguracaoEtapaAgendamento(-1));

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_ConfiguracaoValidaDoTipoCerto_RetornaSucesso()
    {
        var resultado = Etapa.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Aguardar 3 dias", null, TipoEtapa.Agendamento, 0, new ConfiguracaoEtapaAgendamento(3));

        resultado.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Criar_NomeVazio_RetornaFalha()
    {
        var resultado = Etapa.Criar(Guid.NewGuid(), Guid.NewGuid(), "", null, TipoEtapa.Comum, 0, null);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AtualizarDados_DadosValidos_Atualiza()
    {
        var etapa = Etapa.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome antigo", null, TipoEtapa.Comum, 0, null).Value;

        var resultado = etapa.AtualizarDados("Nome novo", "Descrição", 2, null);

        resultado.IsSuccess.Should().BeTrue();
        etapa.Nome.Should().Be("Nome novo");
        etapa.Ordem.Should().Be(2);
    }

    [Fact]
    public void AtualizarDados_ConfiguracaoIncompativelComTipoOriginal_RetornaFalha()
    {
        var etapa = Etapa.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome", null, TipoEtapa.Comum, 0, null).Value;

        var resultado = etapa.AtualizarDados("Nome", null, 0, new ConfiguracaoEtapaAgendamento(1));

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void DefinirConfiguracaoAcesso_AtualizaConfiguracaoAcesso()
    {
        var etapa = Etapa.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome", null, TipoEtapa.Comum, 0, null).Value;
        var configuracaoAcesso = ConfiguracaoAcessoEtapa.Criar([Perfil.Gestor], []);

        etapa.DefinirConfiguracaoAcesso(configuracaoAcesso);

        etapa.ConfiguracaoAcesso.Should().Be(configuracaoAcesso);
    }
}
