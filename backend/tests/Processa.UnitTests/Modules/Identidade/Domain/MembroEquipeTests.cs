using FluentAssertions;
using Processa.Modules.Identidade.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Identidade.Domain;

public class MembroEquipeTests
{
    [Fact]
    public void Adicionar_PapelGestor_RetornaSucesso()
    {
        var resultado = MembroEquipe.Adicionar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Perfil.Gestor);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Papel.Should().Be(Perfil.Gestor);
    }

    [Fact]
    public void Adicionar_PapelAnalista_RetornaSucesso()
    {
        var resultado = MembroEquipe.Adicionar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Perfil.Analista);

        resultado.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Adicionar_PapelAdmin_RetornaFalha()
    {
        // Admin já opera o tenant inteiro (Sprint 1) — não faz sentido escopar por equipe.
        var resultado = MembroEquipe.Adicionar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Perfil.Admin);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Adicionar_EquipeVazia_RetornaFalha()
    {
        var resultado = MembroEquipe.Adicionar(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Perfil.Gestor);

        resultado.IsFailure.Should().BeTrue();
    }
}
