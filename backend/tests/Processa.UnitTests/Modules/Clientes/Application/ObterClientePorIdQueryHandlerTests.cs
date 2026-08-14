using FluentAssertions;
using NSubstitute;
using Processa.Modules.Clientes.Application.Clientes;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Clientes.Application;

public class ObterClientePorIdQueryHandlerTests
{
    private readonly IClienteRepository _clienteRepository = Substitute.For<IClienteRepository>();
    private readonly IContatoClienteRepository _contatoClienteRepository = Substitute.For<IContatoClienteRepository>();
    private readonly IResponsavelClienteRepository _responsavelClienteRepository = Substitute.For<IResponsavelClienteRepository>();

    private static readonly DateOnly DataEntrada = new(2026, 1, 1);

    [Fact]
    public async Task Handle_ClienteExiste_RetornaDetalheComContatosEResponsaveis()
    {
        var cliente = Cliente.Criar(Guid.NewGuid(), "Escritório LTDA", "11222333000181", null, null, RegimeTributario.Mei, DataEntrada).Value;
        var contato = ContatoCliente.Criar(cliente.TenantId, cliente.Id, "Maria", "maria@exemplo.com", null, null).Value;
        var responsavel = ResponsavelCliente.Criar(cliente.TenantId, cliente.Id, Guid.NewGuid(), Guid.NewGuid()).Value;

        _clienteRepository.ObterPorIdAsync(cliente.Id).Returns(cliente);
        _contatoClienteRepository.ListarPorClienteAsync(cliente.Id).Returns([contato]);
        _responsavelClienteRepository.ListarAtivosPorClienteAsync(cliente.Id).Returns([responsavel]);

        var handler = new ObterClientePorIdQueryHandler(_clienteRepository, _contatoClienteRepository, _responsavelClienteRepository);
        var resultado = await handler.Handle(new ObterClientePorIdQuery(cliente.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Contatos.Should().ContainSingle(c => c.Nome == "Maria");
        resultado.Value.Responsaveis.Should().ContainSingle(r => r.Id == responsavel.Id);
    }

    [Fact]
    public async Task Handle_ClienteNaoExiste_RetornaFalha()
    {
        _clienteRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Cliente?)null);
        var handler = new ObterClientePorIdQueryHandler(_clienteRepository, _contatoClienteRepository, _responsavelClienteRepository);

        var resultado = await handler.Handle(new ObterClientePorIdQuery(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}
