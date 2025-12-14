import * as React from "react";
import { API_BASE } from "../components/api";

function normalizeRole(raw: unknown): string | null {
  if (!raw) return null;

  if (Array.isArray(raw)) {
    for (const item of raw) {
      const norm = normalizeRole(item);
      if (norm) return norm;
    }
    return null;
  }

  let role = String(raw).trim();
  if (!role) return null;

  if (role.startsWith("ROLE_")) role = role.slice(5);
  role = role.toLowerCase();

  if (["admin", "administrator"].includes(role)) return "Admin";
  if (["user", "użytkownik", "viewer"].includes(role)) return "User";

  return role.charAt(0).toUpperCase() + role.slice(1);
}

function extractRoleLoose(obj: any): string | null {
  if (!obj || typeof obj !== "object") return null;

  if ("roles" in obj) {
    const maybe = normalizeRole(obj.roles);
    if (maybe) return maybe;
  }
  if ("role" in obj) {
    const maybe = normalizeRole(obj.role);
    if (maybe) return maybe;
  }
  if ("authorities" in obj) {
    const maybe = normalizeRole(obj.authorities);
    if (maybe) return maybe;
  }

  return null;
}

async function fetchMe(): Promise<any | null> {
  try {
    const res = await fetch(`${API_BASE}/api/me`, {
      method: "GET",
      credentials: "include",
      headers: { Accept: "application/json" },
    });

    if (!res.ok) return null;
    return await res.json();
  } catch {
    return null;
  }
}

export type AdminGuardState = "checking" | "allowed" | "redirected";

export function useAdminGuard() {
  const [state, setState] = React.useState<AdminGuardState>("checking");
  const [email, setEmail] = React.useState<string | null>(null);

  React.useEffect(() => {
    (async () => {
      try {
        const me = await fetchMe();

        if (!me) {
          window.location.href = "/login";
          setState("redirected");
          return;
        }

        const role = extractRoleLoose(me);
        if (role !== "Admin") {
          window.location.href = "/dashboard";
          setState("redirected");
          return;
        }

        const userEmail =
          (me.email as string | undefined) ??
          (me.userName as string | undefined) ??
          null;

        setEmail(userEmail ?? "Zalogowany administrator");
        setState("allowed");
      } catch (err) {
        console.error("Błąd przy sprawdzaniu /api/me:", err);
        window.location.href = "/login";
        setState("redirected");
      }
    })();
  }, []);

  return { state, email };
}
