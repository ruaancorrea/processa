using FluentAssertions;
using NSubstitute;
using Processa.Modules.Clientes.Application;
using Processa.Modules.Clientes.Application.Clientes;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Clientes.Application;

public class CriarClienteCommandHandlerTests
{
    private readonly IClienteRepository _clienteRepository = Substitute.For<IClienteRepository>();
    private readonly IGrupoClienteRepository _grupoClienteRepository = Substitute.For<IGrupoClienteRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CriarClienteCommandHandler CriarHandler() =>
        new(_clienteRepository, _grupoClienteRepository, _tenantContext, _unitOfWork);

    private static readonly DateOnly DataEntrada = new(2026, 1, 1);

    [Fact]
    public async Task Handle_DadosValidos_CriaCliente()
    {
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _clienteRepository.ExisteCnpjAsync(Arg.Any<Cnpj>()).Returns(false);

        var resultado = await CriarHandler().Handle(
            new CriarClienteCommand("Escritório LTDA", "11222333000181", null, null, RegimeTributario.SimplesNacional, DataEntrada),
            CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _clienteRepository.Received(1).AddAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CnpjJaCadastrado_RetornaFalha()
    {
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _clienteRepository.ExisteCnpjAsync(Arg.Any<Cnpj>()).Returns(true);

        var resultado = await CriarHandler().Handle(
            new CriarClienteCommand("Escritório LTDA", "11222333000181", null, null, RegimeTributario.SimplesNacional, DataEntrada),
            CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        await _clienteRepository.DidNotReceive().AddAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GrupoInformadoNaoExiste_RetornaFalha()
    {
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _clienteRepository.ExisteCnpjAsync(Arg.Any<Cnpj>()).Returns(false);
        var grupoId = Guid.NewGuid();
        _grupoClienteRepository.ObterPorIdAsync(grupoId).Returns((GrupoCliente?)null);

        var resultado = await CriarHandler().Handle(
            new CriarClienteCommand("Escritório LTDA", "11222333000181", null, grupoId, RegimeTributario.SimplesNacional, DataEntrada),
            CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}
