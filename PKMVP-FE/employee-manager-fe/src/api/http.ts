import { tokenStore } from "../auth/tokenStore";

function resolveBaseUrl() {
  const configured = (import.meta.env.VITE_API_BASE_URL ?? "").trim();

  try {
    if (configured) {
      // Already has protocol (http:// or https://)
      if (/^https?:\/\//i.test(configured)) {
        return new URL(configured).origin;
      }

      // Has protocol but missing // (like http:10.109.25.200:8084)
      if (/^https?:/i.test(configured)) {
        const fixed = configured.replace(/^(https?):(?!\/\/)/, "$1://");
        return new URL(fixed).origin;
      }

      // No protocol, try to construct URL
      if (typeof window !== "undefined") {
        const url = new URL(`${window.location.protocol}//${configured}`);
        return url.origin;
      }

      return `http://${configured}`;
    }

    // Default: use current hostname + port 8084
    if (typeof window !== "undefined") {
      return `${window.location.protocol}//${window.location.hostname}:8084`;
    }

    return "";
  } catch {
    // Fallback if URL parsing fails
    if (typeof window !== "undefined") {
      return `${window.location.protocol}//${window.location.hostname}:8084`;
    }
    return "";
  }
}

const baseUrl = resolveBaseUrl();

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
