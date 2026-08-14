using FluentAssertions;
using NSubstitute;
using Processa.Modules.Identidade.Application;
using Processa.Modules.Identidade.Application.Equipes;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Identidade.Application;

public class CriarEquipeCommandHandlerTests
{
    private readonly IEquipeRepository _equipeRepository = Substitute.For<IEquipeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    [Fact]
    public async Task Handle_DadosValidos_CriaEquipeNoTenantDoContexto()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        var handler = new CriarEquipeCommandHandler(_equipeRepository, _unitOfWork, _tenantContext);

        var resultado = await handler.Handle(new CriarEquipeCommand("Equipe Fiscal", null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _equipeRepository.Received(1).AddAsync(
            Arg.Is<Equipe>(e => e.TenantId == tenantId && e.Nome == "Equipe Fiscal"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NomeVazio_NaoPersisteERetornaFalha()
    {
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        var handler = new CriarEquipeCommandHandler(_equipeRepository, _unitOfWork, _tenantContext);

        var resultado = await handler.Handle(new CriarEquipeCommand("", null), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        await _equipeRepository.DidNotReceive().AddAsync(Arg.Any<Equipe>(), Arg.Any<CancellationToken>());
    }
}
