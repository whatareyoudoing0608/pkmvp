import React, { useEffect, useState } from "react";
import { Card } from "../components/Card";
import { Field } from "../components/Field";
import { Button } from "../components/Button";
import { ToastProvider, useToast } from "../components/Toast";
import { apiFetch } from "../api/http";
import { endpoints } from "../api/endpoints";
import { Member, Team } from "../api/types";

function Inner() {
  const { push } = useToast();
  const [teams, setTeams] = useState<Team[]>([]);
  const [teamId, setTeamId] = useState<string>("");
  const [q, setQ] = useState("");
  const [members, setMembers] = useState<Member[]>([]);
  const [loading, setLoading] = useState(false);

  async function loadTeams() {
    try {
      const res = await apiFetch<Team[]>(endpoints.teamDirectory.teams());
      setTeams(res);
    } catch {
      setTeams([]);
    }
  }

  async function search() {
    setLoading(true);
    try {
      let res: Member[] = [];
      try {
        res = await apiFetch<Member[]>(endpoints.teamDirectory.members(teamId || undefined, q || undefined));
      } catch {
        res = await apiFetch<Member[]>(endpoints.teamDirectory.usersFallback(q || undefined));
      }
      setMembers(res);
    } catch (e: any) {
      push({ type: "bad", message: e?.message ?? "검색 실패" });
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { loadTeams(); }, []);

  return (
    <div className="grid">
      <Card title="Team Directory" right={<Button onClick={search} disabled={loading}>{loading ? "검색..." : "검색"}</Button>}>
        <div className="row">
          <div style={{ width: 220 }}><Field label="Team"><select value={teamId} onChange={(e) => setTeamId(e.target.value)}><option value="">(전체)</option>{teams.map((t) => <option key={t.teamId} value={t.teamId}>{t.teamName} ({t.teamId})</option>)}</select></Field></div>
          <div style={{ flex: 1 }}><Field label="Query"><input value={q} onChange={(e) => setQ(e.target.value)} placeholder="이름/ID/팀" /></Field></div>
        </div>
        <div className="hr" />
        <table className="table">
          <thead><tr><th style={{ width: 90 }}>UserId</th><th>Name</th><th style={{ width: 120 }}>Team</th><th style={{ width: 140 }}>Role</th></tr></thead>
          <tbody>
            {members.length === 0 ? (
              <tr><td colSpan={4} style={{ color: "var(--muted)", padding: 14 }}>검색하세요.</td></tr>
            ) : members.map((m) => (
              <tr key={m.userId}><td>{m.userId}</td><td>{m.displayName}</td><td>{m.teamId}</td><td>{m.role ?? ""}</td></tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  );
}

export function TeamDirectoryPage() {
  return <ToastProvider><Inner /></ToastProvider>;
}
