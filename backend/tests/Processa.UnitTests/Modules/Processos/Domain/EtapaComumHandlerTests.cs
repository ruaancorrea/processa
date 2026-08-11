using FluentAssertions;
using Processa.Modules.Processos.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Domain;

public class EtapaComumHandlerTests
{
    [Fact]
    public async Task ExecutarAsync_SempreConcluiImediatamente()
    {
        // Arrange
        var handler = new EtapaComumHandler();
        var contexto = new ExecucaoEtapaContexto(
            TenantId: Guid.NewGuid(),
            DemandaId: Guid.NewGuid(),
            ExecucaoEtapaId: Guid.NewGuid(),
            ConfiguracaoJson: "{}");

        // Act
        var resultado = await handler.ExecutarAsync(contexto);

        // Assert
        resultado.Desfecho.Should().Be(DesfechoExecucao.Concluida);
    }

    [Fact]
    public void Tipo_EhComum()
    {
        new EtapaComumHandler().Tipo.Should().Be(TipoEtapa.Comum);
    }
}
