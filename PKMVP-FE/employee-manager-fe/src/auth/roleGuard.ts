export type UserRole = "USER" | "MANAGER" | "ADMIN";

export function normalizeRole(role?: string): UserRole {
  const r = String(role ?? "USER").toUpperCase();
  if (r === "ADMIN") return "ADMIN";
  if (r === "MANAGER") return "MANAGER";
  return "USER";
}

export function hasMinRole(role: string | undefined, min: UserRole) {
  const order: Record<UserRole, number> = { USER: 1, MANAGER: 2, ADMIN: 3 };
  return (order[normalizeRole(role)] ?? 0) >= (order[min] ?? 0);
}
