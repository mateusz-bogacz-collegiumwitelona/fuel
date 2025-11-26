import * as React from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";

export default function Footer() {
  const { t } = useTranslation();

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

          {/* Left: Twórcy */}
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

          {/* Middle: Odniesienia */}
          <div className="flex flex-col items-center">
            <h3 className="font-semibold text-lg text-center">{t("footer.linksTitle")}</h3>
            <ul className="mt-2 space-y-1 text-sm text-center">
              <li>
                <Link to="/dashboard" className="link link-hover">
                  {t("footer.links.dashboard")}
                </Link>
              </li>
              <li>
                <Link to="/settings" className="link link-hover">
                  {t("footer.links.settings")}
                </Link>
              </li>
              <li>
                <Link to="/map" className="link link-hover">
                  {t("footer.links.map")}
                </Link>
              </li>
              <li>
                <Link to="/list" className="link link-hover">
                  {t("footer.links.list")}
                </Link>
              </li>
            </ul>
          </div>

          {/* Right: Kontakt / prawa */}
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
