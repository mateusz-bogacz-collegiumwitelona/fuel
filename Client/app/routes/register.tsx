import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";
import { API_BASE } from "../components/api";
import FacebookButton from "../components/FacebookLoginButton";

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
