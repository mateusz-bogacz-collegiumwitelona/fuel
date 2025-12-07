import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";
import { useAdminGuard } from "../components/useAdminGuard";

export default function AdminDashboard() {
  const { state, email } = useAdminGuard();

  const handleLogout = () => {
    try {
      localStorage.removeItem("email");
    } catch {}
    window.location.href = "/login";
  };

  if (state === "checking") {
    return (
      <div className="min-h-screen bg-base-200 flex items-center justify-center">
        <span className="loading loading-spinner loading-lg" />
      </div>
    );
  }

  if (state !== "allowed") {
    return null;
  }
  return (
    <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
      <Header />

      <main className="flex-1 mx-auto w-full max-w-6xl px-4 py-10">
        <div className="flex justify-between items-center mb-4">
          <div>
            <h1 className="text-3xl font-bold mb-1">Panel administratora</h1>
            {email && (
              <p className="text-sm text-gray-400">Zalogowany jako: {email}</p>
            )}
          </div>
          <button className="btn btn-outline btn-sm" onClick={handleLogout}>
            Wyloguj
          </button>
        </div>

        <div className="bg-base-300 rounded-xl p-6 shadow-md mb-8">
          <p className="mb-4 text-lg">
            Witaj w panelu administratora. Wybierz swoje działanie.
          </p>
          <div className="grid gap-4 sm:grid-cols-2 md:grid-cols-4">
            <a href="/brand_admin" className="btn btn-primary w-full">
              Brand panel
            </a>
            <a href="/user_admin" className="btn btn-primary w-full">
              User panel
            </a>
            <a href="/gas_station_admin" className="btn btn-primary w-full">
              Gas station panel
            </a>
            <a href="/proposals_admin" className="btn btn-primary w-full">
              Proposal panel
            </a>
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
}
