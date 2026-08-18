using FluentAssertions;
using Processa.Modules.Identidade.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Identidade.Domain;

public class UsuarioTests
{
    private static Usuario CriarUsuarioValido() =>
        Usuario.Criar(Guid.NewGuid(), "Ana Souza", "ana@exemplo.com", "hash-fake", Perfil.Analista).Value;

    [Fact]
    public void Criar_DadosValidos_RetornaSucessoENaoEstaBloqueado()
    {
        var resultado = Usuario.Criar(Guid.NewGuid(), "Ana Souza", "ana@exemplo.com", "hash-fake", Perfil.Analista);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.EstaBloqueado().Should().BeFalse();
        resultado.Value.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Criar_TenantVazio_RetornaFalha()
    {
        var resultado = Usuario.Criar(Guid.Empty, "Ana", "ana@exemplo.com", "hash", Perfil.Analista);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RegistrarTentativaFalha_MenosDeCincoVezes_NaoBloqueia()
    {
        var usuario = CriarUsuarioValido();

        for (var i = 0; i < 4; i++)
            usuario.RegistrarTentativaFalha();

        usuario.EstaBloqueado().Should().BeFalse();
        usuario.TentativasLoginFalhas.Should().Be(4);
    }

    [Fact]
    public void RegistrarTentativaFalha_QuintaVezConsecutiva_Bloqueia()
    {
        var usuario = CriarUsuarioValido();

        for (var i = 0; i < 5; i++)
            usuario.RegistrarTentativaFalha();

        usuario.EstaBloqueado().Should().BeTrue();
        usuario.BloqueadoAte.Should().BeAfter(DateTimeOffset.UtcNow);
        usuario.BloqueadoAte.Should().BeOnOrBefore(DateTimeOffset.UtcNow.AddMinutes(15).AddSeconds(1));
    }

    [Fact]
    public void RegistrarTentativaFalha_AposJanelaDeBloqueioExpirar_NaoRebloqueiaComUmaUnicaFalha()
    {
        // Bug real pego em revisão: sem resetar o contador quando a janela expira,
        // uma falha isolada após o desbloqueio automático re-bloqueava a conta na hora.
        var usuario = CriarUsuarioValido();
        for (var i = 0; i < 5; i++)
            usuario.RegistrarTentativaFalha();
        usuario.EstaBloqueado().Should().BeTrue();

        SimularExpiracaoDoBloqueio(usuario);

        usuario.RegistrarTentativaFalha();

        usuario.EstaBloqueado().Should().BeFalse();
        usuario.TentativasLoginFalhas.Should().Be(1);
    }

    private static void SimularExpiracaoDoBloqueio(Usuario usuario)
    {
        var campo = typeof(Usuario).GetField("<BloqueadoAte>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Campo BloqueadoAte não encontrado via reflection.");
        campo.SetValue(usuario, DateTimeOffset.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public void RegistrarLoginSucesso_ResetaContadorEDesbloqueia()
    {
        var usuario = CriarUsuarioValido();
        for (var i = 0; i < 5; i++)
            usuario.RegistrarTentativaFalha();

        usuario.EstaBloqueado().Should().BeTrue();

        usuario.RegistrarLoginSucesso();

        usuario.EstaBloqueado().Should().BeFalse();
        usuario.TentativasLoginFalhas.Should().Be(0);
    }
}
