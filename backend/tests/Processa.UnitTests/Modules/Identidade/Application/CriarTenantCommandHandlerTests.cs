using FluentAssertions;
using NSubstitute;
using Processa.Modules.Identidade.Application;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Application.Tenants;
using Processa.Modules.Identidade.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Identidade.Application;

public class CriarTenantCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CriarTenantCommandHandler CriarHandler() =>
        new(_tenantRepository, _usuarioRepository, _passwordHasher, _unitOfWork);

    private static CriarTenantCommand ComandoValido() => new(
        "Escritório Teste", "11.222.333/0001-81", "Ana Admin", "ana@escritorio.com", "senhaForte123");

    [Fact]
    public async Task Handle_DadosValidos_CriaTenantEUsuarioAdmin()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hash-fake");

        var resultado = await CriarHandler().Handle(ComandoValido(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _tenantRepository.Received(1).AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
        await _usuarioRepository.Received(1).AddAsync(
            Arg.Is<Usuario>(u => u.Perfil == Perfil.Admin), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CnpjJaCadastrado_RetornaFalhaSemPersistirNada()
    {
        _tenantRepository.ExisteCnpjAsync(Arg.Any<Cnpj>()).Returns(true);

        var resultado = await CriarHandler().Handle(ComandoValido(), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        await _tenantRepository.DidNotReceive().AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailJaCadastrado_RetornaFalhaSemPersistirNada()
    {
        _usuarioRepository.ExisteEmailAsync(Arg.Any<Email>()).Returns(true);

        var resultado = await CriarHandler().Handle(ComandoValido(), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        await _tenantRepository.DidNotReceive().AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CnpjInvalido_RetornaFalhaSemConsultarRepositorio()
    {
        var comando = ComandoValido() with { Cnpj = "123" };

        var resultado = await CriarHandler().Handle(comando, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        await _tenantRepository.DidNotReceive().ExisteCnpjAsync(Arg.Any<Cnpj>(), Arg.Any<CancellationToken>());
    }
}
