using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Domain;

/// <summary>Entrada de log imutável — a timeline de uma execução (ver requisitos, módulo 10.3). UsuarioId nulo = evento automático (ex.: etapa Automatizada).</summary>
public sealed class HistoricoExecucaoEtapa : Entity
{
    public Guid TenantId { get; private set; }
    public Guid ExecucaoEtapaId { get; private set; }
    public TipoEventoHistorico TipoEvento { get; private set; }
    public string Dados { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private HistoricoExecucaoEtapa()
    {
        Dados = "{}";
    }

    private HistoricoExecucaoEtapa(
        Guid id, Guid tenantId, Guid execucaoEtapaId, TipoEventoHistorico tipoEvento, string dados, Guid? usuarioId) : base(id)
    {
        TenantId = tenantId;
        ExecucaoEtapaId = execucaoEtapaId;
        TipoEvento = tipoEvento;
        Dados = dados;
        UsuarioId = usuarioId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<HistoricoExecucaoEtapa> Registrar(
        Guid tenantId, Guid execucaoEtapaId, TipoEventoHistorico tipoEvento, string? dados, Guid? usuarioId)
    {
        if (tenantId == Guid.Empty || execucaoEtapaId == Guid.Empty)
            return Result.Failure<HistoricoExecucaoEtapa>("Tenant e execução são obrigatórios.");

        return Result.Success(new HistoricoExecucaoEtapa(
            Guid.NewGuid(), tenantId, execucaoEtapaId, tipoEvento, string.IsNullOrWhiteSpace(dados) ? "{}" : dados, usuarioId));
    }
}
