import React, { useEffect, useState } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { Header } from "../components/Header";
import { hasMinRole } from "../auth/roleGuard";
import { apiFetch } from "../api/http";
import { endpoints } from "../api/endpoints";

export function App() {
  const { logout, user } = useAuth();
  const [unreadCount, setUnreadCount] = useState(0);

  useEffect(() => {
    let cancelled = false;

    if (!user) {
      setUnreadCount(0);
      return;
    }

    const loadUnread = async () => {
      try {
        const res = await apiFetch<{ count: number }>(endpoints.notifications.unreadCount());
        if (!cancelled) {
          setUnreadCount(Math.max(0, Number(res?.count ?? 0)));
        }
      } catch {
        if (!cancelled) {
          setUnreadCount(0);
        }
      }
    };

    const onRefresh = () => { void loadUnread(); };
    window.addEventListener("notifications:refresh", onRefresh);

    void loadUnread();
    const timer = window.setInterval(() => {
      void loadUnread();
    }, 30000);

    return () => {
      cancelled = true;
      window.removeEventListener("notifications:refresh", onRefresh);
      window.clearInterval(timer);
    };
  }, [user?.userId, user?.role]);

  return (
    <div className="app-shell">
      <aside className="sidebar no-print">
        <div className="brand">
          <div className="brand-mark" />
          <div>
            <div className="brand-title">직원관리</div>
            <div className="brand-sub">Tasks · Board · DailyWorklog · Notifications</div>
          </div>
        </div>

        <nav className="nav">
          <NavLink to="/board" className={({ isActive }) => (isActive ? "nav-item active" : "nav-item")}>
            Board
          </NavLink>
          <NavLink to="/tasks" className={({ isActive }) => (isActive ? "nav-item active" : "nav-item")}>
            Tasks
          </NavLink>
          <NavLink to="/dailyworklog" className={({ isActive }) => (isActive ? "nav-item active" : "nav-item")}>
            Daily Worklog
          </NavLink>
          <NavLink to="/team-directory" className={({ isActive }) => (isActive ? "nav-item active" : "nav-item")}>
            Team Directory
          </NavLink>
          <NavLink to="/notifications" className={({ isActive }) => (isActive ? "nav-item active" : "nav-item")}>
            <span>Notifications</span>
            {unreadCount > 0 ? <span className="nav-badge">{unreadCount}</span> : null}
          </NavLink>
          {user && hasMinRole(user.role, "MANAGER") && (
            <NavLink to="/reports" className={({ isActive }) => (isActive ? "nav-item active" : "nav-item")}>
              Reports
            </NavLink>
          )}
        </nav>

        <div className="sidebar-footer">
          <div className="mini">
            <div className="mini-label">Signed in</div>
            <div className="mini-value">{user?.displayName ?? user?.userId ?? "-"}</div>
            <div style={{ marginTop: 6, color: "var(--muted)", fontSize: 12 }}>[{user?.role ?? ""}] {user?.teamId ?? ""}</div>
          </div>
          <button className="btn ghost" onClick={logout}>로그아웃</button>
        </div>
      </aside>

      <main className="main">
        <Header />
        <div className="content">
          <Outlet />
        </div>
      </main>
    </div>
  );
}

