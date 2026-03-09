const KEY = "employee_manager_tokens_v1";

type StoredTokens = {
  accessToken?: string;
  refreshToken?: string;
  accessTokenExpiresAt?: string;
  refreshTokenExpiresAt?: string;
  userId?: string;
  displayName?: string;
  teamId?: string;
  role?: string;
};

function read(): StoredTokens {
  const raw = localStorage.getItem(KEY);
  if (!raw) return {};
  try {
    return JSON.parse(raw) as StoredTokens;
  } catch {
    return {};
  }
}

export const tokenStore = {
  get() {
    return read();
  },
  set(tokens: StoredTokens) {
    localStorage.setItem(KEY, JSON.stringify(tokens));
  },
  clear() {
    localStorage.removeItem(KEY);
  },
  getAccessToken() {
    return read().accessToken;
  },
  getRole() {
    return read().role;
  },
  getTeamId() {
    return read().teamId;
  },
  getUserId() {
    return read().userId;
  }
};
