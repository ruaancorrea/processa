import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError, httpClient } from "./httpClient";

function mockFetchOnce(response: Partial<Response> & { json?: () => Promise<unknown> }) {
  vi.stubGlobal(
    "fetch",
    vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({}),
      ...response,
    }),
  );
}

describe("httpClient", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("retorna o corpo JSON em resposta de sucesso", async () => {
    mockFetchOnce({ ok: true, status: 200, json: async () => ({ nome: "Processa" }) });

    const resultado = await httpClient<{ nome: string }>("/qualquer");

    expect(resultado).toEqual({ nome: "Processa" });
  });

  it("retorna undefined em resposta 204", async () => {
    mockFetchOnce({ ok: true, status: 204 });

    const resultado = await httpClient("/sem-conteudo");

    expect(resultado).toBeUndefined();
  });

  it("lança ApiError com status e corpo em resposta de erro", async () => {
    mockFetchOnce({
      ok: false,
      status: 422,
      json: async () => ({ detail: "campo obrigatório" }),
    });

    await expect(httpClient("/invalido")).rejects.toMatchObject({
      status: 422,
      body: { detail: "campo obrigatório" },
    });
    await expect(httpClient("/invalido")).rejects.toBeInstanceOf(ApiError);
  });

  it("inclui o header Authorization quando há token salvo", async () => {
    localStorage.setItem("processa.accessToken", "token-de-teste");
    mockFetchOnce({ ok: true, status: 200, json: async () => ({}) });

    await httpClient("/protegido");

    const [, init] = vi.mocked(fetch).mock.calls[0];
    const headers = init?.headers as Record<string, string>;
    expect(headers.Authorization).toBe("Bearer token-de-teste");
  });

  it("não inclui Authorization quando não há token salvo", async () => {
    mockFetchOnce({ ok: true, status: 200, json: async () => ({}) });

    await httpClient("/publico");

    const [, init] = vi.mocked(fetch).mock.calls[0];
    const headers = init?.headers as Record<string, string>;
    expect(headers.Authorization).toBeUndefined();
  });
});
