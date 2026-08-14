using FluentAssertions;
using NSubstitute;
using Processa.Modules.Identidade.Application;
using Processa.Modules.Identidade.Application.Equipes;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Identidade.Application;

public class EquipeCommandHandlersTests
{
    private readonly IEquipeRepository _equipeRepository = Substitute.For<IEquipeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static Equipe CriarEquipe() => Equipe.Criar(Guid.NewGuid(), "Equipe Fiscal", null).Value;

    [Fact]
    public async Task AtualizarEquipe_EquipeExiste_Atualiza()
    {
        var equipe = CriarEquipe();
        _equipeRepository.ObterPorIdAsync(equipe.Id).Returns(equipe);
        var handler = new AtualizarEquipeCommandHandler(_equipeRepository, _unitOfWork);

        var resultado = await handler.Handle(new AtualizarEquipeCommand(equipe.Id, "Novo Nome", null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        equipe.Nome.Should().Be("Novo Nome");
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AtualizarEquipe_EquipeNaoExiste_RetornaFalha()
    {
        _equipeRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Equipe?)null);
        var handler = new AtualizarEquipeCommandHandler(_equipeRepository, _unitOfWork);

        var resultado = await handler.Handle(new AtualizarEquipeCommand(Guid.NewGuid(), "Novo Nome", null), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task DesativarEquipe_EquipeExiste_Desativa()
    {
        var equipe = CriarEquipe();
        _equipeRepository.ObterPorIdAsync(equipe.Id).Returns(equipe);
        var handler = new DesativarEquipeCommandHandler(_equipeRepository, _unitOfWork);

        var resultado = await handler.Handle(new DesativarEquipeCommand(equipe.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        equipe.Ativa.Should().BeFalse();
    }

    [Fact]
    public async Task DesativarEquipe_EquipeNaoExiste_RetornaFalha()
    {
        _equipeRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Equipe?)null);
        var handler = new DesativarEquipeCommandHandler(_equipeRepository, _unitOfWork);

        var resultado = await handler.Handle(new DesativarEquipeCommand(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ReativarEquipe_EquipeExiste_Reativa()
    {
        var equipe = CriarEquipe();
        equipe.Desativar();
        _equipeRepository.ObterPorIdAsync(equipe.Id).Returns(equipe);
        var handler = new ReativarEquipeCommandHandler(_equipeRepository, _unitOfWork);

        var resultado = await handler.Handle(new ReativarEquipeCommand(equipe.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        equipe.Ativa.Should().BeTrue();
    }

    [Fact]
    public async Task ReativarEquipe_EquipeNaoExiste_RetornaFalha()
    {
        _equipeRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Equipe?)null);
        var handler = new ReativarEquipeCommandHandler(_equipeRepository, _unitOfWork);

        var resultado = await handler.Handle(new ReativarEquipeCommand(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ListarEquipes_RetornaResumoDeTodasAsEquipes()
    {
        var equipe = CriarEquipe();
        _equipeRepository.ListarAsync().Returns([equipe]);
        var handler = new ListarEquipesQueryHandler(_equipeRepository);

        var resultado = await handler.Handle(new ListarEquipesQuery(), CancellationToken.None);

        resultado.Should().ContainSingle(e => e.Id == equipe.Id && e.Nome == equipe.Nome);
    }
}
