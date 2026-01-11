import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";
import { API_BASE } from "../components/api";
import { useTranslation } from "react-i18next";

export default function ForgotPassword() {
  const { t } = useTranslation();
  React.useEffect(() => {
    document.title = t("forgot-password.title") + " - FuelStats";
  }, [t]);
  const [email, setEmail] = React.useState("");
  const [message, setMessage] = React.useState("");
  const [loading, setLoading] = React.useState(false);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    if (!email.trim()) {
      setMessage(t("forgot-password.error_email_required"));
      return;
    }

    setLoading(true);
    setMessage(t("forgot-password.sending"));

    try {
      const response = await fetch(
        `${API_BASE}/api/reset-password?email=${encodeURIComponent(email)}`,
        {
          method: "POST",
          headers: {
            Accept: "application/json",
          },
          credentials: "include",
        },
      );

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
          t("forgot-password.error_sending_failed");
        setMessage(serverMsg);
        return;
      }

      setMessage(
        data?.message ??
          t("forgot-password.success_message"),
      );
    } catch (error) {
      console.error(error);
      setMessage(t("forgot-password.connection_error"));
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
            {t("forgot-password.title")}
          </h2>

          <p className="text-xs text-base-content/70 leading-snug mb-2">
            {t("forgot-password.description")}
          </p>

          <input
            type="email"
            placeholder={t("forgot-password.email_placeholder")}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            className="p-3 rounded-md bg-base-100 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
          />

          <button type="submit" className="btn btn-info" disabled={loading}>
            {loading ? t("forgot-password.sending") : t("forgot-password.submit")}
          </button>

          {message && (
            <p className="text-center text-sm text-base-content/80 mt-2">
              {message}
            </p>
          )}

          <div className="text-center text-sm mt-2">
            {t("forgot-password.remember_password")}{" "}
            <a href="/login" className="link link-primary">
              {t("forgot-password.back_to_login")}
            </a>
          </div>
        </form>
      </div>

      <Footer />
    </div>
  );
}