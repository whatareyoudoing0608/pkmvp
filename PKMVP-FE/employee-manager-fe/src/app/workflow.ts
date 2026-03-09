import { normalizeRole } from "../auth/roleGuard";
import { TaskStatus } from "../api/types";

const TRANSITIONS: Record<TaskStatus, TaskStatus[]> = {
  TODO: ["IN_PROGRESS", "BLOCKED", "CANCELED"],
  IN_PROGRESS: ["BLOCKED", "DONE", "CANCELED"],
  BLOCKED: ["IN_PROGRESS", "CANCELED"],
  DONE: ["IN_PROGRESS", "CANCELED"],
  CANCELED: ["IN_PROGRESS"]
};

export function canTransition(current: TaskStatus, next: TaskStatus, role?: string): boolean {
  const me = normalizeRole(role);

  if (current === next) return true;
  if (!TRANSITIONS[current]?.includes(next)) return false;

  if (me === "USER" && next === "CANCELED") return false;
  if (me === "USER" && current === "DONE" && next === "IN_PROGRESS") return false;

  return true;
}

export function getAllowedNextStatuses(current: TaskStatus, role?: string): TaskStatus[] {
  const all = TRANSITIONS[current] ?? [];
  return all.filter((s) => canTransition(current, s, role));
}

export function getSelectableStatuses(current: TaskStatus, role?: string): TaskStatus[] {
  const rest = getAllowedNextStatuses(current, role);
  return [current, ...rest.filter((x) => x !== current)];
}

export function getWorkflowSummary(role?: string): string {
  const me = normalizeRole(role);
  if (me === "USER") {
    return "USER: CANCELED 전이 불가, DONE 이슈 재오픈 불가";
  }
  return "MANAGER/ADMIN: 정의된 워크플로우 전이 허용";
}
