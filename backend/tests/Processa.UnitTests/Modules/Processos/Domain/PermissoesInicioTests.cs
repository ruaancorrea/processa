using FluentAssertions;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Domain;

public class PermissoesInicioTests
{
    [Fact]
    public void Criar_ComDuplicatas_RemoveDuplicatas()
    {
        var usuarioId = Guid.NewGuid();

        var permissoes = PermissoesInicio.Criar([Perfil.Gestor, Perfil.Gestor, Perfil.Admin], [usuarioId, usuarioId]);

        permissoes.Perfis.Should().BeEquivalentTo([Perfil.Gestor, Perfil.Admin]);
        permissoes.UsuarioIds.Should().BeEquivalentTo([usuarioId]);
    }

    [Fact]
    public void Criar_ComGuidVazio_FiltraGuidVazio()
    {
        var usuarioId = Guid.NewGuid();

        var permissoes = PermissoesInicio.Criar(null, [usuarioId, Guid.Empty]);

        permissoes.UsuarioIds.Should().BeEquivalentTo([usuarioId]);
    }

    [Fact]
    public void Criar_ComNulos_RetornaVazia()
    {
        var permissoes = PermissoesInicio.Criar(null, null);

        permissoes.Perfis.Should().BeEmpty();
        permissoes.UsuarioIds.Should().BeEmpty();
    }

    [Fact]
    public void Equals_MesmoConteudoOrdemDiferente_SaoIguais()
    {
        var usuarioA = Guid.NewGuid();
        var usuarioB = Guid.NewGuid();

        var permissoes1 = PermissoesInicio.Criar([Perfil.Admin, Perfil.Gestor], [usuarioA, usuarioB]);
        var permissoes2 = PermissoesInicio.Criar([Perfil.Gestor, Perfil.Admin], [usuarioB, usuarioA]);

        permissoes1.Should().Be(permissoes2);
    }
}
