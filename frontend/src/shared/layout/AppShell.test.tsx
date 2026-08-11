import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { AppShell } from "./AppShell";

describe("AppShell", () => {
  it("renderiza a marca e o conteúdo", () => {
    render(
      <AppShell>
        <p>conteúdo da página</p>
      </AppShell>,
    );

    expect(screen.getByText("Processa")).toBeInTheDocument();
    expect(screen.getByText("conteúdo da página")).toBeInTheDocument();
  });

  it("renderiza os itens de navegação principais", () => {
    render(
      <AppShell>
        <div />
      </AppShell>,
    );

    expect(screen.getByRole("link", { name: "Painel" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Kanban" })).toBeInTheDocument();
  });
});
