import * as React from "react";
import Header from "../Components/header";
import Footer from "../Components/footer";

function parseJwt(token: string) {
  try {
    return JSON.parse(atob(token.split(".")[1]));
  } catch (e) {
    console.error("Nie można zdekodować tokena JWT:", e);
    return null;
  }
}

export default function Login() {
  const [email, setEmail] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [message, setMessage] = React.useState("");

  const handleLogin = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setMessage("Logowanie...");

    try {
      const response = await fetch("http://localhost:5111/api/login", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        body: JSON.stringify({ email, password }),
      });

      if (!response.ok) {
        setMessage("Błędny email lub hasło");
        return;
      }

      const data = await response.json();

if (data.token) {
  localStorage.setItem("token", data.token);
  localStorage.setItem("token_expiration", data.expiration);
  localStorage.setItem("email",data.email);
  // // Zapis do cookies (ważne np. 7 dni)
  // const expires = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toUTCString();
  // document.cookie = `token=${data.token}; expires=${expires}; path=/`;
  // document.cookie = `email=${email}; expires=${expires}; path=/`;
  // document.cookie = `token_expiration=${data.expiration}; expires=${expires}; path=/`;

  const decoded = parseJwt(data.token);
  const role =
    decoded?.["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];

        setMessage("Zalogowano pomyślnie!");

        setTimeout(() => {
          if (typeof window !== "undefined") {
            if (role === "Admin") {
              window.location.href = "/admin-dashboard";
            } else {
              window.location.href = "/dashboard";
            }
          }
        }, 1000);
      } else {
        setMessage("Nie otrzymano tokena od serwera");
      }
    } catch (error) {
      console.error(error);
      setMessage("Błąd połączenia z serwerem");
    }
  };

  React.useEffect(() => {
    const token = localStorage.getItem("token");
    const expiration = localStorage.getItem("token_expiration");

    if (token && expiration && new Date(expiration) > new Date()) {
      const decoded = parseJwt(token);
      const role =
        decoded?.[
          "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        ];

      if (typeof window !== "undefined") {
        if (role === "Admin") {
          window.location.href = "/admin-dashboard";
        } else {
          window.location.href = "/dashboard";
        }
      }
    }
  }, []);

  return (
  <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
    <Header />

    <div className="flex-grow flex justify-center items-center">
      <form
        onSubmit={handleLogin}
        className="bg-base-300 p-8 rounded-2xl shadow-lg flex flex-col gap-4 w-full max-w-sm"
      >
        <h2 className="text-2xl font-bold text-center mb-2">Logowanie</h2>

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
        
        <button
          type="submit"
          className="btn btn-info"
        >
          Zaloguj
        </button>

        {message && (
          <p className="text-center text-sm text-gray-300 mt-2">{message}</p>
        )}
      </form>
    </div>

    <Footer />
  </div>
);
}
