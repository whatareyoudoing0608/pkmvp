import React from "react";
import { TaskStatus } from "../api/types";

export function StatusBadge({ status }: { status: TaskStatus }) {
  const cls = status === "DONE" ? "badge ok" : status === "IN_PROGRESS" ? "badge info" : status === "BLOCKED" || status === "CANCELED" ? "badge bad" : "badge warn";
  return <span className={cls}>{status}</span>;
}
