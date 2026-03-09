import React, { useEffect, useMemo, useState } from "react";
import { apiFetch } from "../api/http";
import { endpoints } from "../api/endpoints";
import { Task, TaskComment, TaskProgress, TaskStatus } from "../api/types";
import { Card } from "../components/Card";
import { DataTable } from "../components/DataTable";
import { StatusBadge } from "../components/StatusBadge";
import { Button } from "../components/Button";
import { Field } from "../components/Field";
import { Dialog } from "../components/Dialog";
import { ToastProvider, useToast } from "../components/Toast";
import { useAuth } from "../auth/AuthContext";
import { hasMinRole } from "../auth/roleGuard";
import { getSelectableStatuses } from "../app/workflow";

function Inner() {
  const { push } = useToast();
  const { user } = useAuth();
  const [scope, setScope] = useState<"mine" | "team" | "all">("mine");
  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState(false);
  const [selected, setSelected] = useState<Task | null>(null);
  const [progress, setProgress] = useState<TaskProgress[]>([]);
  const [comments, setComments] = useState<TaskComment[]>([]);
  const [progressOpen, setProgressOpen] = useState(false);
  const [authorId, setAuthorId] = useState<number>(1);
  const [status, setStatus] = useState<TaskStatus>("IN_PROGRESS");
  const [progressPct, setProgressPct] = useState<number>(50);
  const [spentMinutes, setSpentMinutes] = useState<number>(30);
  const [commentTxt, setCommentTxt] = useState<string>("");
  const [newComment, setNewComment] = useState<string>("");

  async function load() {
    setLoading(true);
    try {
      const res = await apiFetch<Task[]>(endpoints.tasks.list(scope));
      setTasks(res);
    } catch (e: any) {
      push({ type: "bad", message: e?.message ?? "Tasks 조회 실패" });
    } finally {
      setLoading(false);
    }
  }

  async function loadComments(taskId: number) {
    try {
      const rows = await apiFetch<TaskComment[]>(endpoints.tasks.commentsList(taskId));
      setComments(rows);
    } catch (e: any) {
      push({ type: "warn", message: e?.message ?? "Comment 조회 실패" });
      setComments([]);
    }
  }

  async function openProgress(task: Task) {
    setSelected(task);
    setStatus(task.status);
    setProgressOpen(true);
    try {
      const rows = await apiFetch<TaskProgress[]>(endpoints.tasks.progressList(task.taskId));
      setProgress(rows);
    } catch (e: any) {
      push({ type: "warn", message: e?.message ?? "Progress 조회 실패" });
      setProgress([]);
    }

    await loadComments(task.taskId);
  }

  async function addProgress() {
    if (!selected) return;
    try {
      await apiFetch(endpoints.tasks.progressCreate(selected.taskId), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ taskId: selected.taskId, authorId, status, progressPct, spentMinutes, commentTxt })
      });
      push({ type: "ok", message: "Progress 등록 완료" });
      await openProgress(selected);
      await load();
    } catch (e: any) {
      push({ type: "bad", message: e?.message ?? "Progress 등록 실패" });
    }
  }

  async function addComment() {
    if (!selected) return;
    if (!newComment.trim()) {
      push({ type: "warn", message: "댓글 내용을 입력하세요." });
      return;
    }

    try {
      await apiFetch(endpoints.tasks.commentsCreate(selected.taskId), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ content: newComment.trim() })
      });
      setNewComment("");
      push({ type: "ok", message: "댓글 등록 완료" });
      await loadComments(selected.taskId);
    } catch (e: any) {
      push({ type: "bad", message: e?.message ?? "댓글 등록 실패" });
    }
  }

  useEffect(() => {
    load();
  }, [scope]);

  useEffect(() => {
    const uid = Number(user?.userId ?? 0);
    if (Number.isFinite(uid) && uid > 0) {
      setAuthorId(uid);
    }
  }, [user?.userId]);

  const cols = useMemo(
    () => [
      { key: "taskId", title: "ID", width: "70px" },
      { key: "title", title: "Title" },
      { key: "priority", title: "Priority", width: "90px" },
      { key: "status", title: "Status", width: "140px", render: (t: Task) => <StatusBadge status={t.status} /> },
      {
        key: "progressPct",
        title: "Progress",
        width: "140px",
        render: (t: Task) => (
          <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
            <div style={{ flex: 1, height: 8, borderRadius: 999, background: "rgba(15,23,42,0.08)", overflow: "hidden" }}>
              <div style={{ width: `${Math.max(0, Math.min(100, t.progressPct ?? 0))}%`, height: "100%", background: "rgba(96,165,250,0.65)" }} />
            </div>
            <div style={{ width: 42, fontSize: 12, color: "var(--muted)" }}>{t.progressPct ?? 0}%</div>
          </div>
        )
      }
    ],
    []
  );

  return (
    <div className="grid">
      <Card
        title="Tasks"
        right={
          <div className="row">
            <select value={scope} onChange={(e) => setScope(e.target.value as any)} style={{ width: 160 }}>
              <option value="mine">scope=mine</option>
              {user && hasMinRole(user.role, "MANAGER") && <option value="team">scope=team</option>}
              {user && hasMinRole(user.role, "ADMIN") && <option value="all">scope=all</option>}
            </select>
            <Button onClick={load} disabled={loading}>{loading ? "로딩..." : "새로고침"}</Button>
          </div>
        }
      >
        <DataTable<Task> columns={cols} rows={tasks} rowKey={(t) => t.taskId} onRowClick={(t) => openProgress(t)} />
      </Card>

      <Dialog
        open={progressOpen}
        title={selected ? `Task #${selected.taskId} · Progress` : "Progress"}
        onClose={() => {
          setProgressOpen(false);
          setNewComment("");
        }}
        footer={
          <div className="row" style={{ justifyContent: "space-between" }}>
            <div style={{ color: "var(--muted)", fontSize: 12 }}>
              POST <span className="kbd">/api/tasks/{selected?.taskId}/progress</span>
            </div>
            <Button variant="primary" onClick={addProgress} disabled={!selected}>등록</Button>
          </div>
        }
      >
        <div className="grid" style={{ gridTemplateColumns: "1fr 1fr", alignItems: "end" }}>
          <Field label="authorId"><input value={authorId} onChange={(e) => setAuthorId(Number(e.target.value || 0))} /></Field>
          <Field label="status"><select value={status} onChange={(e) => setStatus(e.target.value as TaskStatus)}>{getSelectableStatuses(selected?.status ?? "TODO", user?.role).map((s) => <option key={s} value={s}>{s}</option>)}</select></Field>
          <Field label="progressPct"><input value={progressPct} onChange={(e) => setProgressPct(Number(e.target.value || 0))} /></Field>
          <Field label="spentMinutes"><input value={spentMinutes} onChange={(e) => setSpentMinutes(Number(e.target.value || 0))} /></Field>
          <div style={{ gridColumn: "1 / -1" }}><Field label="commentTxt"><textarea value={commentTxt} onChange={(e) => setCommentTxt(e.target.value)} rows={3} /></Field></div>
        </div>

        <div className="hr" />

        <Card title="Progress History">
          <table className="table">
            <thead><tr><th style={{ width: 120 }}>Status</th><th style={{ width: 110 }}>Pct</th><th style={{ width: 120 }}>Minutes</th><th>Comment</th><th style={{ width: 190 }}>Created</th></tr></thead>
            <tbody>
              {progress.length === 0 ? (
                <tr><td colSpan={5} style={{ color: "var(--muted)", padding: 14 }}>이력 없음 또는 미구현</td></tr>
              ) : progress.map((p, idx) => (
                <tr key={p.progressId ?? idx}><td><StatusBadge status={p.status} /></td><td>{p.progressPct}%</td><td>{p.spentMinutes}</td><td>{p.commentTxt ?? ""}</td><td style={{ color: "var(--muted)", fontSize: 12 }}>{p.createdAt ?? ""}</td></tr>
              ))}
            </tbody>
          </table>
        </Card>

        <div className="hr" />

        <Card title="Comments" right={<Button onClick={() => selected && loadComments(selected.taskId)} disabled={!selected}>새로고침</Button>}>
          <div className="grid" style={{ gridTemplateColumns: "1fr auto", alignItems: "end", marginBottom: 12 }}>
            <Field label="새 댓글">
              <textarea value={newComment} onChange={(e) => setNewComment(e.target.value)} rows={3} placeholder="협업 메모/논의 내용을 남겨주세요." />
            </Field>
            <Button variant="primary" onClick={addComment} disabled={!selected}>댓글 등록</Button>
          </div>

          <table className="table">
            <thead><tr><th style={{ width: 120 }}>Author</th><th>Content</th><th style={{ width: 190 }}>Created</th></tr></thead>
            <tbody>
              {comments.length === 0 ? (
                <tr><td colSpan={3} style={{ color: "var(--muted)", padding: 14 }}>댓글이 없습니다.</td></tr>
              ) : comments.map((c) => (
                <tr key={c.commentId}>
                  <td>#{c.authorId}</td>
                  <td>{c.content}{c.editedYn === "Y" ? <span style={{ color: "var(--muted)", marginLeft: 8, fontSize: 12 }}>(edited)</span> : null}</td>
                  <td style={{ color: "var(--muted)", fontSize: 12 }}>{c.createdAt ?? ""}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      </Dialog>
    </div>
  );
}

export function TasksPage() {
  return <ToastProvider><Inner /></ToastProvider>;
}

