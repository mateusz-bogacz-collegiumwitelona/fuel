import React from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import ThemeController from "./ThemeController";
import { useTheme } from "../context/ThemeContext";

export default function HeaderHome() {
  const { i18n, t } = useTranslation();
  const { theme, setTheme } = useTheme();

  const changeLanguage = (lng: string) => {
    i18n.changeLanguage(lng);
    try {
      localStorage.setItem("i18nextLng", lng);
      if (typeof document !== "undefined") {
        document.documentElement.lang = lng;
      }
    } catch {}
  };

  return (
    <header className="w-full bg-base-300 text-base-content shadow-md">
      <div className="mx-auto max-w-6xl px-4 py-4 flex items-center justify-between">
        <a href="/" className="flex items-center gap-3">
          <img
            src="/images/fuelstats.png"
            alt="FuelStats"
            className="h-10 w-10 rounded-sm shadow"
          />
          <span className="text-2xl font-extrabold tracking-wide">
            FuelStats
          </span>
        </a>

        <div className="flex items-center gap-3">
          {/* Language */}
          <div className="dropdown">
            <div tabIndex={0} role="button" className="btn m-1">
              {t("buttons.language")}
            </div>
            <ul
              tabIndex={0}
              className="dropdown-content menu bg-base-100 rounded-box z-[1] w-52 p-2 shadow-sm"
            >
              <li>
                <a
                  role="button"
                  onClick={() => changeLanguage("pl")}
                  className={i18n.language === "pl" ? "font-bold" : ""}
                >
                  {t("buttons.polish")}
                </a>
              </li>
              <li>
                <a
                  role="button"
                  onClick={() => changeLanguage("en")}
                  className={i18n.language === "en" ? "font-bold" : ""}
                >
                  {t("buttons.english")}
                </a>
              </li>
            </ul>
          </div>

          <ThemeController theme={theme} setTheme={setTheme} />

          {/* Menu */}
          <div className="dropdown dropdown-end">
            <label tabIndex={0} className="btn btn-ghost btn-square">
              <svg
                xmlns="http://www.w3.org/2000/svg"
                className="h-5 w-5"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth="2"
                  d="M4 6h16M4 12h16M4 18h16"
                />
              </svg>
            </label>

            <ul className="menu menu-compact dropdown-content mt-3 p-2 shadow bg-base-100 rounded-box w-52">
              <Link to="/login" className="btn btn-sm btn-outline">
                {t("home.ctaLogin", { defaultValue: "Login" })}
              </Link>
              <Link to="/register" className="btn btn-sm btn-outline">
                {t("home.ctaRegister", { defaultValue: "Register" })}
              </Link>
            </ul>
          </div>
        </div>
      </div>
    </header>
  );
}