import React, { useEffect, useMemo, useState } from "react";
import { Card } from "../components/Card";
import { Button } from "../components/Button";
import { Field } from "../components/Field";
import { DataTable } from "../components/DataTable";
import { ToastProvider, useToast } from "../components/Toast";
import { apiFetch } from "../api/http";
import { endpoints } from "../api/endpoints";
import { Task, WorklogHeader, WorklogScope } from "../api/types";

function ymd(d: Date) {
  return d.toISOString().slice(0, 10);
}

function parseYmd(s: string) {
  // s: YYYY-MM-DD
  const [y, m, d] = s.split("-").map((x) => Number(x));
  return new Date(y, (m || 1) - 1, d || 1);
}

function inRange(dateStr: string | undefined | null, from: string, to: string) {
  if (!dateStr) return false;
  const ds = String(dateStr).slice(0, 10);
  const t = parseYmd(ds).getTime();
  const f = parseYmd(from).getTime();
  const e = parseYmd(to).getTime();
  return t >= f && t <= e;
}

function getWorklogId(r: any): number | null {
  const v = r?.worklogId ?? r?.WORKLOG_ID ?? r?.id ?? r?.ID;
  const n = Number(v);
  return Number.isFinite(n) && n > 0 ? n : null;
}

function normalizeWorklog(r: any): WorklogHeader {
  const worklogId = getWorklogId(r) ?? 0;
  return {
    worklogId,
    workDate: String(r?.workDate ?? r?.WORK_DATE ?? ""),
    summary: String(r?.summary ?? r?.SUMMARY ?? ""),
    status: r?.status ?? r?.STATUS,
    writerId: r?.writerId ?? r?.REPORTER_ID ?? r?.reporterId,
    writerName: r?.writerName,
    teamId: r?.teamId ?? r?.REPORTER_TEAM_ID,
    createdAt: r?.createdAt ?? r?.CREATED_AT,
    updatedAt: r?.updatedAt ?? r?.UPDATED_AT
  };
}

