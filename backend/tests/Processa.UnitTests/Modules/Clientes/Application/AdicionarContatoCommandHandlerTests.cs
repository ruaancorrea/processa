using FluentAssertions;
using NSubstitute;
using Processa.Modules.Clientes.Application;
using Processa.Modules.Clientes.Application.Contatos;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Clientes.Application;

public class AdicionarContatoCommandHandlerTests
{
    private readonly IContatoClienteRepository _contatoClienteRepository = Substitute.For<IContatoClienteRepository>();
    private readonly IClienteRepository _clienteRepository = Substitute.For<IClienteRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private AdicionarContatoCommandHandler CriarHandler() =>
        new(_contatoClienteRepository, _clienteRepository, _tenantContext, _unitOfWork);

    private static Cliente CriarCliente() =>
        Cliente.Criar(Guid.NewGuid(), "Escritório LTDA", "11222333000181", null, null, RegimeTributario.Mei, new DateOnly(2026, 1, 1)).Value;

    [Fact]
    public async Task Handle_ClienteExiste_AdicionaContato()
    {
        var cliente = CriarCliente();
        _clienteRepository.ObterPorIdAsync(cliente.Id).Returns(cliente);
        _tenantContext.TenantId.Returns(Guid.NewGuid());

        var resultado = await CriarHandler().Handle(
            new AdicionarContatoCommand(cliente.Id, "Maria", "maria@exemplo.com", null, null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _contatoClienteRepository.Received(1).AddAsync(Arg.Any<ContatoCliente>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ClienteNaoEncontrado_RetornaFalha()
    {
        _clienteRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Cliente?)null);

        var resultado = await CriarHandler().Handle(
            new AdicionarContatoCommand(Guid.NewGuid(), "Maria", "maria@exemplo.com", null, null), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}
