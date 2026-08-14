using FluentAssertions;
using Processa.Modules.Clientes.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Clientes.Domain;

public class ContatoClienteTests
{
    [Fact]
    public void Criar_ComEmail_RetornaSucessoEAtivo()
    {
        var resultado = ContatoCliente.Criar(Guid.NewGuid(), Guid.NewGuid(), "Maria Silva", "maria@exemplo.com", null, null);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Ativo.Should().BeTrue();
        resultado.Value.Email!.Valor.Should().Be("maria@exemplo.com");
    }

    [Fact]
    public void Criar_SoComTelefone_RetornaSucesso()
    {
        var resultado = ContatoCliente.Criar(Guid.NewGuid(), Guid.NewGuid(), "Maria Silva", null, "1140028922", null);

        resultado.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Criar_SemNenhumMeioDeContato_RetornaFalha()
    {
        var resultado = ContatoCliente.Criar(Guid.NewGuid(), Guid.NewGuid(), "Maria Silva", null, null, null);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_EmailInvalido_RetornaFalha()
    {
        var resultado = ContatoCliente.Criar(Guid.NewGuid(), Guid.NewGuid(), "Maria Silva", "nao-eh-email", null, null);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_NomeVazio_RetornaFalha()
    {
        var resultado = ContatoCliente.Criar(Guid.NewGuid(), Guid.NewGuid(), "", "maria@exemplo.com", null, null);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void DesativarEReativar_AlternaEstado()
    {
        var contato = ContatoCliente.Criar(Guid.NewGuid(), Guid.NewGuid(), "Maria Silva", "maria@exemplo.com", null, null).Value;

        contato.Desativar();
        contato.Ativo.Should().BeFalse();

        contato.Reativar();
        contato.Ativo.Should().BeTrue();
    }
}
