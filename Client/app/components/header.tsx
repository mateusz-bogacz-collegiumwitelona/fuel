import React, { useEffect, useState } from "react";
import ThemeController from "../components/ThemeController";
import { useTheme } from "../context/ThemeContext";
import { useTranslation } from "react-i18next";
import { API_BASE } from "../components/api";

function normalizeRole(raw: unknown): string | null {
  if (!raw) return null;
  if (Array.isArray(raw)) {
    for (const item of raw) {
      const r = normalizeRole(item);
      if (r) return r;
    }
    return null;
  }
  let role = String(raw).trim();
  if (!role) return null;
  if (role.startsWith("ROLE_")) role = role.slice(5);
  role = role.toLowerCase();
  if (["admin", "administrator"].includes(role)) return "Admin";
  if (["user", "użytkownik", "viewer"].includes(role)) return "User";
  return role.charAt(0).toUpperCase() + role.slice(1);
}

function extractRoleLoose(obj: any): string | null {
  if (!obj || typeof obj !== "object") return null;
  const candidates = [
    "roles",
    "role",
    "authorities",
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role",
  ];
  for (const key of candidates) {
    if (key in obj) {
      const maybe = normalizeRole(obj[key]);
      if (maybe) return maybe;
    }
  }
  return null;
}

export default function Header() {
  const { theme, setTheme } = useTheme();
  const { i18n, t } = useTranslation();
  const [role, setRole] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const res = await fetch(`${API_BASE}/api/me`, {
          method: "GET",
          credentials: "include",
          headers: { Accept: "application/json" },
        });
        if (!mounted) return;
        if (!res.ok) {
          const stored = localStorage.getItem("role");
          setRole(stored ?? null);
          return;
        }
        const me = await res.json();
        const meRole = extractRoleLoose(me);
        setRole(meRole);
        try {
          if (meRole) localStorage.setItem("role", meRole);
          else localStorage.removeItem("role");
        } catch {}
      } catch (err) {
        const stored = localStorage.getItem("role");
        setRole(stored ?? null);
      }
    })();
    return () => {
      mounted = false;
    };
  }, []);

  const handleLogout = async () => {
    try {
      await fetch(`${API_BASE}/api/logout`, {
        method: "POST",
        credentials: "include",
      });
    } catch (e) {
      console.warn("Logout request failed:", e);
    }
    localStorage.removeItem("token");
    localStorage.removeItem("token_expiration");
    localStorage.removeItem("role");
    localStorage.removeItem("email");
    if (typeof window !== "undefined") window.location.href = "/login";
  };

  const changeLanguage = (lng: string) => {
    i18n.changeLanguage(lng);
    localStorage.setItem("i18nextLng", lng);
    if (typeof document !== "undefined") document.documentElement.lang = lng;
  };

  return (
    <header className="w-full bg-base-300 shadow-sm text-base-content">
      <div className="mx-auto max-w-6xl px-4 py-3 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <a href="/dashboard" className="flex items-center gap-2">
            <img src="/images/fuelstats.png" alt="FuelStats" className="h-8 w-8 rounded-sm" />
            <span className="text-xl font-bold">FuelStats</span>
          </a>
        </div>

        <div className="flex items-center gap-3">
          <ThemeController theme={theme} setTheme={setTheme} />

          <div className="dropdown">
            <div tabIndex={0} role="button" className="btn m-1">
              {t("buttons.language")}
            </div>
            <ul tabIndex={0} className="dropdown-content menu bg-base-100 rounded-box z-[1] w-52 p-2 shadow-sm">
              <li>
                <a role="button" onClick={() => changeLanguage("pl")} className={i18n.language === "pl" ? "font-bold" : ""}>
                  {t("buttons.polish")}
                </a>
              </li>
              <li>
                <a role="button" onClick={() => changeLanguage("en")} className={i18n.language === "en" ? "font-bold" : ""}>
                  {t("buttons.english")}
                </a>
              </li>
            </ul>
          </div>

          <div className="dropdown dropdown-end">
            <label tabIndex={0} className="btn btn-ghost btn-square" aria-haspopup="true">
              <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 6h16M4 12h16M4 18h16" />
              </svg>
            </label>

            <ul tabIndex={0} className="menu menu-compact dropdown-content mt-3 p-2 shadow bg-base-100 rounded-box w-52">
              <li><a href="/settings">{t("header.links.settings")}</a></li>
              <li><a href="/dashboard">{t("header.links.dashboard")}</a></li>
              <li><a href="/map">{t("header.links.map")}</a></li>
              <li><a href="/list">{t("header.links.list")}</a></li>
              <li><a href="/proposals">{t("header.links.addproposal")}</a></li>

              {role === "Admin" && (
                <>
                  <li className="menu-title"><span>{t("header.links.foradmin")}</span></li>
                  <li><a href="/admin-dashboard">{t("header.links.admindashboard")}</a></li>
                  <li><a href="/brand_admin">{t("header.links.brands")}</a></li>
                  <li><a href="/user_admin">{t("header.links.users")}</a></li>
                  <li><a href="/gas_station_admin">{t("header.links.stations")}</a></li>
                  <li><a href="/proposal-admin">{t("header.links.proposaladmin")}</a></li>
                </>
              )}

              <li><button onClick={handleLogout} className="w-full text-left">{t("header.links.logout")}</button></li>
            </ul>
          </div>
        </div>
      </div>
    </header>
  );
}
