import * as React from "react";
import Header from "../Components/header";
import Footer from "../Components/footer";

const API_BASE = "http://localhost:5111";

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

  // typowy przypadek dla /api/auth/me: roles: ["User"]
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

function redirectByRole(role: string | null) {
  if (typeof window === "undefined") return;
  if (role === "Admin") window.location.href = "/admin-dashboard";
  else window.location.href = "/dashboard";
}

// pomocnik do pobrania danych zalogowanego usera z nowego endpointu
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


export default function Login() {
  const [email, setEmail] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [message, setMessage] = React.useState("");

  const handleLogin = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setMessage("Logowanie...");

    try {
      const response = await fetch(`${API_BASE}/api/login`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        body: JSON.stringify({ email, password }),
        credentials: "include",
      });

      let data: any = null;
      try {
        data = await response.json();
      } catch {
        data = null;
      }

      if (!response.ok) {
        const serverMsg =
          (data && (data.message || data.error)) ??
          (data && Array.isArray(data.errors) && data.errors.join(", ")) ??
          "Błędny email lub hasło";
        setMessage(serverMsg);
        return;
      }

      // zapamiętaj email (opcjonalne)
      if (data?.email) {
        try {
          localStorage.setItem("email", data.email);
        } catch {
          // ignore
        }
      }

      // 1. rolę spróbuj wyciągnąć bezpośrednio z odpowiedzi logowania
      const roleFromBody = extractRoleLoose(data);
      if (roleFromBody) {
        setMessage("Zalogowano pomyślnie!");
        redirectByRole(roleFromBody);
        return;
      }

      // 2. jeśli backend nie zwrócił roli w /api/login → użyj /api/auth/me
      const me = await fetchMe();
      if (me) {
        const meRole = extractRoleLoose(me);
        const meEmail = me.email ?? me.userName ?? null;

        if (meEmail) {
          try {
            localStorage.setItem("email", String(meEmail));
          } catch {
            // ignore
          }
        }

        setMessage("Zalogowano pomyślnie!");
        redirectByRole(meRole);
        return;
      }

      // 3. ostateczny fallback – brak roli, traktuj jako zwykłego usera
      setMessage("Zalogowano pomyślnie!");
      redirectByRole(null);
    } catch (error) {
      console.error(error);
      setMessage("Błąd połączenia z serwerem");
    }
  };

  // auto-redirect jeśli ktoś już jest zalogowany (na podstawie /api/auth/me)
  React.useEffect(() => {
    (async () => {
      const me = await fetchMe();
      if (me) {
        const meRole = extractRoleLoose(me);
        redirectByRole(meRole);
      }
    })();
  }, []);

  return (
    <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
      <Header />

      <div className="flex-grow flex justify-center items-center">
        <form
          onSubmit={handleLogin}
          className="bg-base-300 p-8 rounded-2xl shadow-lg flex flex-col gap-4 w-full max-w-sm"
        >
          <h2 className="text-2xl font-bold text-center mb-2">Logowanie</h2>

          <input
            type="email"
            placeholder="Email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            className="p-3 rounded-md bg-base-100 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
          />

          <input
            type="password"
            placeholder="Hasło"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            className="p-3 rounded-md bg-base-100 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
          />

          <button type="submit" className="btn btn-info">
            Zaloguj
          </button>

          {message && (
            <p className="text-center text-sm text-gray-300 mt-2">{message}</p>
          )}
        </form>
      </div>

      <Footer />
    </div>
  );
}
