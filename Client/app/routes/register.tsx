import * as React from "react";
import HeaderHome from "../components/HeaderHome";
import FooterHome from "../components/FooterHome";
import { API_BASE } from "../components/api";
import FacebookButton from "../components/FacebookLoginButton";
import { useTranslation } from "react-i18next";

export default function Register() {
  const { t } = useTranslation();
  React.useEffect(() => {
    document.title = t("register.title") + " - FuelStats";
  }, [t]);
  
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
      setMessage(t("register.error_username_required"));
      return;
    }

    if (password !== confirmPassword) {
      setMessage(t("register.error_password_mismatch"));
      return;
    }

    if (password.length < 6) {
      setMessage(t("register.error_password_short"));
      return;
    }

    setLoading(true);
    setMessage(t("register.registering"));

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
          t("register.error_creation_failed");
        
        setMessage(serverMsg);
        return;
      }

      setMessage(
        data?.message ?? t("register.success_message")
      );

      setUsername("");
      setEmail("");
      setPassword("");
      setConfirmPassword("");
    } catch (error) {
      console.error(error);
      setMessage(t("register.connection_error"));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
      <HeaderHome/>

      <div className="flex-grow flex justify-center items-center">
        <form
          onSubmit={handleRegister}
          className="bg-base-300 p-8 rounded-2xl shadow-lg flex flex-col gap-4 w-full max-w-sm"
        >
          <h2 className="text-2xl font-bold text-center mb-2">
            {t("register.title")}
          </h2>

          <input
            type="text"
            placeholder={t("register.username_placeholder")}
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            required
            className="p-3 rounded-md bg-base-100 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
          />

          <input
            type="email"
            placeholder={t("register.email_placeholder")}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            className="p-3 rounded-md bg-base-100 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
          />

          <input
            type="password"
            placeholder={t("register.password_placeholder")}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            className="p-3 rounded-md bg-base-100 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
          />

          <input
            type="password"
            placeholder={t("register.confirm_password_placeholder")}
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            required
            className="p-3 rounded-md bg-base-100 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
          />

          <div className="text-xs text-base-content/70 leading-snug">
            {t("register.password_rules")}
          </div>

          <button type="submit" className="btn btn-info" disabled={loading}>
            {loading ? t("register.registering") : t("register.submit")}
          </button>
          


          {message && (
            <p className="text-center text-sm text-base-content/80 mt-2">
              {message}
            </p>
          )}

          <div className="text-center text-sm mt-2">
            {t("register.have_account")}{" "}
            <a href="/login" className="link link-primary font-bold">
              {t("register.login_link")}
            </a>
          </div>
        </form>
      </div>

      <FooterHome/>
    </div>
  );
}