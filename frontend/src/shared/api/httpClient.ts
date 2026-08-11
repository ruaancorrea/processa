const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";

export class ApiError extends Error {
  status: number;
  body: unknown;

  constructor(status: number, body: unknown) {
    super(`Erro na API (${status})`);
    this.status = status;
    this.body = body;
  }
}

/**
 * Cliente HTTP fino sobre fetch. Injeta o token de autenticação e trata o
 * envelope de erro padrão (RFC 9457 Problem Details) — ver docs/04-api/convencoes-api.md
 */
export async function httpClient<T>(path: string, init?: RequestInit): Promise<T> {
  const token = localStorage.getItem("processa.accessToken");

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new ApiError(response.status, body);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
