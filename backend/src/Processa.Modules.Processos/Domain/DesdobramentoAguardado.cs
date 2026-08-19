using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Suporte à Etapa de União (fork/join) — um registro por ramo Condicional que
/// converge numa União. A União avança quando todos os registros vinculados a ela
/// têm Concluido = true (ver IVerificadorDesdobramentos, Sprint 4).
/// </summary>
public sealed class DesdobramentoAguardado : Entity
{
    public Guid TenantId { get; private set; }
    public Guid ExecucaoEtapaUniaoId { get; private set; }
    public Guid ExecucaoEtapaCondicionalId { get; private set; }
    public bool Concluido { get; private set; }

    private DesdobramentoAguardado()
    {
    }

    private DesdobramentoAguardado(Guid id, Guid tenantId, Guid execucaoEtapaUniaoId, Guid execucaoEtapaCondicionalId) : base(id)
    {
        TenantId = tenantId;
        ExecucaoEtapaUniaoId = execucaoEtapaUniaoId;
        ExecucaoEtapaCondicionalId = execucaoEtapaCondicionalId;
        Concluido = false;
    }

    public static Result<DesdobramentoAguardado> Criar(Guid tenantId, Guid execucaoEtapaUniaoId, Guid execucaoEtapaCondicionalId)
    {
        if (tenantId == Guid.Empty || execucaoEtapaUniaoId == Guid.Empty || execucaoEtapaCondicionalId == Guid.Empty)
            return Result.Failure<DesdobramentoAguardado>("Tenant, execução de união e execução condicional são obrigatórios.");

        return Result.Success(new DesdobramentoAguardado(Guid.NewGuid(), tenantId, execucaoEtapaUniaoId, execucaoEtapaCondicionalId));
    }

    public void MarcarConcluido() => Concluido = true;
}
