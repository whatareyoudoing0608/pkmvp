import React, { useEffect, useState } from "react";
import { apiFetch } from "../api/http";
import { endpoints } from "../api/endpoints";
import { AppNotification } from "../api/types";
import { Card } from "../components/Card";
import { Button } from "../components/Button";
import { ToastProvider, useToast } from "../components/Toast";

function Inner() {
  const { push } = useToast();
  const [rows, setRows] = useState<AppNotification[]>([]);
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [loading, setLoading] = useState(false);

  async function load() {
    setLoading(true);
    try {
      const list = await apiFetch<AppNotification[]>(endpoints.notifications.list(unreadOnly, 100));
      setRows(list);
    } catch (e: any) {
      push({ type: "bad", message: e?.message ?? "알림 조회 실패" });
    } finally {
      setLoading(false);
    }
  }

  async function markRead(notificationId: number) {
    try {
      await apiFetch(endpoints.notifications.markRead(notificationId), { method: "POST" });
      await load();
      window.dispatchEvent(new Event("notifications:refresh"));
    } catch (e: any) {
      push({ type: "bad", message: e?.message ?? "읽음 처리 실패" });
    }
  }

  async function markAllRead() {
    try {
      await apiFetch(endpoints.notifications.markAllRead(), { method: "POST" });
      push({ type: "ok", message: "전체 읽음 처리 완료" });
      await load();
      window.dispatchEvent(new Event("notifications:refresh"));
    } catch (e: any) {
      push({ type: "bad", message: e?.message ?? "전체 읽음 처리 실패" });
    }
  }

  useEffect(() => {
    load();
  }, [unreadOnly]);

  return (
    <div className="grid">
      <Card
        title="Notifications"
        right={
          <div className="row">
            <label style={{ display: "flex", gap: 8, alignItems: "center", color: "var(--muted)", fontSize: 13 }}>
              <input type="checkbox" checked={unreadOnly} onChange={(e) => setUnreadOnly(e.target.checked)} style={{ width: 16, height: 16 }} />
              unread only
            </label>
            <Button onClick={load} disabled={loading}>{loading ? "로딩..." : "새로고침"}</Button>
            <Button onClick={markAllRead}>전체 읽음</Button>
          </div>
        }
      >
        <table className="table">
          <thead>
            <tr>
              <th style={{ width: 90 }}>Type</th>
              <th style={{ width: 240 }}>Title</th>
              <th>Message</th>
              <th style={{ width: 170 }}>Created</th>
              <th style={{ width: 120 }}>Action</th>
            </tr>
          </thead>
          <tbody>
            {rows.length === 0 ? (
              <tr><td colSpan={5} style={{ color: "var(--muted)", padding: 14 }}>알림이 없습니다.</td></tr>
            ) : rows.map((n) => (
              <tr key={n.notificationId}>
                <td>{n.type}</td>
                <td>{n.title}</td>
                <td>{n.message ?? ""}</td>
                <td style={{ color: "var(--muted)", fontSize: 12 }}>{n.createdAt ?? ""}</td>
                <td>
                  {n.isRead === "Y" ? (
                    <span className="badge ok">READ</span>
                  ) : (
                    <Button variant="primary" onClick={() => markRead(n.notificationId)}>읽음</Button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  );
}

export function NotificationsPage() {
  return <ToastProvider><Inner /></ToastProvider>;
}


