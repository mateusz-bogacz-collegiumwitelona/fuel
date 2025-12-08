import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";
import { API_BASE } from "../components/api";

export default function ResetPassword() {
  const [email, setEmail] = React.useState("");
  const [token, setToken] = React.useState(""); // Token jest w stanie, ale nie w widoku
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
       setMessage("Błąd: Brak tokena resetującego w linku.");
    }
  }, []);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    if (!token.trim()) {
      setMessage("Brak tokena weryfikacyjnego (użyj linku z e-maila).");
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
    setMessage("Ustawianie nowego hasła...");

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
          "Nie udało się ustawić nowego hasła.";
        setMessage(serverMsg);
        return;
      }

      setIsSuccess(true);
      setMessage(data?.message ?? "Hasło zostało zmienione. Możesz się teraz zalogować.");
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
          onSubmit={handleSubmit}
          className="bg-base-300 p-8 rounded-2xl shadow-lg flex flex-col gap-4 w-full max-w-sm"
        >
          <h2 className="text-2xl font-bold text-center mb-2">
            Ustaw nowe hasło
          </h2>

          {!isSuccess && (
            <p className="text-xs text-gray-400 leading-snug mb-2">
              Wpisz swoje nowe hasło poniżej.
            </p>
          )}

          <div className="form-control">
             <label className="label py-0"><span className="label-text-alt">Konto</span></label>
             <input
                type="email"
                value={email}
                readOnly
                className="p-3 rounded-md bg-base-200 border border-gray-600 text-gray-500 cursor-not-allowed outline-none"
             />
          </div>


          {!isSuccess && (
            <>
              <input
                type="password"
                placeholder="Nowe hasło"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                className="p-3 rounded-md bg-base-100 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
              />

              <input
                type="password"
                placeholder="Powtórz nowe hasło"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                required
                className="p-3 rounded-md bg-base-100 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
              />

              <div className="text-xs text-gray-400 leading-snug">
                Hasło musi mieć co najmniej 6 znaków, zawierać co najmniej jedną
                wielką literę, jedną cyfrę oraz jeden znak specjalny.
              </div>

              <button type="submit" className="btn btn-info" disabled={loading || !token}>
                {loading ? "Zapisywanie..." : "Zapisz nowe hasło"}
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
                <a href="/login" className="btn btn-primary w-full mt-2">Przejdź do logowania</a>
            ) : (
                <>
                Pamiętasz hasło?{" "}
                <a href="/login" className="link link-primary">
                  Wróć do logowania
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