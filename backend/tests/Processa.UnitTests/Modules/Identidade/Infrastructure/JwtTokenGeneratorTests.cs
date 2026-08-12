using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Processa.Modules.Identidade.Domain;
using Processa.Modules.Identidade.Infrastructure;
using Xunit;

namespace Processa.UnitTests.Modules.Identidade.Infrastructure;

public class JwtTokenGeneratorTests
{
    private readonly JwtTokenGenerator _generator = new(Options.Create(new JwtOptions
    {
        Issuer = "processa-testes",
        Audience = "processa-testes-app",
        SecretKey = "chave-de-teste-nao-usar-em-lugar-nenhum-real-32chars",
        AccessTokenMinutos = 15,
    }));

    [Fact]
    public void GerarAccessToken_ContemAsClaimsEsperadas()
    {
        var usuario = Usuario.Criar(Guid.NewGuid(), "Ana", "ana@exemplo.com", "hash", Perfil.Gestor).Value;

        var token = _generator.GerarAccessToken(usuario);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);

        jwt.Claims.Should().Contain(c => c.Type == "tenant_id" && c.Value == usuario.TenantId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "perfil" && c.Value == "Gestor");
        jwt.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Gestor");
        jwt.Issuer.Should().Be("processa-testes");
        token.ExpiraEm.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GerarRefreshTokenOpaco_GeraValoresDiferentesACadaChamada()
    {
        var token1 = _generator.GerarRefreshTokenOpaco();
        var token2 = _generator.GerarRefreshTokenOpaco();

        token1.Should().NotBe(token2);
    }

    [Fact]
    public void HashDoRefreshToken_EhDeterministico_MesmaEntradaMesmoHash()
    {
        var hash1 = _generator.HashDoRefreshToken("token-opaco-fixo");
        var hash2 = _generator.HashDoRefreshToken("token-opaco-fixo");

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashDoRefreshToken_NuncaArmazenaOTextoPuro()
    {
        var hash = _generator.HashDoRefreshToken("token-opaco-fixo");

        hash.Should().NotBe("token-opaco-fixo");
    }
}
