import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";
import { useAdminGuard } from "../components/useAdminGuard";
import { useTranslation } from "react-i18next";

export default function AdminDashboard() {
  const { t } = useTranslation();
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
            <h1 className="text-3xl font-bold mb-1">{t("admin.title")}</h1>
            {email && (
              <p className="text-sm text-gray-400">
                {t("admin.logged_in_as", { email })}
              </p>
            )}
          </div>
        </div>

        <div className="bg-base-300 rounded-xl p-6 shadow-md mb-8">
          <p className="mb-4 text-lg">{t("admin.welcome")}</p>
          <div className="grid gap-4 sm:grid-cols-2 md:grid-cols-4">
            <a href="/admin/brands" className="btn btn-primary w-full">
              {t("admin.brand_panel")}
            </a>
            <a href="/admin/users" className="btn btn-primary w-full">
              {t("admin.user_panel")}
            </a>
            <a href="/admin/stations" className="btn btn-primary w-full">
              {t("admin.gas_station_panel")}
            </a>
            <a href="/admin/proposals" className="btn btn-primary w-full">
              {t("admin.proposal_panel")}
            </a>
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
}
