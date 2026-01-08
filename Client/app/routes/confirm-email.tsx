import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";
import { API_BASE } from "../components/api";
import { useTranslation } from "react-i18next";

export default function ConfirmEmail() {
  const { t } = useTranslation();
  React.useEffect(() => {
    document.title = t("confirm-email.title") + " - FuelStats";
  }, [t]);
  const [status, setStatus] = React.useState<"loading" | "success" | "error">("loading");
  const [message, setMessage] = React.useState("");
  
  const effectRan = React.useRef(false);

  React.useEffect(() => {
    if (status === "loading") {
      setMessage(t("confirm-email.verifying_initial"));
    }
  }, [t, status]);

  React.useEffect(() => {
    if (effectRan.current === true) return;

    const params = new URLSearchParams(window.location.search);
    const emailParam = params.get("email");
    const tokenParam = params.get("token");

    if (!emailParam || !tokenParam) {
      setStatus("error");
      setMessage(t("confirm-email.error_invalid_link"));
      return;
    }

    effectRan.current = true;

    confirmEmail(emailParam, tokenParam);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const confirmEmail = async (emailValue: string, tokenValue: string) => {
    try {
      const response = await fetch(`${API_BASE}/api/confirm-email`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        credentials: "include",
        body: JSON.stringify({
          email: emailValue,
          token: tokenValue,
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
          t("confirm-email.error_default");

        console.log("Błąd weryfikacji:", serverMsg); 

        if (serverMsg.toLowerCase().includes("already confirmed")) {
            setStatus("success");
            setMessage(t("confirm-email.error_already_confirmed"));
            return;
        }

        setStatus("error");
        setMessage(serverMsg === "Nie udało się potwierdzić adresu e-mail." 
          ? t("confirm-email.error_default") 
          : serverMsg);
        return;
      }

      setStatus("success");
      setMessage(data?.message ?? t("confirm-email.success_message"));

    } catch (error) {
      console.error(error);
      setStatus("error");
      setMessage(t("confirm-email.connection_error"));
    }
  };

  return (
    <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
      <Header />

      <div className="flex-grow flex justify-center items-center">
        <div className="bg-base-300 p-8 rounded-2xl shadow-lg flex flex-col gap-4 w-full max-w-sm text-center">
          
          {status === "loading" && (
            <div className="flex flex-col items-center">
              <span className="loading loading-spinner loading-lg mb-4 text-info"></span>
              <h2 className="text-xl font-bold">{t("confirm-email.loading_title")}</h2>
              <p className="text-sm text-base-content/70 mt-2">{t("confirm-email.loading_desc")}</p>
            </div>
          )}

          {status === "error" && (
            <div className="flex flex-col items-center">
              <h2 className="text-xl font-bold text-error mb-2">{t("confirm-email.error_title")}</h2>
              <p className="text-sm text-base-content/70 mb-4">{message}</p>
              <a href="/login" className="btn btn-outline btn-sm">
                {t("confirm-email.back_to_login")}
              </a>
            </div>
          )}

          {status === "success" && (
            <div className="flex flex-col items-center">
              <h2 className="text-2xl font-bold text-success mb-4">{t("confirm-email.success_title")}</h2>
              <p className="text-lg mb-6">{message}</p>
              <a href="/login" className="btn btn-primary w-full">
                {t("confirm-email.go_to_login")}
              </a>
            </div>
          )}
          
        </div>
      </div>

      <Footer />
    </div>
  );
}