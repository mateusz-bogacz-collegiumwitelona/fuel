// app/components/useUserGuard.ts
import * as React from "react";
import { API_BASE } from "./api";

export type UserGuardState = "checking" | "allowed" | "redirected";

export function useUserGuard() {
  const [state, setState] = React.useState<UserGuardState>("checking");
  const [email, setEmail] = React.useState<string | null>(null);

  React.useEffect(() => {
    let isMounted = true;

    (async () => {
      try {
        const res = await fetch(`${API_BASE}/api/me`, {
          method: "GET",
          credentials: "include",
          headers: { Accept: "application/json" },
        });

        if (!res.ok) {
          if (typeof window !== "undefined") {
            window.location.href = "/login";
          }
          if (isMounted) setState("redirected");
          return;
        }

        const me = await res.json();
        if (!isMounted) return;

        const userEmail =
          (me.email as string | undefined) ??
          (me.userName as string | undefined) ??
          (me.sub as string | undefined) ??
          null;

        setEmail(userEmail ?? "Zalogowany użytkownik");
        setState("allowed");
      } catch (err) {
        console.error("Błąd podczas wywołania /api/me:", err);
        if (typeof window !== "undefined") {
          window.location.href = "/login";
        }
        if (isMounted) setState("redirected");
      }
    })();

    return () => {
      isMounted = false;
    };
  }, []);

  return { state, email };
}
