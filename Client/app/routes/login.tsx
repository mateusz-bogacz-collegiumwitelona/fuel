import * as React from "react";
import { useTranslation } from "react-i18next";
import FacebookButton from "../components/FacebookLoginButton";
import { API_BASE } from "../components/api";
import { useTheme } from "../context/ThemeContext";
import ThemeController from "../components/ThemeController";
import { Link } from "react-router";

function Header() {
  const { i18n, t } = useTranslation();
  const { theme, setTheme } = useTheme();

  const changeLanguage = (lng: string) => {
    i18n.changeLanguage(lng);
    try {
      localStorage.setItem("i18nextLng", lng);
      if (typeof document !== "undefined") document.documentElement.lang = lng;
    } catch {}
  };

  return (
    <header className="w-full bg-base-300 text-base-content shadow-md">
      <div className="mx-auto max-w-6xl px-4 py-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <a href="/" className="flex items-center gap-3">
            <img src="/images/fuelstats.png" alt="FuelStats" className="h-10 w-10 rounded-sm shadow" />
            <span className="text-2xl font-extrabold tracking-wide">FuelStats</span>
          </a>
        </div>

        <div className="flex items-center gap-3">
          <div className="dropdown">
            <div tabIndex={0} role="button" className="btn m-1">
              {t("buttons.language")}
            </div>
            <ul tabIndex={0} className="dropdown-content menu bg-base-100 rounded-box z-[1] w-52 p-2 shadow-sm">
              <li>
                <a role="button" onClick={() => changeLanguage("pl")} className={i18n.language === "pl" ? "font-bold" : ""}>
                  {t("buttons.polish")}
                </a>
              </li>
              <li>
                <a role="button" onClick={() => changeLanguage("en")} className={i18n.language === "en" ? "font-bold" : ""}>
                  {t("buttons.english")}
                </a>
              </li>
            </ul>
          </div>

          <ThemeController theme={theme} setTheme={setTheme} />

          <div className="dropdown dropdown-end">
              <label tabIndex={0} className="btn btn-ghost btn-square" aria-haspopup="true">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 6h16M4 12h16M4 18h16" />
                </svg>
              </label>

            <ul tabIndex={0} className="menu menu-compact dropdown-content mt-3 p-2 shadow bg-base-100 rounded-box w-52">
              <Link to="/login" className="btn btn-sm btn-outline text-base-content">
                {t ? t("home.ctaLogin", { defaultValue: "Login" }) : "Login"}
              </Link>
              <Link to="/register" className="btn btn-sm btn-outline text-base-content">
                {t ? t("home.ctaRegister", { defaultValue: "Register" }) : "Register"}
              </Link>
            </ul>
          </div>
        </div>
      </div>
    </header>
  );
}

function Footer() {
  const { t } = useTranslation();
  return (
    <footer className="w-full bg-base-300 text-base-content py-8 mt-12">
      <div className="mx-auto max-w-6xl px-4 text-center">
        <p className="text-sm">{t ? t("home.footerContact", { defaultValue: "Kontakt: kontakt@fuelstats.example" }) : "Kontakt: kontakt@fuelstats.example"}</p>
        <div className="mt-3 text-xs text-gray-500">
          <a className="link link-hover mr-3">{t ? t("home.privacy", { defaultValue: "Polityka prywatności" }) : "Polityka prywatności"}</a>
          <a className="link link-hover">{t ? t("home.terms", { defaultValue: "Regulamin" }) : "Regulamin"}</a>
        </div>
        <div className="text-xs text-gray-400 mt-4">© {new Date().getFullYear()} FuelStats</div>
      </div>
    </footer>
  );
}

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
  if (role === "Admin") window.location.href = "/admin-dashboard";
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
          <div className="divider my-1 text-sm opacity-70">{t("login.or")}</div>
          
          <FacebookButton 
            onLoginSuccess={handleFacebookSuccess}
            onLoginFailure={(msg) => setMessage(msg)}
          />

        

          <div className="mt-4 flex flex-col gap-2 text-center text-sm">
            <a
              href="/forgot-password"
              className="link link-hover opacity-70 hover:opacity-100 transition-opacity"
            >
              {t("login.forgotpassword")}
            </a>
            
            <div className="mt-1">
              {t("login.noaccount")}{" "}
              <a href="/register" className="link link-primary font-bold">
                {t("login.register")}
              </a>
            </div>
          </div>

        </form>
      </div>

      <Footer />
    </div>
  );
}