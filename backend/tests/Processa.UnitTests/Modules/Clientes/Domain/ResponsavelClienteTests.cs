using FluentAssertions;
using Processa.Modules.Clientes.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Clientes.Domain;

public class ResponsavelClienteTests
{
    [Fact]
    public void Criar_DadosValidos_RetornaSucessoEAtivo()
    {
        var resultado = ResponsavelCliente.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.EstaAtivo.Should().BeTrue();
    }

    [Fact]
    public void Criar_UsuarioVazio_RetornaFalha()
    {
        var resultado = ResponsavelCliente.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RemoverEReativar_AlternaEstaAtivo()
    {
        var responsavel = ResponsavelCliente.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Value;

        responsavel.Remover();
        responsavel.EstaAtivo.Should().BeFalse();

        responsavel.Reativar();
        responsavel.EstaAtivo.Should().BeTrue();
    }
}
