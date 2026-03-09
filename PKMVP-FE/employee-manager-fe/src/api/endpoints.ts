export const endpoints = {
  auth: {
    login: () => `/api/auth/login`,
    me: () => `/api/auth/me`
  },

  tasks: {
    list: (scope: "mine" | "team" | "all") => `/api/tasks?scope=${scope}`,
    create: () => `/api/tasks`,
    progressList: (taskId: number) => `/api/tasks/${taskId}/progress`,
    progressCreate: (taskId: number) => `/api/tasks/${taskId}/progress`,
    commentsList: (taskId: number) => `/api/tasks/${taskId}/comments`,
    commentsCreate: (taskId: number) => `/api/tasks/${taskId}/comments`,
    updateStatus: (taskId: number, status: string) => `/api/tasks/${taskId}/status?status=${encodeURIComponent(status)}`
  },

  projects: {
    list: () => `/api/projects`,
    create: () => `/api/projects`,
    boardsList: (projectId: number) => `/api/projects/${projectId}/boards`,
    boardsCreate: (projectId: number) => `/api/projects/${projectId}/boards`
  },

  boards: {
    sprintsList: (boardId: number) => `/api/boards/${boardId}/sprints`,
    sprintsCreate: (boardId: number) => `/api/boards/${boardId}/sprints`,
    issuesList: (boardId: number, sprintId?: number | null, status?: string | null) => {
      const params = new URLSearchParams();
      if (typeof sprintId === "number") params.set("sprintId", String(sprintId));
      if (status) params.set("status", status);
      const q = params.toString();
      return `/api/boards/${boardId}/issues${q ? `?${q}` : ""}`;
    },
    planIssue: (boardId: number, taskId: number) => `/api/boards/${boardId}/issues/${taskId}/plan`
  },

  sprints: {
    updateStatus: (sprintId: number) => `/api/sprints/${sprintId}/status`
  },

  notifications: {
    list: (unreadOnly: boolean, limit: number) => `/api/notifications?unreadOnly=${unreadOnly ? "true" : "false"}&limit=${limit}`,
    unreadCount: () => `/api/notifications/unread-count`,
    markRead: (notificationId: number) => `/api/notifications/${notificationId}/read`,
    markAllRead: () => `/api/notifications/read-all`
  },

  teamDirectory: {
    teams: () => `/api/team-directory/teams`,
    members: (teamId?: string, q?: string) => {
      const params = new URLSearchParams();
      if (teamId) params.set("teamId", teamId);
      if (q) params.set("q", q);
      return `/api/team-directory/members?${params.toString()}`;
    },
    usersFallback: (q?: string) => {
      const params = new URLSearchParams();
      if (q) params.set("q", q);
      return `/api/users?${params.toString()}`;
    }
  },

  worklogs: {
    list: (fromDate: string, toDate: string, scope: "mine" | "team" | "all") =>
      `/api/worklogs?fromDate=${encodeURIComponent(fromDate)}&toDate=${encodeURIComponent(toDate)}&scope=${scope}`,
    get: (worklogId: number) => `/api/worklogs/${worklogId}`,
    create: () => `/api/worklogs`,
    addItem: (worklogId: number) => `/api/worklogs/${worklogId}/items`,
    submit: (worklogId: number) => `/api/worklogs/${worklogId}/submit`,
    approve: (worklogId: number) => `/api/worklogs/${worklogId}/approve`,
    reject: (worklogId: number) => `/api/worklogs/${worklogId}/reject`
  }
};
