import React from "react";
import { Link } from "react-router-dom";

export function NotFoundPage() {
  return (
    <div style={{ minHeight: "100%", display: "grid", placeItems: "center", padding: 18 }}>
      <div style={{ textAlign: "center" }}>
        <div style={{ fontSize: 22, fontWeight: 900 }}>404</div>
        <div style={{ color: "var(--muted)", marginTop: 10 }}>페이지를 찾을 수 없습니다.</div>
        <div style={{ marginTop: 14 }}><Link to="/tasks" className="btn primary">Tasks로 이동</Link></div>
      </div>
    </div>
  );
}
