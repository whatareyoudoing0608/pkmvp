import React, { useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { Card } from "../components/Card";
import { Field } from "../components/Field";
import { Button } from "../components/Button";
import { ToastProvider, useToast } from "../components/Toast";

function Inner() {
  const { login } = useAuth();
  const { push } = useToast();

  const [loginId, setLoginId] = useState("admin");
  const [password, setPassword] = useState("admin123");
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    try {
      await login({ loginId, password });
      push({ type: "ok", message: "로그인 성공" });
    } catch (err: any) {
      push({ type: "bad", message: err?.message ?? "로그인 실패" });
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ minHeight: "100vh", display: "grid", placeItems: "center", padding: 18, background: "radial-gradient(1200px 900px at 10% 0%, rgba(96,165,250,0.22), transparent 60%), radial-gradient(900px 700px at 90% 20%, rgba(251,113,133,0.18), transparent 55%), linear-gradient(180deg, #f7fbff 0%, #ffffff 60%)" }}>
      <div style={{ width: 420, maxWidth: "95vw" }}>
        <Card title="로그인">
          <form className="grid" onSubmit={onSubmit}>
            <Field label="loginId" hint='API: {"loginId": "...", "password": "..."}'>
              <input value={loginId} onChange={(e) => setLoginId(e.target.value)} autoFocus />
            </Field>
            <Field label="password">
              <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" />
            </Field>
            <div className="row" style={{ justifyContent: "flex-end" }}>
              <Button variant="primary" type="submit" disabled={loading}>
                {loading ? "로그인 중..." : "로그인"}
              </Button>
            </div>

            <div style={{ color: "var(--muted)", fontSize: 12 }}>
              테스트 계정 예시: admin/admin123, manager/manager123, user/user123, user3/user123(HR)
            </div>
          </form>
        </Card>
      </div>
    </div>
  );
}

export function LoginPage() {
  return (
    <ToastProvider>
      <Inner />
    </ToastProvider>
  );
}
