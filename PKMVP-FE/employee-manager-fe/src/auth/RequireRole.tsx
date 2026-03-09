import React from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "./AuthContext";
import { hasMinRole, UserRole } from "./roleGuard";

export function RequireRole({ minRole, children }: { minRole: UserRole; children: React.ReactNode }) {
  const { isAuthed, user } = useAuth();
  if (!isAuthed) return <Navigate to="/login" replace />;
  if (!hasMinRole(user?.role, minRole)) return <Navigate to="/tasks" replace />;
  return <>{children}</>;
}
