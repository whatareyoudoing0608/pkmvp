export type LoginRequest = { loginId: string; password: string };
export type LoginResponse = {
  accessToken: string;
  accessTokenExpiresAt?: string;
  refreshToken?: string;
  refreshTokenExpiresAt?: string;
};

export type AuthMeResponse = {
  userId: number;
  role: string;
  teamId?: string;
};

export type TaskStatus = "TODO" | "IN_PROGRESS" | "BLOCKED" | "DONE" | "CANCELED";

export type Task = {
  taskId: number;
  taskType?: string | null;
  issueKey?: string | null;
  title: string;
  description?: string | null;
  priority: number;
  status: TaskStatus;
  progressPct: number;
  reporterId?: number | null;
  assigneeId?: number | null;
  startDate?: string | null;
  dueDate?: string | null;
  doneDate?: string | null;
  createdAt?: string;
  updatedAt?: string;
};

export type TaskProgress = {
  progressId?: number;
  taskId: number;
  authorId: number;
  status: TaskStatus;
  progressPct: number;
  spentMinutes: number;
  commentTxt?: string | null;
  createdAt?: string;
};

export type TaskComment = {
  commentId: number;
  taskId: number;
  parentCommentId?: number | null;
  authorId: number;
  content: string;
  editedYn?: string;
  deletedYn?: string;
  createdAt?: string;
  updatedAt?: string;
};

export type AppNotification = {
  notificationId: number;
  userId: number;
  type: string;
  title: string;
  message?: string | null;
  targetType?: string | null;
  targetId?: number | null;
  isRead: string;
  createdAt?: string;
};

export type AgileProject = {
  projectId: number;
  projectKey: string;
  name: string;
  description?: string | null;
  leadUserId?: number | null;
  createdAt?: string;
  updatedAt?: string;
};

export type AgileBoard = {
  boardId: number;
  projectId: number;
  name: string;
  boardType: "KANBAN" | "SCRUM";
  createdAt?: string;
  updatedAt?: string;
};

export type AgileSprint = {
  sprintId: number;
  boardId: number;
  name: string;
  goal?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  status: "PLANNED" | "ACTIVE" | "CLOSED";
  createdAt?: string;
  updatedAt?: string;
};

export type BoardIssue = {
  taskId: number;
  issueKey: string;
  taskType?: string | null;
  title: string;
  description?: string | null;
  status: TaskStatus;
  priority: number;
  progressPct: number;
  reporterId: number;
  assigneeId?: number | null;
  projectId?: number | null;
  sprintId?: number | null;
  storyPoints?: number | null;
  dueDate?: string | null;
  updatedAt?: string;
};

export type Team = { teamId: string; teamName: string };
export type Member = { userId: number; displayName: string; teamId: string; role?: string };

export type WorklogScope = "mine" | "team" | "all";

export type WorklogHeader = {
  worklogId: number;
  workDate: string;
  summary: string;
  status?: string;
  writerId?: number;
  writerName?: string;
  teamId?: string;
  createdAt?: string;
  updatedAt?: string;
};

export type WorklogCreateRequest = {
  workDate: string;
  summary: string;
};

export type WorklogItemCreateRequest = {
  seq: number;
  title: string;
  description: string;
  spentMinutes: number;
  progressPct: number;
};

export type WorklogApproveRequest = {
  score: number;
  commentTxt: string;
};

export type WorklogRejectRequest = {
  commentTxt: string;
};

