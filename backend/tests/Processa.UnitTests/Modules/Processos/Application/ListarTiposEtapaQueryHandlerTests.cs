using FluentAssertions;
using Processa.Modules.Processos.Application.Etapas;
using Processa.Modules.Processos.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Application;

public class ListarTiposEtapaQueryHandlerTests
{
    [Fact]
    public async Task Handle_RetornaOsOitoTiposDeEtapa()
    {
        var handler = new ListarTiposEtapaQueryHandler();

        var resultado = await handler.Handle(new ListarTiposEtapaQuery(), CancellationToken.None);

        resultado.Should().HaveCount(8);
        resultado.Should().Contain(TipoEtapa.Uniao);
    }
}
