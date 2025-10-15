import * as React from "react";

export default function Dashboard() {
  const [email, setEmail] = React.useState<string | null>(null);

  React.useEffect(() => {
    const token = localStorage.getItem("token");
    if (!token) {
      if (typeof window !== "undefined") {
        window.location.href = "/login";
      }
      return;
    }

    setEmail("Zalogowany użytkownik"); 
  }, []);

  const handleLogout = () => {
    localStorage.removeItem("token");
    localStorage.removeItem("token_expiration");
    if (typeof window !== "undefined") {
      window.location.href = "/login";
    }
  };

  return (
    <div className="flex flex-col items-center justify-center min-h-screen bg-gray-900 text-white">
      <div className="bg-gray-800 rounded-2xl shadow-lg p-10 w-full max-w-md text-center">
        <h1 className="text-3xl font-bold mb-4">Admin Dashboard</h1>
        <p className="text-gray-300 mb-6">
          {email ? `Witaj, ${email}!` : "Wczytywanie..."}
        </p>
        <button
          onClick={handleLogout}
          className="bg-red-600 hover:bg-red-500 transition-colors font-semibold py-2 px-6 rounded-md"
        >
          Wyloguj
        </button>
      </div>
    </div>
  );
}
