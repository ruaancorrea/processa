/**
 * Access token guardado só em memória (variável de módulo), nunca em localStorage/
 * sessionStorage — evita exposição a um XSS que consiga rodar JS na página. Some ao
 * recarregar a aba; a sessão é recuperada chamando /api/v1/auth/refresh, que usa o
 * refresh token no cookie httpOnly (invisível a JS) para emitir um access token novo.
 * Ver .faf/decisions.faf.
 */
let accessToken: string | null = null;

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

export function getAccessToken(): string | null {
  return accessToken;
}
