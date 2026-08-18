using FluentAssertions;
using NSubstitute;
using Processa.Modules.Processos.Application;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Application.TiposProcesso;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Application;

public class CriarTipoProcessoCommandHandlerTests
{
    private readonly ITipoProcessoRepository _tipoProcessoRepository = Substitute.For<ITipoProcessoRepository>();
    private readonly IVerificadorEquipe _verificadorEquipe = Substitute.For<IVerificadorEquipe>();
    private readonly IVerificadorMembroEquipe _verificadorMembroEquipe = Substitute.For<IVerificadorMembroEquipe>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CriarTipoProcessoCommandHandler CriarHandler() =>
        new(_tipoProcessoRepository, _verificadorEquipe, _verificadorMembroEquipe, _tenantContext, _unitOfWork);

    public CriarTipoProcessoCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _verificadorEquipe.ExisteAsync(Arg.Any<Guid>()).Returns(true);
    }

    [Fact]
    public async Task Handle_DadosValidos_CriaTipoProcesso()
    {
        var resultado = await CriarHandler().Handle(
            new CriarTipoProcessoCommand(Guid.NewGuid(), "Abertura de empresa", null, true, ModoAtribuicao.Dinamico, null),
            CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _tipoProcessoRepository.Received(1).AddAsync(Arg.Any<TipoProcesso>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EquipeNaoExiste_RetornaFalha()
    {
        _verificadorEquipe.ExisteAsync(Arg.Any<Guid>()).Returns(false);

        var resultado = await CriarHandler().Handle(
            new CriarTipoProcessoCommand(Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Manual, null),
            CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        await _tipoProcessoRepository.DidNotReceive().AddAsync(Arg.Any<TipoProcesso>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ModoFixoResponsavelNaoEhMembro_RetornaFalha()
    {
        _verificadorMembroEquipe.EhMembroAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);

        var resultado = await CriarHandler().Handle(
            new CriarTipoProcessoCommand(Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Fixo, Guid.NewGuid()),
            CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ModoFixoResponsavelEhMembro_CriaTipoProcesso()
    {
        _verificadorMembroEquipe.EhMembroAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(true);

        var resultado = await CriarHandler().Handle(
            new CriarTipoProcessoCommand(Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Fixo, Guid.NewGuid()),
            CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ModoFixoSemResponsavelInformado_RetornaFalha()
    {
        var resultado = await CriarHandler().Handle(
            new CriarTipoProcessoCommand(Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Fixo, null),
            CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        await _verificadorMembroEquipe.DidNotReceive().EhMembroAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }
}
