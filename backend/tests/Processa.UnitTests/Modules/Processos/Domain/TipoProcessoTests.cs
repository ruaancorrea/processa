using FluentAssertions;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Domain;

public class TipoProcessoTests
{
    [Fact]
    public void Criar_DadosValidos_RetornaSucessoEAtivo()
    {
        var resultado = TipoProcesso.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Abertura de empresa", "Descrição", true, ModoAtribuicao.Dinamico, null);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Ativo.Should().BeTrue();
        resultado.Value.PermissoesInicio.Should().Be(PermissoesInicio.Vazia);
    }

    [Fact]
    public void Criar_NomeVazio_RetornaFalha()
    {
        var resultado = TipoProcesso.Criar(Guid.NewGuid(), Guid.NewGuid(), "", null, true, ModoAtribuicao.Manual, null);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_TenantOuEquipeVazio_RetornaFalha()
    {
        var resultado = TipoProcesso.Criar(Guid.Empty, Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Manual, null);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_ModoFixoSemResponsavel_RetornaFalha()
    {
        var resultado = TipoProcesso.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Fixo, null);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_ModoFixoComResponsavel_RetornaSucesso()
    {
        var responsavelId = Guid.NewGuid();

        var resultado = TipoProcesso.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Fixo, responsavelId);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.ResponsavelFixoId.Should().Be(responsavelId);
    }

    [Fact]
    public void Criar_ModoNaoFixoComResponsavelInformado_IgnoraResponsavel()
    {
        var resultado = TipoProcesso.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Dinamico, Guid.NewGuid());

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.ResponsavelFixoId.Should().BeNull();
    }

    [Fact]
    public void AtualizarDados_ModoFixoSemResponsavel_RetornaFalha()
    {
        var tipoProcesso = TipoProcesso.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Manual, null).Value;

        var resultado = tipoProcesso.AtualizarDados("Nome novo", null, true, ModoAtribuicao.Fixo, null);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AtualizarDados_DadosValidos_Atualiza()
    {
        var tipoProcesso = TipoProcesso.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Nome antigo", null, true, ModoAtribuicao.Manual, null).Value;

        var resultado = tipoProcesso.AtualizarDados("Nome novo", "Descrição nova", false, ModoAtribuicao.Dinamico, null);

        resultado.IsSuccess.Should().BeTrue();
        tipoProcesso.Nome.Should().Be("Nome novo");
        tipoProcesso.ResponsavelObrigatorio.Should().BeFalse();
    }

    [Fact]
    public void AtivarDesativar_AlteramStatus()
    {
        var tipoProcesso = TipoProcesso.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Manual, null).Value;

        tipoProcesso.Desativar();
        tipoProcesso.Ativo.Should().BeFalse();

        tipoProcesso.Ativar();
        tipoProcesso.Ativo.Should().BeTrue();
    }

    [Fact]
    public void DefinirPermissoesInicio_AtualizaPermissoes()
    {
        var tipoProcesso = TipoProcesso.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Manual, null).Value;
        var permissoes = PermissoesInicio.Criar([Perfil.Admin], []);

        tipoProcesso.DefinirPermissoesInicio(permissoes);

        tipoProcesso.PermissoesInicio.Should().Be(permissoes);
    }
}
