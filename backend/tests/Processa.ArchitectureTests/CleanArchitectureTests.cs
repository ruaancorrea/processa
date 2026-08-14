using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Processa.ArchitectureTests;

/// <summary>
/// Valida, para todo módulo, as regras de dependência do ADR-001
/// (docs/02-arquitetura/decisoes/adr-001-clean-architecture-modular-monolith.md):
///   Domain não depende de nada (nem de framework);
///   Application depende só de Domain (e do Shared.Kernel);
///   Infrastructure e Presentation dependem de Application, nunca o contrário.
/// Uma violação aqui quebra o CI — não depende de review manual.
/// </summary>
public class CleanArchitectureTests
{
    // Um marcador por módulo é suficiente para localizar o assembly inteiro do módulo.
    private static readonly Assembly[] ModuleAssemblies =
    [
        typeof(Processa.Modules.Identidade.IdentidadeModuleMarker).Assembly,
        typeof(Processa.Modules.Clientes.ClientesModuleMarker).Assembly,
        typeof(Processa.Modules.Processos.ProcessosModuleMarker).Assembly,
        typeof(Processa.Modules.Documentos.DocumentosModuleMarker).Assembly,
        typeof(Processa.Modules.Notificacoes.NotificacoesModuleMarker).Assembly,
        typeof(Processa.Modules.Portal.PortalModuleMarker).Assembly,
    ];

    public static IEnumerable<object[]> Modulos() => ModuleAssemblies.Select(a => new object[] { a });

    [Theory]
    [MemberData(nameof(Modulos))]
    public void Domain_NaoDependeDeApplication_Infrastructure_ouPresentation(Assembly moduleAssembly)
    {
        var result = Types.InAssembly(moduleAssembly)
            .That().ResideInNamespaceContaining(".Domain")
            .ShouldNot().HaveDependencyOnAny(
                NamespaceContaining(moduleAssembly, ".Application"),
                NamespaceContaining(moduleAssembly, ".Infrastructure"),
                NamespaceContaining(moduleAssembly, ".Presentation"))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatarFalhas(result));
    }

    [Theory]
    [MemberData(nameof(Modulos))]
    public void Domain_NaoDependeDeFrameworksExternos(Assembly moduleAssembly)
    {
        var result = Types.InAssembly(moduleAssembly)
            .That().ResideInNamespaceContaining(".Domain")
            .ShouldNot().HaveDependencyOnAny("MediatR", "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatarFalhas(result));
    }

    [Theory]
    [MemberData(nameof(Modulos))]
    public void Application_NaoDependeDeInfrastructure_ouPresentation(Assembly moduleAssembly)
    {
        var result = Types.InAssembly(moduleAssembly)
            .That().ResideInNamespaceContaining(".Application")
            .ShouldNot().HaveDependencyOnAny(
                NamespaceContaining(moduleAssembly, ".Infrastructure"),
                NamespaceContaining(moduleAssembly, ".Presentation"))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatarFalhas(result));
    }

    /// <summary>
    /// Nenhum módulo pode referenciar OUTRO módulo diretamente — nem Domain, nem
    /// Application, nem Infrastructure, nem Presentation. Comunicação entre módulos
    /// passa por uma interface em Processa.Shared.Kernel (implementada por quem
    /// possui a informação, consumida pelo outro lado só pela abstração) — ver
    /// IVerificadorMembroEquipe e .faf/decisions.faf (Sprint 2). Sem isso, é fácil
    /// um módulo "vazar" um ProjectReference pro outro por conveniência (ex.: reusar
    /// uma query via ISender), o que recria acoplamento de compilação que o desenho
    /// de monólito modular existe pra evitar.
    /// </summary>
    [Theory]
    [MemberData(nameof(Modulos))]
    public void Modulo_NaoDependeDeOutroModulo(Assembly moduleAssembly)
    {
        var namespaceRaiz = moduleAssembly.GetName().Name!;
        var outrosNamespaces = ModuleAssemblies
            .Select(a => a.GetName().Name!)
            .Where(nome => nome != namespaceRaiz)
            .ToArray();

        var result = Types.InAssembly(moduleAssembly)
            .ShouldNot().HaveDependencyOnAny(outrosNamespaces)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatarFalhas(result));
    }

    private static string NamespaceContaining(Assembly assembly, string suffix)
    {
        // Deriva o namespace raiz do módulo (ex: "Processa.Modules.Processos") a partir
        // do nome do assembly, evitando hardcode do namespace completo em cada regra.
        return assembly.GetName().Name + suffix;
    }

    private static string FormatarFalhas(TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : "Tipos que violam a regra: " + string.Join(", ", result.FailingTypeNames ?? []);
}
