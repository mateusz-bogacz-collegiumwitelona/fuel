import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";
import { API_BASE } from "../components/api";
import { useTranslation } from "react-i18next";

export default function ResetPassword() {
  const { t } = useTranslation();
  const [email, setEmail] = React.useState("");
  const [token, setToken] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [confirmPassword, setConfirmPassword] = React.useState("");
  const [message, setMessage] = React.useState("");
  const [loading, setLoading] = React.useState(false);
  const [isSuccess, setIsSuccess] = React.useState(false);

  React.useEffect(() => {
    if (typeof window === "undefined") return;

    const params = new URLSearchParams(window.location.search);
    const emailParam = params.get("email");
    const tokenParam = params.get("token");

    if (emailParam) setEmail(emailParam);
    if (tokenParam) setToken(tokenParam);
    
    if (!tokenParam) {
       setMessage(t("reset-password.error_missing_token_link"));
    }
  }, [t]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    if (!token.trim()) {
      setMessage(t("reset-password.error_missing_token_submit"));
      return;
    }

    if (password !== confirmPassword) {
      setMessage(t("reset-password.error_password_mismatch"));
      return;
    }

    if (password.length < 6) {
      setMessage(t("reset-password.error_password_short"));
      return;
    }

    setLoading(true);
    setMessage(t("reset-password.saving"));

    try {
      const response = await fetch(`${API_BASE}/api/set-new-password`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        credentials: "include",
        body: JSON.stringify({
          email,
          password,
          confirmPassword,
          token,
        }),
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
          t("reset-password.error_setting_failed");
        setMessage(serverMsg);
        return;
      }

      setIsSuccess(true);
      setMessage(data?.message ?? t("reset-password.success_message"));
      setPassword("");
      setConfirmPassword("");
    } catch (error) {
      console.error(error);
      setMessage(t("reset-password.connection_error"));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
      <Header />

      <div className="flex-grow flex justify-center items-center">
        <form
          onSubmit={handleSubmit}
          className="bg-base-300 p-8 rounded-2xl shadow-lg flex flex-col gap-4 w-full max-w-sm"
        >
          <h2 className="text-2xl font-bold text-center mb-2">
            {t("reset-password.title")}
          </h2>

          {!isSuccess && (
            <p className="text-xs text-base-content/70 leading-snug mb-2">
              {t("reset-password.description")}
            </p>
          )}

          <div className="form-control">
             <label className="label py-0"><span className="label-text-alt">{t("reset-password.account_label")}</span></label>
             <input
                type="email"
                value={email}
                readOnly
                className="p-3 rounded-md bg-base-200 border border-gray-600 text-base-content/50 cursor-not-allowed outline-none"
             />
          </div>


          {!isSuccess && (
            <>
              <input
                type="password"
                placeholder={t("reset-password.new_password_placeholder")}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                className="p-3 rounded-md bg-base-100 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
              />

              <input
                type="password"
                placeholder={t("reset-password.confirm_password_placeholder")}
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                required
                className="p-3 rounded-md bg-base-100 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
              />

              <div className="text-xs text-base-content/70 leading-snug">
                {t("reset-password.password_rules")}
              </div>

              <button type="submit" className="btn btn-info" disabled={loading || !token}>
                {loading ? t("reset-password.saving") : t("reset-password.submit")}
              </button>
            </>
          )}

          {message && (
            <div className={`text-center text-sm mt-2 p-2 rounded ${isSuccess ? 'bg-success/10 text-success' : 'text-error'}`}>
              {message}
            </div>
          )}

          <div className="text-center text-sm mt-2">
            {isSuccess ? (
                <a href="/login" className="btn btn-primary w-full mt-2">{t("reset-password.success_button")}</a>
            ) : (
                <>
                {t("reset-password.remember_password")}{" "}
                <a href="/login" className="link link-primary">
                  {t("reset-password.back_to_login")}
                </a>
                </>
            )}
          </div>
        </form>
      </div>

      <Footer />
    </div>
  );
}