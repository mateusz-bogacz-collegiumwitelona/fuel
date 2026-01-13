import React, { useEffect, useState } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { API_BASE } from "../components/api";

function normalizeRole(raw: unknown): string | null {
  if (!raw) return null;

  if (Array.isArray(raw)) {
    for (const r of raw) {
      const n = normalizeRole(r);
      if (n) return n;
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

  const fields = [
    "roles",
    "role",
    "authorities",
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role",
  ];

  for (const f of fields) {
    if (f in obj) {
      const maybe = normalizeRole(obj[f]);
      if (maybe) return maybe;
    }
  }

  return null;
}

export default function Footer() {
  const { t } = useTranslation();
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
          setRole(localStorage.getItem("role"));
          return;
        }

        const me = await res.json();
        const r = extractRoleLoose(me);
        setRole(r);

        if (r) localStorage.setItem("role", r);
      } catch {
        setRole(localStorage.getItem("role"));
      }
    })();

    return () => {
      mounted = false;
    };
  }, []);

  const creators = [
    "Mateusz Bogacz-Drewniak",
    "Paweł Kruk",
    "Szymon Mikołajek",
    "Michał Nocuń",
    "Mateusz Chimkowski",
  ];

  return (
    <footer className="w-full bg-base-300 text-base-content py-10">
      <div className="mx-auto max-w-6xl px-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">

          {/* Left: Creators */}
          <div className="flex flex-col gap-3">
            <h3 className="font-semibold text-lg">{t("footer.creatorsTitle")}</h3>
            <ul className="mt-2 space-y-1 text-sm">
              {creators.map((c) => (
                <li key={c} className="truncate" title={c}>
                  {c}
                </li>
              ))}
            </ul>
          </div>

          {/* Middle: Links */}
          <div className="flex flex-col items-center">
            <h3 className="font-semibold text-lg text-center">{t("footer.linksTitle")}</h3>

            {/* For all */}
            <ul className="mt-2 space-y-1 text-sm text-center">
              <li><Link to="/dashboard" className="link link-hover">{t("footer.links.dashboard")}</Link></li>
              <li><Link to="/settings" className="link link-hover">{t("footer.links.settings")}</Link></li>
              <li><Link to="/map" className="link link-hover">{t("footer.links.map")}</Link></li>
              <li><Link to="/list" className="link link-hover">{t("footer.links.list")}</Link></li>
            </ul>

            {/* For admin */}
            {role === "Admin" && (
              <>
                <div className="divider my-2" />

                <h4 className="text-sm font-semibold text-center opacity-80">
                  {t("footer.links.foradmin")}
                </h4>

                <ul className="mt-1 space-y-1 text-sm text-center">
                  <li><Link to="/admin-dashboard" className="link link-hover">{t("footer.links.admindashboard")}</Link></li>
                  <li><Link to="/brand_admin" className="link link-hover">{t("footer.links.brands")}</Link></li>
                  <li><Link to="/user_admin" className="link link-hover">{t("footer.links.users")}</Link></li>
                  <li><Link to="/gas_station_admin" className="link link-hover">{t("footer.links.stations")}</Link></li>
                </ul>
              </>
            )}
          </div>

          {/* Right: Contact */}
          <div className="flex flex-col items-end">
            <h3 className="font-semibold text-lg">{t("footer.rightTitle")}</h3>
            <p className="mt-2 text-sm text-right">{t("footer.rightText")}</p>
            <a
              href="mailto:kontakt@fuelstats.example"
              className="mt-4 inline-block text-sm link link-hover"
              aria-label={t("footer.contactAria")}
            >
              {t("footer.contact")}
            </a>
          </div>
        </div>

        <div className="text-center text-xs text-gray-500 mt-6">
          © {new Date().getFullYear()} FuelStats · {t("footer.allRights")}
        </div>
      </div>
    </footer>
  );
}
