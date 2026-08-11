import { useQuery } from "@tanstack/react-query";
import { httpClient } from "@/shared/api/httpClient";

export type TipoEtapa =
  | "Comum"
  | "Condicional"
  | "Automatizada"
  | "Notificacao"
  | "Agendamento"
  | "Subprocesso"
  | "Conclusao"
  | "Uniao";

export function useTiposEtapa() {
  return useQuery({
    queryKey: ["tipos-etapa"],
    queryFn: () => httpClient<TipoEtapa[]>("/api/v1/tipos-etapa"),
  });
}
