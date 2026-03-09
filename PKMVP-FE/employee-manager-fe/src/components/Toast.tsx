import React, { createContext, useContext, useMemo, useState } from "react";

type ToastItem = { id: string; type: "info" | "ok" | "warn" | "bad"; message: string };
const ToastCtx = createContext<{ push: (t: Omit<ToastItem, "id">) => void } | null>(null);

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [items, setItems] = useState<ToastItem[]>([]);

  function push(t: Omit<ToastItem, "id">) {
    const id = crypto.randomUUID();
    const item = { id, ...t };
    setItems((prev) => [item, ...prev].slice(0, 4));
    window.setTimeout(() => setItems((prev) => prev.filter((x) => x.id !== id)), 3500);
  }

  const value = useMemo(() => ({ push }), []);

  return (
    <ToastCtx.Provider value={value}>
      {children}
      <div className="toast-stack">{items.map((t) => <div key={t.id} className={`toast ${t.type}`}>{t.message}</div>)}</div>
    </ToastCtx.Provider>
  );
}

export function useToast() {
  const ctx = useContext(ToastCtx);
  if (!ctx) throw new Error("ToastProvider not found");
  return ctx;
}
