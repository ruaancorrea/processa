import type { ReactNode } from "react";

const NAV_ITEMS = [
  { label: "Painel", href: "/" },
  { label: "Clientes", href: "/clientes" },
  { label: "Processos", href: "/processos" },
  { label: "Kanban", href: "/kanban" },
  { label: "Documentos", href: "/documentos" },
];

export function AppShell({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-screen bg-neutral-50 text-neutral-900">
      <aside className="w-56 shrink-0 border-r border-neutral-200 bg-white p-4">
        <div className="mb-8 text-lg font-semibold tracking-tight">Processa</div>
        <nav className="flex flex-col gap-1">
          {NAV_ITEMS.map((item) => (
            <a
              key={item.href}
              href={item.href}
              className="rounded-md px-3 py-2 text-sm text-neutral-600 hover:bg-neutral-100 hover:text-neutral-900"
            >
              {item.label}
            </a>
          ))}
        </nav>
      </aside>
      <main className="flex-1 p-8">{children}</main>
    </div>
  );
}
