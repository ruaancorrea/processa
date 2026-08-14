using FluentAssertions;
using NSubstitute;
using Processa.Modules.Clientes.Application;
using Processa.Modules.Clientes.Application.Clientes;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Clientes.Application;

public class ClienteStatusEQueryTests
{
    private readonly IClienteRepository _clienteRepository = Substitute.For<IClienteRepository>();
    private readonly IGrupoClienteRepository _grupoClienteRepository = Substitute.For<IGrupoClienteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static readonly DateOnly DataEntrada = new(2026, 1, 1);

    private static Cliente CriarCliente() =>
        Cliente.Criar(Guid.NewGuid(), "Escritório LTDA", "11222333000181", null, null, RegimeTributario.Mei, DataEntrada).Value;

    [Fact]
    public async Task AtualizarCliente_ClienteExiste_Atualiza()
    {
        var cliente = CriarCliente();
        _clienteRepository.ObterPorIdAsync(cliente.Id).Returns(cliente);
        var handler = new AtualizarClienteCommandHandler(_clienteRepository, _grupoClienteRepository, _unitOfWork);

        var resultado = await handler.Handle(
            new AtualizarClienteCommand(cliente.Id, "Novo Nome", null, null, RegimeTributario.LucroReal), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        cliente.RazaoSocial.Should().Be("Novo Nome");
    }

    [Fact]
    public async Task AtualizarCliente_ClienteNaoExiste_RetornaFalha()
    {
        _clienteRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Cliente?)null);
        var handler = new AtualizarClienteCommandHandler(_clienteRepository, _grupoClienteRepository, _unitOfWork);

        var resultado = await handler.Handle(
            new AtualizarClienteCommand(Guid.NewGuid(), "Novo Nome", null, null, RegimeTributario.LucroReal), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task SuspenderCliente_ClienteExiste_Suspende()
    {
        var cliente = CriarCliente();
        _clienteRepository.ObterPorIdAsync(cliente.Id).Returns(cliente);
        var handler = new SuspenderClienteCommandHandler(_clienteRepository, _unitOfWork);

        var resultado = await handler.Handle(new SuspenderClienteCommand(cliente.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        cliente.Status.Should().Be(StatusCliente.Suspenso);
    }

    [Fact]
    public async Task SuspenderCliente_ClienteNaoExiste_RetornaFalha()
    {
        _clienteRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Cliente?)null);
        var handler = new SuspenderClienteCommandHandler(_clienteRepository, _unitOfWork);

        var resultado = await handler.Handle(new SuspenderClienteCommand(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task InativarCliente_ClienteExiste_Inativa()
    {
        var cliente = CriarCliente();
        _clienteRepository.ObterPorIdAsync(cliente.Id).Returns(cliente);
        var handler = new InativarClienteCommandHandler(_clienteRepository, _unitOfWork);

        var resultado = await handler.Handle(new InativarClienteCommand(cliente.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        cliente.Status.Should().Be(StatusCliente.Inativo);
    }

    [Fact]
    public async Task ReativarCliente_ClienteExiste_Reativa()
    {
        var cliente = CriarCliente();
        cliente.Suspender();
        _clienteRepository.ObterPorIdAsync(cliente.Id).Returns(cliente);
        var handler = new ReativarClienteCommandHandler(_clienteRepository, _unitOfWork);

        var resultado = await handler.Handle(new ReativarClienteCommand(cliente.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        cliente.Status.Should().Be(StatusCliente.Ativo);
    }

    [Fact]
    public async Task ListarClientes_RetornaResumoDeTodos()
    {
        var cliente = CriarCliente();
        _clienteRepository.ListarAsync().Returns([cliente]);
        var handler = new ListarClientesQueryHandler(_clienteRepository);

        var resultado = await handler.Handle(new ListarClientesQuery(), CancellationToken.None);

        resultado.Should().ContainSingle(c => c.Id == cliente.Id && c.Cnpj == "11222333000181");
    }
}
