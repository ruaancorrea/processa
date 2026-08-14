using FluentAssertions;
using Processa.Modules.Clientes.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Clientes.Domain;

public class ClienteTests
{
    private static readonly DateOnly DataEntrada = new(2026, 1, 1);

    [Fact]
    public void Criar_DadosValidos_RetornaSucessoEAtivo()
    {
        var resultado = Cliente.Criar(
            Guid.NewGuid(), "Escritório Contábil LTDA", "11222333000181", "COD-1", null, RegimeTributario.SimplesNacional, DataEntrada);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Status.Should().Be(StatusCliente.Ativo);
        resultado.Value.Cnpj.Numero.Should().Be("11222333000181");
    }

    [Fact]
    public void Criar_CnpjInvalido_RetornaFalha()
    {
        var resultado = Cliente.Criar(
            Guid.NewGuid(), "Escritório Contábil LTDA", "123", null, null, RegimeTributario.SimplesNacional, DataEntrada);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_RazaoSocialVazia_RetornaFalha()
    {
        var resultado = Cliente.Criar(
            Guid.NewGuid(), "", "11222333000181", null, null, RegimeTributario.SimplesNacional, DataEntrada);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void SuspenderInativarReativar_AlteramStatus()
    {
        var cliente = Cliente.Criar(
            Guid.NewGuid(), "Escritório LTDA", "11222333000181", null, null, RegimeTributario.Mei, DataEntrada).Value;

        cliente.Suspender();
        cliente.Status.Should().Be(StatusCliente.Suspenso);

        cliente.Inativar();
        cliente.Status.Should().Be(StatusCliente.Inativo);

        cliente.Reativar();
        cliente.Status.Should().Be(StatusCliente.Ativo);
    }

    [Fact]
    public void AtualizarDados_RazaoSocialValida_Atualiza()
    {
        var cliente = Cliente.Criar(
            Guid.NewGuid(), "Nome Antigo", "11222333000181", null, null, RegimeTributario.Mei, DataEntrada).Value;

        var resultado = cliente.AtualizarDados("Nome Novo", "COD-2", null, RegimeTributario.LucroReal);

        resultado.IsSuccess.Should().BeTrue();
        cliente.RazaoSocial.Should().Be("Nome Novo");
        cliente.RegimeTributario.Should().Be(RegimeTributario.LucroReal);
    }
}
