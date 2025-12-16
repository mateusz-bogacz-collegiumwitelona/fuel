import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";
import { API_BASE } from "../components/api";

export default function ConfirmEmail() {
  const [status, setStatus] = React.useState<"loading" | "success" | "error">("loading");
  const [message, setMessage] = React.useState("Weryfikacja adresu e-mail...");
  
  const effectRan = React.useRef(false);

  React.useEffect(() => {
    if (effectRan.current === true) return;

    const params = new URLSearchParams(window.location.search);
    const emailParam = params.get("email");
    const tokenParam = params.get("token");

    if (!emailParam || !tokenParam) {
      setStatus("error");
      setMessage("Nieprawidłowy link potwierdzający (brak tokena lub e-maila).");
      return;
    }

    effectRan.current = true;

    confirmEmail(emailParam, tokenParam);
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
          "Nie udało się potwierdzić adresu e-mail.";

        console.log("Błąd weryfikacji:", serverMsg); 

        if (serverMsg.toLowerCase().includes("already confirmed")) {
            setStatus("success");
            setMessage("Twój e-mail został już wcześniej potwierdzony. Możesz się zalogować.");
            return;
        }

        setStatus("error");
        setMessage(serverMsg);
        return;
      }

      setStatus("success");
      setMessage("Potwierdzono email. Możesz się teraz zalogować.");

    } catch (error) {
      console.error(error);
      setStatus("error");
      setMessage("Błąd połączenia z serwerem.");
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
              <h2 className="text-xl font-bold">Weryfikacja...</h2>
              <p className="text-sm text-gray-400 mt-2">Proszę czekać, sprawdzamy Twój link.</p>
            </div>
          )}

          {status === "error" && (
            <div className="flex flex-col items-center">
              <h2 className="text-xl font-bold text-error mb-2">Błąd weryfikacji</h2>
              <p className="text-sm text-gray-300 mb-4">{message}</p>
              <a href="/login" className="btn btn-outline btn-sm">
                Wróć do logowania
              </a>
            </div>
          )}

          {status === "success" && (
            <div className="flex flex-col items-center">
              <h2 className="text-2xl font-bold text-success mb-4">Sukces!</h2>
              <p className="text-lg mb-6">{message}</p>
              <a href="/login" className="btn btn-primary w-full">
                Przejdź do logowania
              </a>
            </div>
          )}
          
        </div>
      </div>

      <Footer />
    </div>
  );
}