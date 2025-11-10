import * as React from "react";
import Header from "../Components/header";
import Footer from "../Components/footer";

const API_BASE = "http://localhost:5111";

function parseJwt(token: string | null) {
  if (!token) return null;
  try {
    const payload = token.split(".")[1];
    return JSON.parse(decodeURIComponent(escape(atob(payload))));
  } catch (e) {
    console.error("Nie można zdekodować tokena JWT:", e);
    return null;
  }
}

function extractRoleFromDecoded(decoded: any): string | null {
  if (!decoded) return null;

  const possibleKeys = [
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
    "role",
    "roles",
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role",
  ];

  for (const key of possibleKeys) {
    const val = decoded[key];
    if (!val) continue;
    if (Array.isArray(val)) return val[0] ?? null;
    if (typeof val === "string") return val;
    if (typeof val === "object" && val !== null) {
      const first = Object.values(val)[0];
      if (typeof first === "string") return first;
    }
  }

  if (decoded["roles"] && Array.isArray(decoded["roles"])) return decoded["roles"][0];
  return null;
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
        if (data && (data.message || data.errors)) {
          const serverMsg = data.message ?? (Array.isArray(data.errors) ? data.errors.join(", ") : JSON.stringify(data.errors));
          setMessage(serverMsg);
        } else {
          setMessage("Błędny email lub hasło");
        }
        return;
      }

      if (data && data.token) {
        const token: string = data.token;
        let expiration: string | null = data.expiration ?? null;
        if (!expiration) {
          const decoded = parseJwt(token);
          if (decoded && decoded.exp) {
            expiration = new Date(decoded.exp * 1000).toISOString();
          }
        }

        localStorage.setItem("token", token);
        if (expiration) localStorage.setItem("token_expiration", expiration);
        if (data.email) localStorage.setItem("email", data.email);

        const decoded = parseJwt(token);
        const role = extractRoleFromDecoded(decoded);

        setMessage("Zalogowano pomyślnie!");
        setTimeout(() => {
          if (typeof window !== "undefined") {
            if (role === "Admin") window.location.href = "/admin-dashboard";
            else window.location.href = "/dashboard";
          }
        }, 700);

        return;
      }

      setMessage("Zalogowano pomyślnie!");
      setTimeout(() => {
        if (typeof window !== "undefined") window.location.href = "/dashboard";
      }, 700);

    } catch (error) {
      console.error(error);
      setMessage("Błąd połączenia z serwerem");
    }
  };


  React.useEffect(() => {
    (async () => {
      const token = localStorage.getItem("token");
      const expiration = localStorage.getItem("token_expiration");
      if (token && expiration && new Date(expiration) > new Date()) {
        const decoded = parseJwt(token);
        const role = extractRoleFromDecoded(decoded);
        if (typeof window !== "undefined") {
          if (role === "Admin") window.location.href = "/admin-dashboard";
          else window.location.href = "/dashboard";
        }
        return;
      }

      try {
        const meRes = await fetch(`${API_BASE}/api/user/me`, {
          method: "GET",
          credentials: "include",
          headers: { Accept: "application/json" },
        });
        if (meRes.ok) {
          if (typeof window !== "undefined") window.location.href = "/dashboard";
        }
      } catch (err) {
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

          <button type="submit" className="btn btn-info">Zaloguj</button>

          {message && <p className="text-center text-sm text-gray-300 mt-2">{message}</p>}
        </form>
      </div>

      <Footer />
    </div>
  );
}
