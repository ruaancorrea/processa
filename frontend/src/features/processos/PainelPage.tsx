import { useTiposEtapa } from "./useTiposEtapa";

/**
 * Placeholder do painel operacional — a versão real (kanban + lista com filtros)
 * nasce no Sprint 6 (docs/07-roadmap/backlog-sprints.md). Por ora, só prova que
 * a cadeia frontend → API → motor de processos está conectada de ponta a ponta.
 */
export function PainelPage() {
  const { data: tiposEtapa, isLoading, isError } = useTiposEtapa();

  return (
    <div>
      <h1 className="text-2xl font-semibold">Painel</h1>
      <p className="mt-1 text-sm text-neutral-500">
        Motor de processos — tipos de etapa suportados
      </p>

      <ul className="mt-6 grid grid-cols-2 gap-2 sm:grid-cols-4">
        {isLoading && <li className="text-sm text-neutral-400">Carregando…</li>}
        {isError && (
          <li className="text-sm text-red-600">
            Não foi possível carregar — a API está rodando?
          </li>
        )}
        {tiposEtapa?.map((tipo) => (
          <li
            key={tipo}
            className="rounded-md border border-neutral-200 bg-white px-3 py-2 text-sm"
          >
            {tipo}
          </li>
        ))}
      </ul>
    </div>
  );
}
