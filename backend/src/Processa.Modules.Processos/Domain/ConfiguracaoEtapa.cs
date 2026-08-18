using System.Text.Json.Serialization;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Configuração específica de cada tipo de etapa — um dos 7 tipos que exigem
/// configuração (Comum e Conclusão não exigem nenhuma, ver Etapa.Criar). Os atributos
/// de polimorfismo (System.Text.Json, parte do BCL — não fere a regra de Domain sem
/// framework externo) permitem bind direto do corpo JSON da requisição na Presentation
/// e serialização/desserialização direta na Infrastructure (HasConversion), sem um DTO
/// achatado com um campo por tipo. Deserializar direto num tipo concreto (como os
/// handlers fazem via ExecucaoEtapaContexto.ConfiguracaoJson) ignora o discriminador
/// "$type" normalmente — não há conflito entre os dois usos.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "tipo", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(ConfiguracaoEtapaCondicional), nameof(TipoEtapa.Condicional))]
[JsonDerivedType(typeof(ConfiguracaoEtapaAutomatizada), nameof(TipoEtapa.Automatizada))]
[JsonDerivedType(typeof(ConfiguracaoEtapaNotificacao), nameof(TipoEtapa.Notificacao))]
[JsonDerivedType(typeof(ConfiguracaoEtapaAgendamento), nameof(TipoEtapa.Agendamento))]
[JsonDerivedType(typeof(ConfiguracaoEtapaSubprocesso), nameof(TipoEtapa.Subprocesso))]
[JsonDerivedType(typeof(ConfiguracaoEtapaUniao), nameof(TipoEtapa.Uniao))]
public abstract record ConfiguracaoEtapa
{
    public abstract Result Validar();
}

public sealed record RamoCondicional(Guid CampoPersonalizadoId, OperadorCondicional Operador, string Valor, Guid EtapaDestinoId);

/// <summary>Etapa Condicional (PROJ-46) — desdobra o fluxo por valor de campo personalizado.</summary>
public sealed record ConfiguracaoEtapaCondicional(IReadOnlyList<RamoCondicional> Ramos, Guid EtapaPadraoId) : ConfiguracaoEtapa
{
    public override Result Validar()
    {
        if (Ramos.Count == 0)
            return Result.Failure("Etapa condicional exige ao menos um ramo.");
        if (EtapaPadraoId == Guid.Empty)
            return Result.Failure("Etapa condicional exige uma etapa padrão (quando nenhum ramo casa).");
        if (Ramos.Any(r => r.CampoPersonalizadoId == Guid.Empty || r.EtapaDestinoId == Guid.Empty))
            return Result.Failure("Todo ramo condicional exige campo personalizado e etapa de destino.");

        return Result.Success();
    }
}

/// <summary>Etapa Automatizada (PROJ-47) — chamada HTTP real, validada por status esperado.</summary>
public sealed record ConfiguracaoEtapaAutomatizada(string Url, MetodoHttp Metodo, string? CorpoTemplate, int StatusHttpEsperado) : ConfiguracaoEtapa
{
    public override Result Validar()
    {
        if (!Uri.IsWellFormedUriString(Url, UriKind.Absolute))
            return Result.Failure("Etapa automatizada exige uma URL absoluta válida.");
        if (StatusHttpEsperado is < 100 or > 599)
            return Result.Failure("Status HTTP esperado precisa ser um código HTTP válido (100–599).");

        return Result.Success();
    }
}

/// <summary>
/// Etapa de Notificação (PROJ-48) — o envio de verdade é do módulo Notificações
/// (Sprint 8); aqui só a configuração de quem/como/o quê.
/// </summary>
public sealed record ConfiguracaoEtapaNotificacao(DestinatarioNotificacao Destinatario, CanalNotificacao Canal, string Mensagem) : ConfiguracaoEtapa
{
    public override Result Validar()
    {
        if (string.IsNullOrWhiteSpace(Mensagem))
            return Result.Failure("Etapa de notificação exige uma mensagem.");

        return Result.Success();
    }
}

/// <summary>
/// Etapa de Agendamento (PROJ-48) — aguarda um número de dias a partir do início
/// da execução antes de concluir automaticamente.
/// </summary>
public sealed record ConfiguracaoEtapaAgendamento(int DiasOffset) : ConfiguracaoEtapa
{
    public override Result Validar()
    {
        if (DiasOffset < 0)
            return Result.Failure("Dias de espera do agendamento não pode ser negativo.");

        return Result.Success();
    }
}

/// <summary>
/// Etapa de Subprocesso (PROJ-49) — dispara um processo filho, com herança
/// opcional do responsável do processo pai (cliente é sempre herdado).
/// </summary>
public sealed record ConfiguracaoEtapaSubprocesso(Guid TipoProcessoFilhoId, bool HerdarResponsavel) : ConfiguracaoEtapa
{
    public override Result Validar()
    {
        if (TipoProcessoFilhoId == Guid.Empty)
            return Result.Failure("Etapa de subprocesso exige um tipo de processo filho.");

        return Result.Success();
    }
}

/// <summary>
/// Etapa de União (PROJ-50) — fork/join: aguarda que todos os ramos condicionais
/// listados cheguem a uma conclusão antes de avançar.
/// </summary>
public sealed record ConfiguracaoEtapaUniao(IReadOnlyList<Guid> EtapasAguardadasIds) : ConfiguracaoEtapa
{
    public override Result Validar()
    {
        if (EtapasAguardadasIds.Count < 2)
            return Result.Failure("Etapa de união exige ao menos duas etapas aguardadas (senão não há o que unir).");
        if (EtapasAguardadasIds.Any(id => id == Guid.Empty))
            return Result.Failure("Toda etapa aguardada precisa de um id válido.");

        return Result.Success();
    }
}
