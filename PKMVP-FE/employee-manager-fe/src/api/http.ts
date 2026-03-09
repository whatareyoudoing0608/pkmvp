import { tokenStore } from "../auth/tokenStore";

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? "").trim();

export class ApiError extends Error {
  status: number;
  body?: unknown;
  constructor(message: string, status: number, body?: unknown) {
    super(message);
    this.status = status;
    this.body = body;
  }
}

async function parseBody(res: Response) {
  const text = await res.text();
  if (!text) return null;
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

export async function apiFetch<T>(path: string, init?: RequestInit & { skipAuth?: boolean }): Promise<T> {
  const headers = new Headers(init?.headers ?? {});
  headers.set("Accept", "application/json");

  if (!init?.skipAuth) {
    const token = tokenStore.getAccessToken();
    if (token) headers.set("Authorization", `Bearer ${token}`);
  }

  const res = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers,
    mode: "cors"
  });

  const body = await parseBody(res);
  if (!res.ok) {
    throw new ApiError(typeof body === "string" ? body : `HTTP ${res.status}`, res.status, body);
  }

  return body as T;
}

export function getBaseUrl() {
  return baseUrl;
}
