import React, { createContext, useContext, useMemo, useState } from "react";

type ToastItem = { id: string; type: "info" | "ok" | "warn" | "bad"; message: string };
const ToastCtx = createContext<{ push: (t: Omit<ToastItem, "id">) => void } | null>(null);

// UUID generator with fallback for browsers that don't support crypto.randomUUID()
function generateId(): string {
  if (typeof crypto !== "undefined" && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  // Fallback: Simple UUID v4 generator
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
    const r = (Math.random() * 16) | 0;
    const v = c === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [items, setItems] = useState<ToastItem[]>([]);

  function push(t: Omit<ToastItem, "id">) {
    const id = generateId();
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
