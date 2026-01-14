import React from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";

export default function FooterHome() {
  const { t } = useTranslation();

  return (
    <footer className="w-full bg-base-300 text-base-content py-8 mt-12">
      <div className="mx-auto max-w-6xl px-4 text-center">
        <p className="text-sm">
          {t("home.footerContact", {
            defaultValue: "Kontakt: kontakt@fuelstats.example",
          })}
        </p>

        <div className="mt-3 text-xs text-gray-500">
          <Link to="/privacy" className="link link-hover mr-3">
            {t("footer.links.privacy")}
          </Link>
          <Link to="/delete-account" className="link link-hover">
            {t("footer.links.delete-account")}
          </Link>
        </div>

        <div className="text-xs text-gray-400 mt-4">
          © {new Date().getFullYear()} FuelStats
        </div>
      </div>
    </footer>
  );
}
