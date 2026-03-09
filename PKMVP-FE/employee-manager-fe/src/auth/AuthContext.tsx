import React, { createContext, useContext, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { apiFetch } from "../api/http";
import { endpoints } from "../api/endpoints";
import { LoginRequest, LoginResponse } from "../api/types";
import { tokenStore } from "./tokenStore";

type AuthUser = {
  userId?: string;
  displayName?: string;
  teamId?: string;
  role?: string;
};

type AuthContextValue = {
  isAuthed: boolean;
  user: AuthUser | null;
  login: (req: LoginRequest) => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const navigate = useNavigate();
  const initial = tokenStore.get();
  const [user, setUser] = useState<AuthUser | null>(
    initial.accessToken
      ? { userId: initial.userId, displayName: initial.displayName, teamId: initial.teamId, role: initial.role }
      : null
  );

  const isAuthed = !!tokenStore.getAccessToken();

  useEffect(() => {
    const token = tokenStore.getAccessToken();
    if (!token) return;

    // 앱 시작 시 /auth/me 로 role/teamId 동기화
    apiFetch<any>(endpoints.auth.me())
      .then((me) => {
        tokenStore.set({
          ...tokenStore.get(),
          userId: me?.userId ? String(me.userId) : undefined,
          teamId: me?.teamId ? String(me.teamId) : undefined,
          role: me?.role ? String(me.role) : undefined,
          displayName: me?.userId ? `user${me.userId}` : undefined
        });
        setUser({
          userId: me?.userId ? String(me.userId) : undefined,
          displayName: me?.userId ? `user${me.userId}` : undefined,
          teamId: me?.teamId ? String(me.teamId) : undefined,
          role: me?.role ? String(me.role) : undefined
        });
      })
      .catch(() => {
        // ignore
      });
  }, []);


  async function login(req: LoginRequest) {
    const res = await apiFetch<LoginResponse>(endpoints.auth.login(), {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(req),
      skipAuth: true
    });

    // 토큰 저장
    tokenStore.set({
      accessToken: res.accessToken,
      refreshToken: res.refreshToken,
      accessTokenExpiresAt: res.accessTokenExpiresAt,
      refreshTokenExpiresAt: res.refreshTokenExpiresAt
    });

    // me 조회로 userId/role/teamId 확정
    try {
      const me = await apiFetch<any>(endpoints.auth.me());
      tokenStore.set({
        ...tokenStore.get(),
        userId: String(me?.userId ?? ""),
        teamId: me?.teamId ? String(me.teamId) : undefined,
        role: me?.role ? String(me.role) : undefined,
        displayName: me?.userId ? `user${me.userId}` : undefined
      });
      setUser({
        userId: me?.userId ? String(me.userId) : undefined,
        displayName: me?.userId ? `user${me.userId}` : undefined,
        teamId: me?.teamId ? String(me.teamId) : undefined,
        role: me?.role ? String(me.role) : undefined
      });
    } catch {
      // 최소한 토큰만으로도 진행
      setUser({ userId: undefined, displayName: undefined, teamId: undefined, role: undefined });
    }

    navigate("/tasks", { replace: true });
  }

  function logout() {
    tokenStore.clear();
    setUser(null);
    navigate("/login", { replace: true });
  }

  const value = useMemo<AuthContextValue>(() => ({ isAuthed, user, login, logout }), [isAuthed, user]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("AuthContext not found");
  return ctx;
}
