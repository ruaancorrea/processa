using FluentAssertions;
using NSubstitute;
using Processa.Modules.Identidade.Application;
using Processa.Modules.Identidade.Application.Equipes;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Identidade.Application;

public class RemoverMembroCommandHandlerTests
{
    private readonly IMembroEquipeRepository _membroEquipeRepository = Substitute.For<IMembroEquipeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_MembroExiste_Remove()
    {
        var membro = MembroEquipe.Adicionar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Perfil.Analista).Value;
        _membroEquipeRepository.ObterAsync(membro.EquipeId, membro.UsuarioId).Returns(membro);
        var handler = new RemoverMembroCommandHandler(_membroEquipeRepository, _unitOfWork);

        var resultado = await handler.Handle(new RemoverMembroCommand(membro.EquipeId, membro.UsuarioId), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _membroEquipeRepository.Received(1).Remover(membro);
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MembroNaoExiste_RetornaFalha()
    {
        _membroEquipeRepository.ObterAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns((MembroEquipe?)null);
        var handler = new RemoverMembroCommandHandler(_membroEquipeRepository, _unitOfWork);

        var resultado = await handler.Handle(new RemoverMembroCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}

public class ObterEquipePorIdQueryHandlerTests
{
    private readonly IEquipeRepository _equipeRepository = Substitute.For<IEquipeRepository>();
    private readonly IMembroEquipeRepository _membroEquipeRepository = Substitute.For<IMembroEquipeRepository>();
    private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();

    [Fact]
    public async Task Handle_EquipeExiste_RetornaDetalheComMembros()
    {
        var equipe = Equipe.Criar(Guid.NewGuid(), "Equipe Fiscal", null).Value;
        var usuario = Usuario.Criar(equipe.TenantId, "Ana", "ana@exemplo.com", "hash", Perfil.Analista).Value;
        var membro = MembroEquipe.Adicionar(equipe.TenantId, equipe.Id, usuario.Id, Perfil.Analista).Value;

        _equipeRepository.ObterPorIdAsync(equipe.Id).Returns(equipe);
        _membroEquipeRepository.ListarPorEquipeAsync(equipe.Id).Returns([membro]);
        _usuarioRepository.ObterPorIdAsync(usuario.Id).Returns(usuario);

        var handler = new ObterEquipePorIdQueryHandler(_equipeRepository, _membroEquipeRepository, _usuarioRepository);
        var resultado = await handler.Handle(new ObterEquipePorIdQuery(equipe.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Membros.Should().ContainSingle(m => m.UsuarioId == usuario.Id && m.NomeUsuario == "Ana");
    }

    [Fact]
    public async Task Handle_EquipeNaoExiste_RetornaFalha()
    {
        _equipeRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Equipe?)null);
        var handler = new ObterEquipePorIdQueryHandler(_equipeRepository, _membroEquipeRepository, _usuarioRepository);

        var resultado = await handler.Handle(new ObterEquipePorIdQuery(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}
