import * as React from "react";
import { useTheme } from "../context/ThemeContext";
import ThemeController from "../components/ThemeController";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { API_BASE } from "../components/api";
import FacebookButton from "../components/FacebookLoginButton";

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

export default function Register() {
  const [username, setUsername] = React.useState("");
  const [email, setEmail] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [confirmPassword, setConfirmPassword] = React.useState("");
  const [message, setMessage] = React.useState("");
  const [loading, setLoading] = React.useState(false);


  const handleFacebookSuccess = (data: any) => {

    if (data?.email) localStorage.setItem("email", data.email);
    if (data?.token) localStorage.setItem("token", data.token);


    if (typeof window !== "undefined") {
       window.location.href = "/dashboard";
    }
  };

  const handleRegister = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    if (!username.trim()) {
      setMessage("Podaj nazwę użytkownika.");
      return;
    }

    if (password !== confirmPassword) {
      setMessage("Hasła nie są takie same.");
      return;
    }

    if (password.length < 6) {
      setMessage("Hasło musi mieć co najmniej 6 znaków.");
      return;
    }

    setLoading(true);
    setMessage("Rejestrowanie...");

    try {
      const response = await fetch(`${API_BASE}/api/register`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        body: JSON.stringify({
          userName: username,
          email,
          password,
          confirmPassword,
        }),
        credentials: "include",
      });

      let data: any = null;
      try {
        data = await response.json();
      } catch {
        data = null;
      }

      if (!response.ok || data?.success === false) {
        const serverMsg =
          (data && (data.message || data.error)) ??
          (data &&
            Array.isArray(data.errors) &&
            data.errors.join(", ")) ??
          "Nie udało się utworzyć konta.";
        setMessage("test");
        return;
      }

      setMessage(
        data?.message ??
          "Konto zostało utworzone. Sprawdź e-mail, aby potwierdzić adres.",
      );

      setUsername("");
      setEmail("");
      setPassword("");
      setConfirmPassword("");
    } catch (error) {
      console.error(error);
      setMessage("Błąd połączenia z serwerem.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
      <Header />

      <div className="flex-grow flex justify-center items-center">
        <form
          onSubmit={handleRegister}
          className="bg-base-300 p-8 rounded-2xl shadow-lg flex flex-col gap-4 w-full max-w-sm"
        >
          <h2 className="text-2xl font-bold text-center mb-2">
            Rejestracja
          </h2>

          <input
            type="text"
            placeholder="Nazwa użytkownika"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            required
            className="p-3 rounded-md bg-base-100 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
          />

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

          <input
            type="password"
            placeholder="Powtórz hasło"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            required
            className="p-3 rounded-md bg-base-100 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
          />

          <div className="text-xs text-base-400 leading-snug">
            Hasło musi mieć co najmniej 6 znaków, zawierać co najmniej:
            jedną wielką literę, jedną cyfrę oraz jeden znak specjalny.
          </div>

          <button type="submit" className="btn btn-info" disabled={loading}>
            {loading ? "Rejestrowanie..." : "Zarejestruj się"}
          </button>
          
          {/* --- FACEBOOK BUTTON --- */}
          <div className="divider my-2 text-sm text-base-500">LUB</div>
          
          <FacebookButton 
            buttonText="Zarejestruj się przez Facebook"
            onLoginSuccess={handleFacebookSuccess}
            onLoginFailure={(msg) => setMessage(msg)}
          />

          {message && (
            <p className="text-center text-sm text-base-400 mt-2">
              {message}
            </p>
          )}

          <div className="text-center text-sm mt-2">
            Masz już konto?{" "}
            <a href="/login" className="link link-primary">
              Zaloguj się
            </a>
          </div>
        </form>
      </div>

      <Footer />
    </div>
  );
}
