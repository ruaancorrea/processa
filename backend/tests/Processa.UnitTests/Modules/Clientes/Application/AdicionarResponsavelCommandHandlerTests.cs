using FluentAssertions;
using NSubstitute;
using Processa.Modules.Clientes.Application;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Application.Responsaveis;
using Processa.Modules.Clientes.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Clientes.Application;

public class AdicionarResponsavelCommandHandlerTests
{
    private readonly IResponsavelClienteRepository _responsavelClienteRepository = Substitute.For<IResponsavelClienteRepository>();
    private readonly IClienteRepository _clienteRepository = Substitute.For<IClienteRepository>();
    private readonly IVerificadorMembroEquipe _verificadorMembroEquipe = Substitute.For<IVerificadorMembroEquipe>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private AdicionarResponsavelCommandHandler CriarHandler() => new(
        _responsavelClienteRepository, _clienteRepository, _verificadorMembroEquipe, _tenantContext, _unitOfWork);

    private static Cliente CriarCliente() =>
        Cliente.Criar(Guid.NewGuid(), "Escritório LTDA", "11222333000181", null, null, RegimeTributario.Mei, new DateOnly(2026, 1, 1)).Value;

    [Fact]
    public async Task Handle_UsuarioEhMembroDaEquipe_AdicionaResponsavel()
    {
        var cliente = CriarCliente();
        var equipeId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        _clienteRepository.ObterPorIdAsync(cliente.Id).Returns(cliente);
        _verificadorMembroEquipe.EhMembroAsync(equipeId, usuarioId).Returns(true);
        _responsavelClienteRepository.ObterAsync(cliente.Id, equipeId, usuarioId).Returns((ResponsavelCliente?)null);
        _tenantContext.TenantId.Returns(Guid.NewGuid());

        var resultado = await CriarHandler().Handle(
            new AdicionarResponsavelCommand(cliente.Id, equipeId, usuarioId), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _responsavelClienteRepository.Received(1).AddAsync(Arg.Any<ResponsavelCliente>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UsuarioNaoEhMembroDaEquipe_RetornaFalha()
    {
        var cliente = CriarCliente();
        _clienteRepository.ObterPorIdAsync(cliente.Id).Returns(cliente);
        _verificadorMembroEquipe.EhMembroAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);

        var resultado = await CriarHandler().Handle(
            new AdicionarResponsavelCommand(cliente.Id, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        await _responsavelClienteRepository.DidNotReceive().AddAsync(Arg.Any<ResponsavelCliente>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_VinculoJaAtivoExistente_RetornaFalha()
    {
        var cliente = CriarCliente();
        var equipeId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        _clienteRepository.ObterPorIdAsync(cliente.Id).Returns(cliente);
        _verificadorMembroEquipe.EhMembroAsync(equipeId, usuarioId).Returns(true);
        var existente = ResponsavelCliente.Criar(Guid.NewGuid(), cliente.Id, equipeId, usuarioId).Value;
        _responsavelClienteRepository.ObterAsync(cliente.Id, equipeId, usuarioId).Returns(existente);

        var resultado = await CriarHandler().Handle(
            new AdicionarResponsavelCommand(cliente.Id, equipeId, usuarioId), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_VinculoRemovidoAnteriormente_Reativa()
    {
        var cliente = CriarCliente();
        var equipeId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        _clienteRepository.ObterPorIdAsync(cliente.Id).Returns(cliente);
        _verificadorMembroEquipe.EhMembroAsync(equipeId, usuarioId).Returns(true);
        var existente = ResponsavelCliente.Criar(Guid.NewGuid(), cliente.Id, equipeId, usuarioId).Value;
        existente.Remover();
        _responsavelClienteRepository.ObterAsync(cliente.Id, equipeId, usuarioId).Returns(existente);

        var resultado = await CriarHandler().Handle(
            new AdicionarResponsavelCommand(cliente.Id, equipeId, usuarioId), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        existente.EstaAtivo.Should().BeTrue();
        await _responsavelClienteRepository.DidNotReceive().AddAsync(Arg.Any<ResponsavelCliente>(), Arg.Any<CancellationToken>());
    }
}
