import React from "react";
import { Routes, Route, Navigate } from "react-router-dom";
import { App } from "./App";
import { RequireAuth } from "../auth/RequireAuth";
import { RequireRole } from "../auth/RequireRole";
import { LoginPage } from "../pages/LoginPage";
import { TasksPage } from "../pages/TasksPage";
import { BoardPage } from "../pages/BoardPage";
import { DailyWorklogPage } from "../pages/DailyWorklogPage";
import { TeamDirectoryPage } from "../pages/TeamDirectoryPage";
import { ReportsPage } from "../pages/ReportsPage";
import { NotificationsPage } from "../pages/NotificationsPage";
import { NotFoundPage } from "../pages/NotFoundPage";

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <RequireAuth>
            <App />
          </RequireAuth>
        }
      >
        <Route index element={<Navigate to="/board" replace />} />
        <Route path="board" element={<BoardPage />} />
        <Route path="tasks" element={<TasksPage />} />
        <Route path="dailyworklog" element={<DailyWorklogPage />} />
        <Route path="team-directory" element={<TeamDirectoryPage />} />
        <Route path="notifications" element={<NotificationsPage />} />
        <Route path="reports" element={<RequireRole minRole="MANAGER"><ReportsPage /></RequireRole>} />
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
