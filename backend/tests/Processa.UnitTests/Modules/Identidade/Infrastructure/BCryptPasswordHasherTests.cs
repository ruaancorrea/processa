using FluentAssertions;
using Processa.Modules.Identidade.Infrastructure;
using Xunit;

namespace Processa.UnitTests.Modules.Identidade.Infrastructure;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_MesmaSenhaDuasVezes_GeraHashesDiferentes()
    {
        // bcrypt usa salt aleatório — hashes do mesmo texto nunca devem ser iguais.
        var hash1 = _hasher.Hash("minhaSenha123");
        var hash2 = _hasher.Hash("minhaSenha123");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verificar_SenhaCorreta_RetornaTrue()
    {
        var hash = _hasher.Hash("minhaSenha123");

        _hasher.Verificar("minhaSenha123", hash).Should().BeTrue();
    }

    [Fact]
    public void Verificar_SenhaErrada_RetornaFalse()
    {
        var hash = _hasher.Hash("minhaSenha123");

        _hasher.Verificar("senha-diferente", hash).Should().BeFalse();
    }
}
