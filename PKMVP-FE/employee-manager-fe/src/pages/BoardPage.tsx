import React, { useEffect, useMemo, useState } from "react";
import { apiFetch } from "../api/http";
import { endpoints } from "../api/endpoints";
import { AgileBoard, AgileProject, AgileSprint, BoardIssue, Task, TaskStatus } from "../api/types";
import { Button } from "../components/Button";
import { Card } from "../components/Card";
import { Field } from "../components/Field";
import { ToastProvider, useToast } from "../components/Toast";
import { useAuth } from "../auth/AuthContext";
import { hasMinRole } from "../auth/roleGuard";
import { canTransition, getAllowedNextStatuses, getWorkflowSummary } from "../app/workflow";

const ISSUE_STATUSES: TaskStatus[] = ["TODO", "IN_PROGRESS", "BLOCKED", "DONE", "CANCELED"];
const ISSUE_TYPES = ["EPIC", "STORY", "TASK", "SUBTASK", "BUG", "SPIKE", "DOC", "RESEARCH"] as const;

function Inner() {
  const { push } = useToast();
  const { user } = useAuth();
  const canManage = !!user && hasMinRole(user.role, "MANAGER");
  const workflowSummary = getWorkflowSummary(user?.role);

  const [projects, setProjects] = useState<AgileProject[]>([]);
  const [boards, setBoards] = useState<AgileBoard[]>([]);
  const [sprints, setSprints] = useState<AgileSprint[]>([]);
  const [issues, setIssues] = useState<BoardIssue[]>([]);

  const [selectedProjectId, setSelectedProjectId] = useState<number | null>(null);
  const [selectedBoardId, setSelectedBoardId] = useState<number | null>(null);
  const [sprintFilterId, setSprintFilterId] = useState<number | null>(null);
  const [statusFilter, setStatusFilter] = useState<"" | TaskStatus>("");

  const [newProjectKey, setNewProjectKey] = useState("PKMVP");
  const [newProjectName, setNewProjectName] = useState("PKMVP Team");
  const [newBoardName, setNewBoardName] = useState("Main Board");
  const [newBoardType, setNewBoardType] = useState<"KANBAN" | "SCRUM">("KANBAN");
  const [newSprintName, setNewSprintName] = useState("Sprint 1");
  const [newSprintGoal, setNewSprintGoal] = useState("");

  const [newIssueTitle, setNewIssueTitle] = useState("");
  const [newIssueDescription, setNewIssueDescription] = useState("");
  const [newIssuePriority, setNewIssuePriority] = useState(3);
  const [newIssueType, setNewIssueType] = useState<(typeof ISSUE_TYPES)[number]>("TASK");
  const [newIssueAssigneeId, setNewIssueAssigneeId] = useState("");
  const [newIssueSprintId, setNewIssueSprintId] = useState<string>("");

  const [planTaskId, setPlanTaskId] = useState("");
  const [planSprintId, setPlanSprintId] = useState<string>("");

  const [loading, setLoading] = useState(false);
  const [draggingTaskId, setDraggingTaskId] = useState<number | null>(null);

  const sprintMap = useMemo(() => {
    const m = new Map<number, AgileSprint>();
    sprints.forEach((s) => m.set(s.sprintId, s));
    return m;
  }, [sprints]);

  const grouped = useMemo(() => {
    const map: Record<TaskStatus, BoardIssue[]> = {
      TODO: [],
      IN_PROGRESS: [],
      BLOCKED: [],
      DONE: [],
      CANCELED: []
    };

    issues.forEach((issue) => {
      const key = issue.status as TaskStatus;
      if (ISSUE_STATUSES.includes(key)) {
        map[key].push(issue);
      }
    });

    return map;
  }, [issues]);

  async function loadProjects(preferredProjectId?: number) {
    try {
      const rows = await apiFetch<AgileProject[]>(endpoints.projects.list());
      setProjects(rows);

      if (rows.length === 0) {
        setSelectedProjectId(null);
        setBoards([]);
        setSelectedBoardId(null);
        return;
      }

      const nextProjectId =
        typeof preferredProjectId === "number"
          ? preferredProjectId
          : (selectedProjectId && rows.some((p) => p.projectId === selectedProjectId)
            ? selectedProjectId
            : rows[0].projectId);

      setSelectedProjectId(nextProjectId);
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : "프로젝트 조회 실패";
      push({ type: "bad", message: msg });
    }
  }

  async function loadBoards(projectId: number, preferredBoardId?: number) {
    try {
      const rows = await apiFetch<AgileBoard[]>(endpoints.projects.boardsList(projectId));
      setBoards(rows);

      if (rows.length === 0) {
        setSelectedBoardId(null);
        setSprints([]);
        setIssues([]);
        return;
      }

      const nextBoardId =
        typeof preferredBoardId === "number"
          ? preferredBoardId
          : (selectedBoardId && rows.some((b) => b.boardId === selectedBoardId)
            ? selectedBoardId
            : rows[0].boardId);

      setSelectedBoardId(nextBoardId);
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : "보드 조회 실패";
      push({ type: "bad", message: msg });
    }
  }

  async function loadSprints(boardId: number) {
    try {
      const rows = await apiFetch<AgileSprint[]>(endpoints.boards.sprintsList(boardId));
      setSprints(rows);
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : "스프린트 조회 실패";
      push({ type: "bad", message: msg });
      setSprints([]);
    }
  }

  async function loadIssues(boardId: number, sprintId: number | null, status: "" | TaskStatus) {
    try {
      setLoading(true);
      const rows = await apiFetch<BoardIssue[]>(endpoints.boards.issuesList(boardId, sprintId, status || null));
      setIssues(rows);
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : "이슈 조회 실패";
      push({ type: "bad", message: msg });
      setIssues([]);
    } finally {
      setLoading(false);
    }
  }

  async function createProject() {
    if (!canManage) return;
    if (!newProjectKey.trim() || !newProjectName.trim()) {
      push({ type: "warn", message: "projectKey/name을 입력하세요." });
      return;
    }

    try {
      const created = await apiFetch<AgileProject>(endpoints.projects.create(), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          projectKey: newProjectKey.trim(),
          name: newProjectName.trim()
        })
      });

      push({ type: "ok", message: `프로젝트 생성: ${created.projectKey}` });
      await loadProjects(created.projectId);
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : "프로젝트 생성 실패";
      push({ type: "bad", message: msg });
    }
  }

  async function createBoard() {
    if (!canManage) return;
    if (!selectedProjectId) {
      push({ type: "warn", message: "프로젝트를 먼저 선택하세요." });
      return;
    }
    if (!newBoardName.trim()) {
      push({ type: "warn", message: "보드 이름을 입력하세요." });
      return;
    }

    try {
      const created = await apiFetch<AgileBoard>(endpoints.projects.boardsCreate(selectedProjectId), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: newBoardName.trim(),
          boardType: newBoardType
        })
      });

      push({ type: "ok", message: `보드 생성: ${created.name}` });
      await loadBoards(selectedProjectId, created.boardId);
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : "보드 생성 실패";
      push({ type: "bad", message: msg });
    }
  }

  async function createSprint() {
    if (!canManage) return;
    if (!selectedBoardId) {
      push({ type: "warn", message: "보드를 먼저 선택하세요." });
      return;
    }
    if (!newSprintName.trim()) {
      push({ type: "warn", message: "스프린트 이름을 입력하세요." });
      return;
    }

    try {
      const created = await apiFetch<AgileSprint>(endpoints.boards.sprintsCreate(selectedBoardId), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: newSprintName.trim(),
          goal: newSprintGoal.trim() || null
        })
      });

      push({ type: "ok", message: `스프린트 생성: ${created.name}` });
      await loadSprints(selectedBoardId);
      setPlanSprintId(String(created.sprintId));
      setNewIssueSprintId(String(created.sprintId));
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : "스프린트 생성 실패";
      push({ type: "bad", message: msg });
    }
  }

  async function createIssueForBoard() {
    if (!canManage) return;
    if (!selectedBoardId) {
      push({ type: "warn", message: "보드를 먼저 선택하세요." });
      return;
    }
    if (!newIssueTitle.trim()) {
      push({ type: "warn", message: "이슈 제목을 입력하세요." });
      return;
    }

    if (newIssuePriority < 1 || newIssuePriority > 5) {
      push({ type: "warn", message: "priority는 1~5 범위여야 합니다." });
      return;
    }

    const assigneeId = newIssueAssigneeId.trim() ? Number(newIssueAssigneeId.trim()) : null;
    if (assigneeId !== null && (!Number.isFinite(assigneeId) || assigneeId <= 0)) {
      push({ type: "warn", message: "assigneeId는 양수여야 합니다." });
      return;
    }

    const sprintId = newIssueSprintId ? Number(newIssueSprintId) : null;

    try {
      const created = await apiFetch<Task>(endpoints.tasks.create(), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          title: newIssueTitle.trim(),
          description: newIssueDescription.trim() || null,
          priority: newIssuePriority,
          status: "TODO",
          progressPct: 0,
          taskType: newIssueType,
          assigneeId
        })
      });

      await apiFetch(endpoints.boards.planIssue(selectedBoardId, created.taskId), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ sprintId })
      });

      push({ type: "ok", message: `이슈 생성 완료: TASK-${created.taskId}` });
      await loadIssues(selectedBoardId, sprintFilterId, statusFilter);

      setNewIssueTitle("");
      setNewIssueDescription("");
      setNewIssueAssigneeId("");
      setNewIssueType("TASK");
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : "보드 이슈 생성 실패";
      push({ type: "bad", message: msg });
    }
  }

  async function updateSprintStatus(sprintId: number, status: "PLANNED" | "ACTIVE" | "CLOSED") {
    if (!canManage) return;
    try {
      await apiFetch<AgileSprint>(endpoints.sprints.updateStatus(sprintId), {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ status })
      });

      if (selectedBoardId) {
        await loadSprints(selectedBoardId);
      }
      push({ type: "ok", message: `스프린트 상태 변경: ${status}` });
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : "스프린트 상태 변경 실패";
      push({ type: "bad", message: msg });
    }
  }

  async function planIssue() {
    if (!canManage) return;
    if (!selectedBoardId) {
      push({ type: "warn", message: "보드를 먼저 선택하세요." });
      return;
    }

    const taskId = Number(planTaskId);
    if (!Number.isFinite(taskId) || taskId <= 0) {
      push({ type: "warn", message: "유효한 Task ID를 입력하세요." });
      return;
    }

    const sprintId = planSprintId ? Number(planSprintId) : null;

    try {
      await apiFetch(endpoints.boards.planIssue(selectedBoardId, taskId), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ sprintId })
      });

      push({ type: "ok", message: `Task #${taskId} 계획 반영 완료` });
      await loadIssues(selectedBoardId, sprintFilterId, statusFilter);
      setPlanTaskId("");
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : "Task 계획 반영 실패";
      push({ type: "bad", message: msg });
    }
  }

  async function moveIssueStatus(taskId: number, status: TaskStatus) {
    if (!selectedBoardId) return;

    try {
      await apiFetch(endpoints.tasks.updateStatus(taskId, status), { method: "PATCH" });
      await loadIssues(selectedBoardId, sprintFilterId, statusFilter);
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : "상태 변경 실패";
      push({ type: "bad", message: msg });
    }
  }

  function onDropStatus(e: React.DragEvent<HTMLElement>, targetStatus: TaskStatus) {
    e.preventDefault();
    const raw = e.dataTransfer.getData("text/plain");
    const taskId = Number(raw || draggingTaskId || 0);
    setDraggingTaskId(null);

    if (!Number.isFinite(taskId) || taskId <= 0) return;

    const issue = issues.find((x) => x.taskId === taskId);
    if (!issue) return;
    if (issue.status === targetStatus) return;

    if (!canTransition(issue.status, targetStatus, user?.role)) {
      push({ type: "warn", message: `전이 불가: ${issue.status} -> ${targetStatus}` });
      return;
    }

    void moveIssueStatus(taskId, targetStatus);
  }

  useEffect(() => {
    void loadProjects();
  }, []);

  useEffect(() => {
    if (selectedProjectId === null) return;
    void loadBoards(selectedProjectId);
  }, [selectedProjectId]);

  useEffect(() => {
    if (selectedBoardId === null) {
      setSprints([]);
      setIssues([]);
      return;
    }

    setSprintFilterId(null);
    setStatusFilter("");
    void loadSprints(selectedBoardId);
    void loadIssues(selectedBoardId, null, "");
  }, [selectedBoardId]);

  useEffect(() => {
    if (selectedBoardId === null) return;
    void loadIssues(selectedBoardId, sprintFilterId, statusFilter);
  }, [sprintFilterId, statusFilter]);

  return (
    <div className="grid">
      <Card
        title="Planning Control"
        right={
          <div className="row">
            <Button onClick={() => void loadProjects()} disabled={loading}>새로고침</Button>
          </div>
        }
      >
        <div className="grid board-control-grid">
          <Field label="Project">
            <select value={selectedProjectId ?? ""} onChange={(e) => setSelectedProjectId(e.target.value ? Number(e.target.value) : null)}>
              {projects.length === 0 && <option value="">(프로젝트 없음)</option>}
              {projects.map((p) => <option key={p.projectId} value={p.projectId}>{p.projectKey} - {p.name}</option>)}
            </select>
          </Field>

          <Field label="Board">
            <select value={selectedBoardId ?? ""} onChange={(e) => setSelectedBoardId(e.target.value ? Number(e.target.value) : null)}>
              {boards.length === 0 && <option value="">(보드 없음)</option>}
              {boards.map((b) => <option key={b.boardId} value={b.boardId}>{b.name} [{b.boardType}]</option>)}
            </select>
          </Field>

          <Field label="Sprint Filter">
            <select value={sprintFilterId ?? ""} onChange={(e) => setSprintFilterId(e.target.value ? Number(e.target.value) : null)}>
              <option value="">ALL</option>
              {sprints.map((s) => <option key={s.sprintId} value={s.sprintId}>{s.name} ({s.status})</option>)}
            </select>
          </Field>

          <Field label="Status Filter">
            <select value={statusFilter} onChange={(e) => setStatusFilter((e.target.value || "") as "" | TaskStatus)}>
              <option value="">ALL</option>
              {ISSUE_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
            </select>
          </Field>
        </div>
      </Card>

      {canManage ? (
        <Card title="Manager Actions">
          <div className="grid board-action-grid">
            <div className="card card-body">
              <div className="field-label" style={{ marginBottom: 8 }}>Create Project</div>
              <div className="grid">
                <input value={newProjectKey} onChange={(e) => setNewProjectKey(e.target.value.toUpperCase())} placeholder="PKMVP" />
                <input value={newProjectName} onChange={(e) => setNewProjectName(e.target.value)} placeholder="Project name" />
                <Button onClick={createProject}>프로젝트 생성</Button>
              </div>
            </div>

            <div className="card card-body">
              <div className="field-label" style={{ marginBottom: 8 }}>Create Board</div>
              <div className="grid">
                <input value={newBoardName} onChange={(e) => setNewBoardName(e.target.value)} placeholder="Board name" />
                <select value={newBoardType} onChange={(e) => setNewBoardType(e.target.value as "KANBAN" | "SCRUM")}>
                  <option value="KANBAN">KANBAN</option>
                  <option value="SCRUM">SCRUM</option>
                </select>
                <Button onClick={createBoard} disabled={!selectedProjectId}>보드 생성</Button>
              </div>
            </div>

            <div className="card card-body">
              <div className="field-label" style={{ marginBottom: 8 }}>Create Sprint</div>
              <div className="grid">
                <input value={newSprintName} onChange={(e) => setNewSprintName(e.target.value)} placeholder="Sprint name" />
                <input value={newSprintGoal} onChange={(e) => setNewSprintGoal(e.target.value)} placeholder="Goal (optional)" />
                <Button onClick={createSprint} disabled={!selectedBoardId}>스프린트 생성</Button>
              </div>
            </div>

            <div className="card card-body">
              <div className="field-label" style={{ marginBottom: 8 }}>Create Issue In Board</div>
              <div className="grid">
                <input value={newIssueTitle} onChange={(e) => setNewIssueTitle(e.target.value)} placeholder="Issue title" />
                <textarea value={newIssueDescription} onChange={(e) => setNewIssueDescription(e.target.value)} rows={2} placeholder="Description (optional)" />
                <div className="row" style={{ gap: 8 }}>
                  <select style={{ width: 130 }} value={newIssueType} onChange={(e) => setNewIssueType(e.target.value as (typeof ISSUE_TYPES)[number])}>
                    {ISSUE_TYPES.map((x) => <option key={x} value={x}>{x}</option>)}
                  </select>
                  <input style={{ width: 90 }} value={newIssuePriority} onChange={(e) => setNewIssuePriority(Number(e.target.value || 0))} placeholder="priority" />
                  <input style={{ width: 130 }} value={newIssueAssigneeId} onChange={(e) => setNewIssueAssigneeId(e.target.value)} placeholder="assigneeId" />
                </div>
                <select value={newIssueSprintId} onChange={(e) => setNewIssueSprintId(e.target.value)}>
                  <option value="">Backlog (no sprint)</option>
                  {sprints.map((s) => <option key={s.sprintId} value={s.sprintId}>{s.name}</option>)}
                </select>
                <Button onClick={createIssueForBoard} disabled={!selectedBoardId}>이슈 생성 + 보드 연결</Button>
              </div>
            </div>

            <div className="card card-body">
              <div className="field-label" style={{ marginBottom: 8 }}>Plan Existing Task</div>
              <div className="grid">
                <input value={planTaskId} onChange={(e) => setPlanTaskId(e.target.value)} placeholder="Task ID" />
                <select value={planSprintId} onChange={(e) => setPlanSprintId(e.target.value)}>
                  <option value="">Backlog (no sprint)</option>
                  {sprints.map((s) => <option key={s.sprintId} value={s.sprintId}>{s.name}</option>)}
                </select>
                <Button onClick={planIssue} disabled={!selectedBoardId}>Task 계획 반영</Button>
              </div>
            </div>
          </div>
        </Card>
      ) : (
        <Card title="Manager Actions">
          <div style={{ color: "var(--muted)", fontSize: 13 }}>
            프로젝트/보드/스프린트 생성과 Task 계획 반영은 `MANAGER` 이상 권한이 필요합니다.
          </div>
        </Card>
      )}

      <Card title="Sprints">
        <table className="table">
          <thead>
            <tr>
              <th style={{ width: 90 }}>ID</th>
              <th>Name</th>
              <th style={{ width: 120 }}>Status</th>
              <th style={{ width: 300 }}>Action</th>
            </tr>
          </thead>
          <tbody>
            {sprints.length === 0 ? (
              <tr><td colSpan={4} style={{ color: "var(--muted)", padding: 14 }}>스프린트가 없습니다.</td></tr>
            ) : sprints.map((s) => (
              <tr key={s.sprintId}>
                <td>#{s.sprintId}</td>
                <td>{s.name}</td>
                <td><span className="badge info">{s.status}</span></td>
                <td>
                  <div className="row">
                    <Button onClick={() => void updateSprintStatus(s.sprintId, "PLANNED")} disabled={!canManage}>PLANNED</Button>
                    <Button onClick={() => void updateSprintStatus(s.sprintId, "ACTIVE")} disabled={!canManage}>ACTIVE</Button>
                    <Button onClick={() => void updateSprintStatus(s.sprintId, "CLOSED")} disabled={!canManage}>CLOSED</Button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>

      <Card title="Kanban Board" right={<div style={{ color: "var(--muted)", fontSize: 12 }}>
          {workflowSummary}
        </div>}>
        <div className="kanban-grid">
          {ISSUE_STATUSES.map((status) => (
            <section
              key={status}
              className="kanban-col"
              onDragOver={(e) => e.preventDefault()}
              onDrop={(e) => onDropStatus(e, status)}
            >
              <header className="kanban-col-head">
                <div>{status}</div>
                <span className="badge">{grouped[status].length}</span>
              </header>
              <div className="kanban-cards">
                {grouped[status].map((issue) => {
                  const sprintName = issue.sprintId ? sprintMap.get(issue.sprintId)?.name : null;
                  const allowedNext = getAllowedNextStatuses(issue.status, user?.role);
                  const canDrag = allowedNext.length > 0;
                  return (
                    <article
                      key={issue.taskId}
                      className="kanban-card"
                      draggable={canDrag}
                      title={canDrag ? "drag to allowed status" : "현재 권한에서 이동 불가"}
                      onDragStart={(e) => {
                        if (!canDrag) {
                          e.preventDefault();
                          return;
                        }
                        e.dataTransfer.setData("text/plain", String(issue.taskId));
                        setDraggingTaskId(issue.taskId);
                      }}
                      onDragEnd={() => setDraggingTaskId(null)}
                    >
                      <div className="kanban-issue-key">{issue.issueKey}</div>
                      <div className="kanban-issue-title">{issue.title}</div>
                      <div className="row" style={{ justifyContent: "space-between", marginTop: 8 }}>
                        <span className="badge">P{issue.priority}</span>
                        {issue.taskType ? <span className="badge">{issue.taskType}</span> : <span className="badge">TASK</span>}
                      </div>
                      <div style={{ marginTop: 8, color: "var(--muted)", fontSize: 12 }}>
                        assignee: {issue.assigneeId ?? "-"} · sprint: {sprintName ?? "backlog"}
                      </div>
                      <div style={{ marginTop: 6, color: "var(--muted2)", fontSize: 11 }}>
                        next: {allowedNext.length > 0 ? allowedNext.join(", ") : "(none)"}
                      </div>
                    </article>
                  );
                })}
              </div>
            </section>
          ))}
        </div>
      </Card>
    </div>
  );
}

export function BoardPage() {
  return <ToastProvider><Inner /></ToastProvider>;
}


