import React, { useEffect, useMemo, useState } from "react";
import { Card } from "../components/Card";
import { Field } from "../components/Field";
import { Button } from "../components/Button";
import { Dialog } from "../components/Dialog";
import { DataTable } from "../components/DataTable";
import { ToastProvider, useToast } from "../components/Toast";
import { useAuth } from "../auth/AuthContext";
import { hasMinRole } from "../auth/roleGuard";
import { apiFetch } from "../api/http";
import { endpoints } from "../api/endpoints";
import {
  WorklogHeader,
  WorklogScope,
  WorklogCreateRequest,
  WorklogItemCreateRequest,
  WorklogApproveRequest,
  WorklogRejectRequest
} from "../api/types";

function ymd(d: Date) { return d.toISOString().slice(0, 10); }

function getWorklogId(r: any): number | null {
  const v =
    r?.worklogId ??
    r?.WorklogId ??
    r?.IT_WORKLOG_ID ??
    r?.itWorklogId ??
    r?.worklog_id ??
    r?.WORKLOG_ID ??
    r?.id ??
    r?.ID;
  const n = Number(v);
  return Number.isFinite(n) && n > 0 ? n : null;
}

function normalizeRow(r: any): WorklogHeader {
  const worklogId = getWorklogId(r) ?? 0;
  return {
    worklogId,
    workDate: String(r?.workDate ?? r?.WORK_DATE ?? r?.date ?? ""),
    summary: String(r?.summary ?? r?.SUMMARY ?? r?.title ?? ""),
    status: r?.status ?? r?.STATUS,
    writerId: r?.writerId ?? r?.WRITER_ID,
    writerName: r?.writerName ?? r?.WRITER_NAME,
    teamId: r?.teamId ?? r?.TEAM_ID,
    createdAt: r?.createdAt ?? r?.CREATED_AT,
    updatedAt: r?.updatedAt ?? r?.UPDATED_AT
  };
}

