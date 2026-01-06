import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";
import { useTranslation } from "react-i18next";
import FacebookButton from "../components/FacebookLoginButton";
import { API_BASE } from "../components/api";
import GoogleLoginButto from "../components/GoogleLoginButton";

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

function redirectByRole(role: string | null) {
  if (typeof window === "undefined") return;
  if (role === "Admin") window.location.href = "/admin";
  else window.location.href = "/dashboard";
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

export default function Login() {
  const { t } = useTranslation();

  const [email, setEmail] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [message, setMessage] = React.useState("");


  const handleFacebookSuccess = (data: any) => {
    if (data?.email) localStorage.setItem("email", data.email);
    if (data?.token) localStorage.setItem("token", data.token);

    const roleFromBody = extractRoleLoose(data);
    if (roleFromBody) {
      setMessage(t("login.success") || "Zalogowano pomyślnie!");
      redirectByRole(roleFromBody);
      return;
    }

    fetchMe().then((me) => {
        if (me) {
             const meRole = extractRoleLoose(me);
             redirectByRole(meRole);
        } else {
             redirectByRole("User");
        }
    });
  };

  const handleLogin = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setMessage(t("login.logging_in"));

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
          t("login.invalid_credentials");
        setMessage(serverMsg);
        return;
      }

      if (data?.email) {
        try {
          localStorage.setItem("email", data.email);
        } catch {

        }
      }

      const roleFromBody = extractRoleLoose(data);
      if (roleFromBody) {
        setMessage(t("login.success"));
        redirectByRole(roleFromBody);
        return;
      }

      const me = await fetchMe();
      if (me) {
        const meRole = extractRoleLoose(me);
        const meEmail = me.email ?? me.userName ?? null;

        if (meEmail) {
          try {
            localStorage.setItem("email", String(meEmail));
          } catch {

          }
        }

        setMessage(t("login.success"));
        redirectByRole(meRole);
        return;
      }

      setMessage(t("login.success"));
      redirectByRole(null);
    } catch (error) {
      console.error(error);
      setMessage(t("login.connection_error"));
    }
  };

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

          <h2 className="text-2xl font-bold text-center mb-2">{t("login.title")}</h2>

          <input
            type="email"
            placeholder={t("login.email_placeholder")}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            className="p-3 rounded-md bg-base-100 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
          />

          <input
            type="password"
            placeholder={t("login.password_placeholder")}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            className="p-3 rounded-md bg-base-100 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
          />

          <button type="submit" className="btn btn-info">
            {t("login.submit")}
          </button>
          

            {message && (
            <p className="text-center text-sm text-base mt-2">{message}</p>
          )}


          {/* --- FACEBOOK BUTTON --- */}
          <div className="divider my-1 text-sm opacity-70">LUB</div>
          
          <FacebookButton 
            onLoginSuccess={handleFacebookSuccess}
            onLoginFailure={(msg) => setMessage(msg)}
          />
            <GoogleLoginButto
                onLoginSuccess={handleFacebookSuccess}
                onLoginFailure={(msg) => setMessage(msg)}
                />
        

          <div className="mt-4 flex flex-col gap-2 text-center text-sm">
            <a
              href="/forgot-password"
              className="link link-hover opacity-70 hover:opacity-100 transition-opacity"
            >
              Zapomniałeś hasła? Kliknij tutaj
            </a>
            
            <div className="mt-1">
              Nie masz konta?{" "}
              <a href="/register" className="link link-primary font-bold">
                Zarejestruj się!
              </a>
            </div>
          </div>

        </form>
      </div>

      <Footer />
    </div>
  );
}