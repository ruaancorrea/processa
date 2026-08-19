using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Demandas;

/// <summary>
/// Requisitos, módulo 10.3: "responsável, gestor da equipe, admin" podem
/// interagir com uma execução. Simplificação consciente (mesma linha da já
/// aceita pra Clientes/TipoProcesso, ver .faf/pendencias.faf): "gestor" aqui é
/// qualquer Gestor do tenant, não só o(s) gestor(es) DA equipe responsável —
/// o RBAC deste projeto ainda não isola por equipe.
/// </summary>
public static class AutorizacaoExecucao
{
    public static bool PodeInteragir(IUsuarioContext usuarioContext, ExecucaoEtapa execucao) =>
        usuarioContext.Perfil is Perfil.Admin or Perfil.Gestor || execucao.ResponsavelId == usuarioContext.UsuarioId;
}
