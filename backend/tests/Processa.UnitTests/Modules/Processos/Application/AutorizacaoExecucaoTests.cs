using FluentAssertions;
using NSubstitute;
using Processa.Modules.Processos.Application.Demandas;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Application;

public class AutorizacaoExecucaoTests
{
    private static ExecucaoEtapa CriarExecucao(Guid? responsavelId) =>
        ExecucaoEtapa.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), responsavelId).Value;

    private static IUsuarioContext CriarContexto(Guid usuarioId, Perfil perfil)
    {
        var contexto = Substitute.For<IUsuarioContext>();
        contexto.UsuarioId.Returns(usuarioId);
        contexto.Perfil.Returns(perfil);
        return contexto;
    }

    [Fact]
    public void PodeInteragir_Admin_SempreAutorizado()
    {
        var execucao = CriarExecucao(Guid.NewGuid());
        var contexto = CriarContexto(Guid.NewGuid(), Perfil.Admin);

        AutorizacaoExecucao.PodeInteragir(contexto, execucao).Should().BeTrue();
    }

    [Fact]
    public void PodeInteragir_Gestor_SempreAutorizado()
    {
        var execucao = CriarExecucao(Guid.NewGuid());
        var contexto = CriarContexto(Guid.NewGuid(), Perfil.Gestor);

        AutorizacaoExecucao.PodeInteragir(contexto, execucao).Should().BeTrue();
    }

    [Fact]
    public void PodeInteragir_AnalistaResponsavelPelaExecucao_Autorizado()
    {
        var usuarioId = Guid.NewGuid();
        var execucao = CriarExecucao(usuarioId);
        var contexto = CriarContexto(usuarioId, Perfil.Analista);

        AutorizacaoExecucao.PodeInteragir(contexto, execucao).Should().BeTrue();
    }

    [Fact]
    public void PodeInteragir_AnalistaNaoResponsavel_NaoAutorizado()
    {
        var execucao = CriarExecucao(Guid.NewGuid());
        var contexto = CriarContexto(Guid.NewGuid(), Perfil.Analista);

        AutorizacaoExecucao.PodeInteragir(contexto, execucao).Should().BeFalse();
    }

    [Fact]
    public void PodeInteragir_AnalistaSemResponsavelDefinido_NaoAutorizado()
    {
        var execucao = CriarExecucao(null);
        var contexto = CriarContexto(Guid.NewGuid(), Perfil.Analista);

        AutorizacaoExecucao.PodeInteragir(contexto, execucao).Should().BeFalse();
    }
}
