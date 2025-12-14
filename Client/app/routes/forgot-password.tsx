import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";
import { API_BASE } from "../components/api";

export default function ForgotPassword() {
  const [email, setEmail] = React.useState("");
  const [message, setMessage] = React.useState("");
  const [loading, setLoading] = React.useState(false);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    if (!email.trim()) {
      setMessage("Podaj adres e-mail.");
      return;
    }

    setLoading(true);
    setMessage("Wysyłanie instrukcji resetu hasła...");

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
          "Nie udało się wysłać maila z resetem hasła.";
        setMessage(serverMsg);
        return;
      }

      setMessage(
        data?.message ??
          "Jeśli konto istnieje i e-mail jest potwierdzony, wysłaliśmy wiadomość z instrukcją resetu hasła.",
      );
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
          onSubmit={handleSubmit}
          className="bg-base-300 p-8 rounded-2xl shadow-lg flex flex-col gap-4 w-full max-w-sm"
        >
          <h2 className="text-2xl font-bold text-center mb-2">
            Przypomnienie hasła
          </h2>

          <p className="text-xs text-gray-400 leading-snug mb-2">
            Wpisz adres e-mail użyty przy rejestracji. Jeśli konto istnieje i
            e-mail jest potwierdzony, wyślemy wiadomość z instrukcją zmiany
            hasła.
          </p>

          <input
            type="email"
            placeholder="E-mail"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            className="p-3 rounded-md bg-base-100 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
          />

          <button type="submit" className="btn btn-info" disabled={loading}>
            {loading ? "Wysyłanie..." : "Wyślij link resetujący"}
          </button>

          {message && (
            <p className="text-center text-sm text-gray-300 mt-2">
              {message}
            </p>
          )}

          <div className="text-center text-sm mt-2">
            Pamiętasz hasło?{" "}
            <a href="/login" className="link link-primary">
              Wróć do logowania
            </a>
          </div>
        </form>
      </div>

      <Footer />
    </div>
  );
}
