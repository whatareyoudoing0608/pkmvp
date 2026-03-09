import React from "react";
import { getBaseUrl } from "../api/http";

export function Header() {
  return (
    <div className="header no-print">
      <div>
        <div className="header-title">Dashboard</div>
        <div className="header-sub">API: <span className="kbd">{getBaseUrl()}</span></div>
      </div>
      <div><span className="kbd">Ctrl</span> + <span className="kbd">K</span></div>
    </div>
  );
}