function Inner() {
  const { push } = useToast();

  const today = useMemo(() => new Date(), []);
  const [fromDate, setFromDate] = useState(ymd(new Date(today.getFullYear(), today.getMonth(), today.getDate())));
  const [toDate, setToDate] = useState(ymd(today));

  const [preset, setPreset] = useState<"1D" | "7D" | "30D" | "CUSTOM">("7D");

  const [taskScope] = useState<"team">("team");
  const [worklogScope] = useState<WorklogScope>("team");

  const [loading, setLoading] = useState(false);
  const [tasksAll, setTasksAll] = useState<Task[]>([]);
  const [worklogsAll, setWorklogsAll] = useState<WorklogHeader[]>([]);

  const [selectedUserIds, setSelectedUserIds] = useState<number[]>([]); // empty = all

  function applyPreset(p: "1D" | "7D" | "30D") {
    setPreset(p);
    const end = new Date();
    const start = new Date();
    if (p === "1D") {
      // today
    } else if (p === "7D") {
      start.setDate(end.getDate() - 6);
    } else if (p === "30D") {
      start.setDate(end.getDate() - 29);
    }
    setFromDate(ymd(start));
    setToDate(ymd(end));
  }

  async function load() {
    setLoading(true);
    try {
      const [tRes, wRes] = await Promise.allSettled([
        apiFetch<Task[]>(endpoints.tasks.list(taskScope)),
        apiFetch<any>(endpoints.worklogs.list(fromDate, toDate, worklogScope))
      ]);

      if (tRes.status === "fulfilled") {
        setTasksAll(Array.isArray(tRes.value) ? tRes.value : []);
      } else {
        push({ type: "warn", message: tRes.reason?.message ?? "Tasks(team) 조회 실패" });
        setTasksAll([]);
      }

      if (wRes.status === "fulfilled") {
        const arr = Array.isArray(wRes.value) ? wRes.value : (Array.isArray((wRes.value as any)?.items) ? (wRes.value as any).items : []);
        setWorklogsAll(arr.map(normalizeWorklog).filter((x: WorklogHeader) => x.worklogId));
      } else {
        push({ type: "bad", message: wRes.reason?.message ?? "Worklogs(team) 조회 실패" });
        setWorklogsAll([]);
      }

      // selection 목록 초기화(필터가 팀 전체라면 유지)
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { load(); }, []);

  // tasks 기간 필터(서버가 from/to를 받지 않으므로 FE에서 필터)
  const tasksByRange = useMemo(() => {
    return tasksAll.filter((t) => {
      const d = (t.updatedAt ?? t.createdAt) as any;
      return inRange(d, fromDate, toDate);
    });
  }, [tasksAll, fromDate, toDate]);

  const worklogsByRange = useMemo(() => {
    // API 자체가 range filtering 했지만, 안전하게 다시 필터
    return worklogsAll.filter((w) => inRange(w.workDate, fromDate, toDate));
  }, [worklogsAll, fromDate, toDate]);

  const availableUsers = useMemo(() => {
    const set = new Set<number>();
    for (const t of tasksByRange) {
      if (t.reporterId) set.add(Number(t.reporterId));
      if (t.assigneeId) set.add(Number(t.assigneeId));
    }
    for (const w of worklogsByRange) {
      if (w.writerId) set.add(Number(w.writerId));
    }
    return Array.from(set).sort((a, b) => a - b);
  }, [tasksByRange, worklogsByRange]);

  const tasks = useMemo(() => {
    if (selectedUserIds.length === 0) return tasksByRange;
    return tasksByRange.filter((t) => (t.assigneeId && selectedUserIds.includes(Number(t.assigneeId))) || (t.reporterId && selectedUserIds.includes(Number(t.reporterId))));
  }, [tasksByRange, selectedUserIds]);

  const worklogs = useMemo(() => {
    if (selectedUserIds.length === 0) return worklogsByRange;
    return worklogsByRange.filter((w) => w.writerId && selectedUserIds.includes(Number(w.writerId)));
  }, [worklogsByRange, selectedUserIds]);

  const taskKpi = useMemo(() => {
    const by: Record<string, number> = {};
    let sumProgress = 0;
    for (const t of tasks) {
      by[t.status] = (by[t.status] ?? 0) + 1;
      sumProgress += Number(t.progressPct ?? 0);
    }
    const total = tasks.length;
    const done = by["DONE"] ?? 0;
    return {
      total,
      by,
      doneRate: total ? Math.round((done / total) * 100) : 0,
      avgProgress: total ? Math.round(sumProgress / total) : 0
    };
  }, [tasks]);

  const worklogKpi = useMemo(() => {
    const by: Record<string, number> = {};
    for (const w of worklogs) {
      const s = String(w.status ?? "");
      by[s] = (by[s] ?? 0) + 1;
    }
    return {
      total: worklogs.length,
      by
    };
  }, [worklogs]);

  const taskCols = useMemo(() => ([
    { key: "taskId", title: "TaskId", width: "90px" },
    { key: "title", title: "Title" },
    { key: "status", title: "Status", width: "130px" },
    { key: "progressPct", title: "Progress", width: "110px" },
    { key: "reporterId", title: "Reporter", width: "110px" },
    { key: "assigneeId", title: "Assignee", width: "110px" },
    { key: "updatedAt", title: "Updated", width: "150px", render: (t: Task) => String(t.updatedAt ?? t.createdAt ?? "").slice(0, 10) }
  ]), []);

  const worklogCols = useMemo(() => ([
    { key: "worklogId", title: "WorklogId", width: "110px" },
    { key: "workDate", title: "Date", width: "140px" },
    { key: "status", title: "Status", width: "140px" },
    { key: "writerId", title: "Writer", width: "120px" },
    { key: "summary", title: "Summary" }
  ]), []);

  return (
    <div className="grid">
      <div className="no-print">
        <Card
          title="Team Reports"
          right={
            <div className="row" style={{ justifyContent: "flex-end" }}>
              <Button variant="ghost" onClick={() => applyPreset("1D")}>오늘</Button>
              <Button variant="ghost" onClick={() => applyPreset("7D")}>최근 7일</Button>
              <Button variant="ghost" onClick={() => applyPreset("30D")}>최근 30일</Button>
              <Button onClick={load} disabled={loading}>{loading ? "로딩..." : "조회"}</Button>
              <Button variant="primary" onClick={() => window.print()}>Print/PDF</Button>
            </div>
          }
        >
          <div className="row" style={{ alignItems: "end" }}>
            <div style={{ width: 180 }}>
              <Field label="From">
                <input type="date" value={fromDate} onChange={(e) => { setPreset("CUSTOM"); setFromDate(e.target.value); }} />
              </Field>
            </div>
            <div style={{ width: 180 }}>
              <Field label="To">
                <input type="date" value={toDate} onChange={(e) => { setPreset("CUSTOM"); setToDate(e.target.value); }} />
              </Field>
            </div>

            <div style={{ flex: 1 }}>
              <Field label="Team members (optional)" hint="미선택 시 전체">
                <select
                  multiple
                  value={selectedUserIds.map(String)}
                  onChange={(e) => {
                    const ids = Array.from(e.target.selectedOptions).map((o) => Number(o.value));
                    setSelectedUserIds(ids);
                  }}
                  style={{ minHeight: 44 }}
                >
                  {availableUsers.map((id) => (
                    <option key={id} value={id}>{id}</option>
                  ))}
                </select>
              </Field>
            </div>
          </div>

          <div className="hr" />

          <div className="grid" style={{ gridTemplateColumns: "1fr 1fr" }}>
            <Card title="Tasks Summary">
              <div className="row" style={{ justifyContent: "space-between" }}>
                <div><div style={{ fontSize: 12, color: "var(--muted)" }}>Total</div><div style={{ fontWeight: 900, fontSize: 22 }}>{taskKpi.total}</div></div>
                <div><div style={{ fontSize: 12, color: "var(--muted)" }}>Done Rate</div><div style={{ fontWeight: 900, fontSize: 22 }}>{taskKpi.doneRate}%</div></div>
                <div><div style={{ fontSize: 12, color: "var(--muted)" }}>Avg Progress</div><div style={{ fontWeight: 900, fontSize: 22 }}>{taskKpi.avgProgress}%</div></div>
              </div>
              <div className="hr" />
              <div className="row" style={{ flexWrap: "wrap", gap: 8 }}>
                {Object.entries(taskKpi.by).sort().map(([k, v]) => (
                  <span key={k} className="badge info">{k}: {v}</span>
                ))}
              </div>
            </Card>

            <Card title="Worklogs Summary">
              <div className="row" style={{ justifyContent: "space-between" }}>
                <div><div style={{ fontSize: 12, color: "var(--muted)" }}>Total</div><div style={{ fontWeight: 900, fontSize: 22 }}>{worklogKpi.total}</div></div>
                <div style={{ flex: 1 }} />
              </div>
              <div className="hr" />
              <div className="row" style={{ flexWrap: "wrap", gap: 8 }}>
                {Object.entries(worklogKpi.by).sort().map(([k, v]) => (
                  <span key={k} className="badge info">{k || "(NULL)"}: {v}</span>
                ))}
              </div>
            </Card>
          </div>
        </Card>
      </div>

      <Card title={`Tasks (team) · ${fromDate} ~ ${toDate}`}>
        <DataTable<Task> columns={taskCols as any} rows={tasks} rowKey={(t) => t.taskId} />
      </Card>

      <Card title={`Daily Worklogs (team) · ${fromDate} ~ ${toDate}`}>
        <DataTable<WorklogHeader> columns={worklogCols as any} rows={worklogs} rowKey={(w) => w.worklogId} />
      </Card>
    </div>
  );
}

export function ReportsPage() {
  return (
    <ToastProvider>
      <Inner />
    </ToastProvider>
  );
}