function Inner() {
  const { push } = useToast();
  const { user } = useAuth();
  const today = useMemo(() => new Date(), []);
  const [fromDate, setFromDate] = useState(ymd(new Date(today.getFullYear(), today.getMonth(), 1)));
  const [toDate, setToDate] = useState(ymd(today));
  const [scope, setScope] = useState<WorklogScope>("mine");

  const [rows, setRows] = useState<WorklogHeader[]>([]);
  const [loading, setLoading] = useState(false);

  // create
  const [workDate, setWorkDate] = useState(ymd(today));
  const [summary, setSummary] = useState("");

  // dialogs
  const [itemOpen, setItemOpen] = useState(false);
  const [itemTarget, setItemTarget] = useState<WorklogHeader | null>(null);
  const [seq, setSeq] = useState(1);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [spentMinutes, setSpentMinutes] = useState(30);
  const [progressPct, setProgressPct] = useState(50);

  const [approveOpen, setApproveOpen] = useState(false);
  const [approveTarget, setApproveTarget] = useState<WorklogHeader | null>(null);
  const [approveScore, setApproveScore] = useState(5);
  const [approveComment, setApproveComment] = useState("");

  const [rejectOpen, setRejectOpen] = useState(false);
  const [rejectTarget, setRejectTarget] = useState<WorklogHeader | null>(null);
  const [rejectComment, setRejectComment] = useState("");

  async function load() {
    setLoading(true);
    try {
      const res = await apiFetch<any>(endpoints.worklogs.list(fromDate, toDate, scope));
      const arr = Array.isArray(res) ? res : (Array.isArray(res?.items) ? res.items : []);
      setRows(arr.map(normalizeRow).filter((x: WorklogHeader) => x.worklogId));
    } catch (e: any) {
      push({ type: "bad", message: e?.message ?? "Worklogs 조회 실패" });
    } finally {
      setLoading(false);
    }
  }

  async function createWorklog() {
    const payload: WorklogCreateRequest = { workDate, summary };
    try {
      await apiFetch(endpoints.worklogs.create(), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });
      push({ type: "ok", message: "Worklog 생성 완료" });
      setSummary("");
      await load();
    } catch (e: any) {
      push({ type: "bad", message: e?.message ?? "Worklog 생성 실패" });
    }
  }

  function openAddItem(r: WorklogHeader) {
    setItemTarget(r);
    setSeq(1);
    setTitle("");
    setDescription("");
    setSpentMinutes(30);
    setProgressPct(50);
    setItemOpen(true);
  }

  async function addItem() {
    if (!itemTarget) return;
    const payload: WorklogItemCreateRequest = {
      seq: Number(seq || 0),
      title,
      description,
      spentMinutes: Number(spentMinutes || 0),
      progressPct: Number(progressPct || 0)
    };

    try {
      const wid = getWorklogId(itemTarget);
      if (!wid) { push({ type: "bad", message: "worklogId 누락(목록 응답 필드 확인 필요)" }); return; }
      await apiFetch(endpoints.worklogs.addItem(wid), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });
      push({ type: "ok", message: `Item 추가 완료 (#${itemTarget.worklogId})` });
      setItemOpen(false);
      await load();
    } catch (e: any) {
      push({ type: "bad", message: e?.message ?? "Item 추가 실패" });
    }
  }

  async function submit(r: WorklogHeader) {
    try {
      const wid = getWorklogId(r);
      if (!wid) { push({ type: "bad", message: "worklogId 누락" }); return; }
      await apiFetch(endpoints.worklogs.submit(wid), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: ""
      });
      push({ type: "ok", message: `Submit 완료 (#${wid})` });
      await load();
    } catch (e: any) {
      push({ type: "bad", message: e?.message ?? "Submit 실패" });
    }
  }

  function openApprove(r: WorklogHeader) {
    setApproveTarget(r);
    setApproveScore(5);
    setApproveComment("");
    setApproveOpen(true);
  }

  async function approve() {
    if (!approveTarget) return;
    const payload: WorklogApproveRequest = { score: Number(approveScore || 0), commentTxt: approveComment };

    try {
      const wid = getWorklogId(approveTarget);
      if (!wid) { push({ type: "bad", message: "worklogId 누락" }); return; }
      await apiFetch(endpoints.worklogs.approve(wid), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });
      push({ type: "ok", message: `Approve 완료 (#${wid})` });
      setApproveOpen(false);
      await load();
    } catch (e: any) {
      push({ type: "bad", message: e?.message ?? "Approve 실패" });
    }
  }

  function openReject(r: WorklogHeader) {
    setRejectTarget(r);
    setRejectComment("");
    setRejectOpen(true);
  }

  async function reject() {
    if (!rejectTarget) return;
    const payload: WorklogRejectRequest = { commentTxt: rejectComment };

    try {
      const wid = getWorklogId(rejectTarget);
      if (!wid) { push({ type: "bad", message: "worklogId 누락" }); return; }
      await apiFetch(endpoints.worklogs.reject(wid), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });
      push({ type: "ok", message: `Reject 완료 (#${wid})` });
      setRejectOpen(false);
      await load();
    } catch (e: any) {
      push({ type: "bad", message: e?.message ?? "Reject 실패" });
    }
  }

  useEffect(() => { load(); }, []);

  const columns = useMemo(() => ([
    { key: "worklogId", title: "ID", width: "90px" },
    { key: "workDate", title: "Date", width: "130px" },
    { key: "summary", title: "Summary" },
    { key: "status", title: "Status", width: "120px" },
    {
      key: "_actions",
      title: "Actions",
      width: "340px",
      render: (r: WorklogHeader) => (
        <div className="row" style={{ gap: 8 }}>
          <Button variant="ghost" onClick={(e) => { e.stopPropagation(); openAddItem(r); }}>Add Item</Button>
          <Button variant="ghost" onClick={(e) => { e.stopPropagation(); submit(r); }}>Submit</Button>
          <Button variant="primary" onClick={(e) => { e.stopPropagation(); openApprove(r); }}>Approve</Button>
          <Button variant="danger" onClick={(e) => { e.stopPropagation(); openReject(r); }}>Reject</Button>
        </div>
      )
    }
  ]), []);

  return (
    <div className="grid" style={{ gridTemplateColumns: "1.4fr 1fr" }}>
      <Card
        title="Worklogs"
        right={
          <div className="row">
            <input type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} style={{ width: 160 }} />
            <input type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} style={{ width: 160 }} />
            <select value={scope} onChange={(e) => setScope(e.target.value as WorklogScope)} style={{ width: 140 }}>
              <option value="mine">mine</option>
              {(user && hasMinRole(user.role, "MANAGER")) && <option value="team">team</option>}
              {(user && hasMinRole(user.role, "ADMIN")) && <option value="all">all</option>}
            </select>
            <Button onClick={load} disabled={loading}>{loading ? "로딩..." : "조회"}</Button>
          </div>
        }
      >
        <DataTable columns={columns} rows={rows} rowKey={(r) => r.worklogId || (getWorklogId(r) ?? Math.random())} />
        <div style={{ marginTop: 10, color: "var(--muted)", fontSize: 12 }}>
          list: <span className="kbd">GET</span> <span className="kbd">/api/worklogs?fromDate=...&toDate=...&scope=mine|team|all</span>
        </div>
      </Card>

      <Card title="Create Worklog" right={<Button variant="primary" onClick={createWorklog} disabled={!summary.trim()}>Create</Button>}>
        <div className="grid">
          <Field label="workDate">
            <input type="date" value={workDate} onChange={(e) => setWorkDate(e.target.value)} />
          </Field>
          <Field label="summary">
            <textarea value={summary} onChange={(e) => setSummary(e.target.value)} rows={4} placeholder="예: IT approve flow" />
          </Field>

          <div style={{ color: "var(--muted)", fontSize: 12 }}>
            create: <span className="kbd">POST</span> <span className="kbd">/api/worklogs</span> body: <span className="kbd">{`{"workDate":"YYYY-MM-DD","summary":"..."}`}</span>
          </div>
        </div>
      </Card>

      <Dialog
        open={itemOpen}
        title={itemTarget ? `Add Item · Worklog #${itemTarget.worklogId}` : "Add Item"}
        onClose={() => setItemOpen(false)}
        footer={
          <div className="row" style={{ justifyContent: "space-between" }}>
            <div style={{ color: "var(--muted)", fontSize: 12 }}>
              <span className="kbd">POST</span> <span className="kbd">/api/worklogs/{itemTarget?.worklogId}/items</span>
            </div>
            <Button variant="primary" onClick={addItem} disabled={!itemTarget || !title.trim()}>저장</Button>
          </div>
        }
      >
        <div className="grid" style={{ gridTemplateColumns: "1fr 1fr" }}>
          <Field label="seq"><input value={seq} onChange={(e) => setSeq(Number(e.target.value || 0))} /></Field>
          <Field label="progressPct"><input value={progressPct} onChange={(e) => setProgressPct(Number(e.target.value || 0))} /></Field>
          <Field label="spentMinutes"><input value={spentMinutes} onChange={(e) => setSpentMinutes(Number(e.target.value || 0))} /></Field>
          <div />
          <div style={{ gridColumn: "1 / -1" }}>
            <Field label="title"><input value={title} onChange={(e) => setTitle(e.target.value)} /></Field>
          </div>
          <div style={{ gridColumn: "1 / -1" }}>
            <Field label="description"><textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={3} /></Field>
          </div>
        </div>
      </Dialog>

      <Dialog
        open={approveOpen}
        title={approveTarget ? `Approve · Worklog #${approveTarget.worklogId}` : "Approve"}
        onClose={() => setApproveOpen(false)}
        footer={
          <div className="row" style={{ justifyContent: "space-between" }}>
            <div style={{ color: "var(--muted)", fontSize: 12 }}>
              <span className="kbd">POST</span> <span className="kbd">/api/worklogs/{approveTarget?.worklogId}/approve</span>
            </div>
            <Button variant="primary" onClick={approve} disabled={!approveTarget}>승인</Button>
          </div>
        }
      >
        <div className="grid">
          <Field label="score">
            <select value={approveScore} onChange={(e) => setApproveScore(Number(e.target.value))}>
              {[1, 2, 3, 4, 5].map((n) => <option key={n} value={n}>{n}</option>)}
            </select>
          </Field>
          <Field label="commentTxt">
            <textarea value={approveComment} onChange={(e) => setApproveComment(e.target.value)} rows={4} placeholder='예: "approved by manager"' />
          </Field>
        </div>
      </Dialog>

      <Dialog
        open={rejectOpen}
        title={rejectTarget ? `Reject · Worklog #${rejectTarget.worklogId}` : "Reject"}
        onClose={() => setRejectOpen(false)}
        footer={
          <div className="row" style={{ justifyContent: "space-between" }}>
            <div style={{ color: "var(--muted)", fontSize: 12 }}>
              <span className="kbd">POST</span> <span className="kbd">/api/worklogs/{rejectTarget?.worklogId}/reject</span>
            </div>
            <Button variant="danger" onClick={reject} disabled={!rejectTarget || !rejectComment.trim()}>반려</Button>
          </div>
        }
      >
        <div className="grid">
          <Field label="commentTxt">
            <textarea value={rejectComment} onChange={(e) => setRejectComment(e.target.value)} rows={4} placeholder='예: "cross-team reject"' />
          </Field>
        </div>
      </Dialog>
    </div>
  );
}

export function DailyWorklogPage() {
  return <ToastProvider><Inner /></ToastProvider>;
}
