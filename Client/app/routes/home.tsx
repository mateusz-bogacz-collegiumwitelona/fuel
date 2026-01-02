import React, { useEffect } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import ThemeController from "../components/ThemeController";
import { useTheme } from "../context/ThemeContext";


function HeaderHome() {
  const { i18n, t } = useTranslation();
  const { theme, setTheme } = useTheme();

  const changeLanguage = (lng: string) => {
    i18n.changeLanguage(lng);
    try {
      localStorage.setItem("i18nextLng", lng);
      if (typeof document !== "undefined") document.documentElement.lang = lng;
    } catch {}
  };

  return (
    <header className="w-full bg-base-300 text-base-content shadow-md">
      <div className="mx-auto max-w-6xl px-4 py-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <a href="/" className="flex items-center gap-3">
            <img src="/images/fuelstats.png" alt="FuelStats" className="h-10 w-10 rounded-sm shadow" />
            <span className="text-2xl font-extrabold tracking-wide">FuelStats</span>
          </a>
        </div>

        <div className="flex items-center gap-3">
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

          <ThemeController theme={theme} setTheme={setTheme} />

          <Link to="/login" className="btn btn-sm btn-outline text-base-content">
            {t ? t("home.ctaLogin", { defaultValue: "Login" }) : "Login"}
          </Link>
          <Link to="/register" className="btn btn-sm btn-outline text-base-content">
            {t ? t("home.ctaRegister", { defaultValue: "Register" }) : "Register"}
          </Link>
        </div>
      </div>
    </header>
  );
}

function FooterHome() {
  const { t } = useTranslation();
  return (
    <footer className="w-full bg-base-300 text-base-content py-8 mt-12">
      <div className="mx-auto max-w-6xl px-4 text-center">
        <p className="text-sm">{t ? t("home.footerContact", { defaultValue: "Kontakt: kontakt@fuelstats.example" }) : "Kontakt: kontakt@fuelstats.example"}</p>
        <div className="mt-3 text-xs text-gray-500">
          <a className="link link-hover mr-3">{t ? t("home.privacy", { defaultValue: "Polityka prywatności" }) : "Polityka prywatności"}</a>
          <a className="link link-hover">{t ? t("home.terms", { defaultValue: "Regulamin" }) : "Regulamin"}</a>
        </div>
        <div className="text-xs text-gray-400 mt-4">© {new Date().getFullYear()} FuelStats</div>
      </div>
    </footer>
  );
}

async function loadExternalTranslations() {
  try {
    const langs = ["pl", "en"];
    for (const lng of langs) {
      try {
        const homeRes = await fetch(`/locales/${lng}/home.json`);
        if (homeRes.ok) {
          const homeJson = await homeRes.json();
          if (!window?.i18next?.hasResourceBundle || !window?.i18next?.hasResourceBundle(lng, "home")) {
            window?.i18next?.addResourceBundle?.(lng, "home", homeJson, true, true);
          }
        }
      } catch (e) {
        console.warn("Failed to load home translations for", lng, e);
      }

      try {
        const btnRes = await fetch(`/locales/${lng}/buttons.json`);
        if (btnRes.ok) {
          const btnJson = await btnRes.json();
          if (!window?.i18next?.hasResourceBundle || !window?.i18next?.hasResourceBundle(lng, "buttons")) {
            window?.i18next?.addResourceBundle?.(lng, "buttons", btnJson, true, true);
          }
        }
      } catch (e) {
        console.warn("Failed to load buttons translations for", lng, e);
      }
    }
  } catch (e) {
    // eslint-disable-next-line no-console
    console.warn("loadExternalTranslations failed", e);
  }
}

export default function Home(): JSX.Element {
  const { t, i18n } = useTranslation();

  useEffect(() => {
    // try load translations only on client
    if (typeof window !== "undefined") loadExternalTranslations();
    // ensure html lang set
    try {
      if (typeof document !== "undefined" && i18n?.language) document.documentElement.lang = i18n.language;
    } catch {}
  }, [i18n]);

  const howItWorksRaw = t("home.howItWorks", { returnObjects: true });
  const howItWorks = Array.isArray(howItWorksRaw) ? (howItWorksRaw as string[]) : [];

  return (
    <div className="min-h-screen bg-base-100 text-base-content">
      <HeaderHome />

      <main className="mx-auto max-w-6xl px-4 py-12">
        {/* Hero */}
        <section className="grid grid-cols-1 md:grid-cols-2 gap-8 items-center">
          <div className="space-y-4">
            <h1 className="text-3xl md:text-5xl font-extrabold leading-tight">{t("home.title")}</h1>
            <p className="text-base md:text-lg text-gray-600">{t("home.subtitle")}</p>

            <div className="flex flex-wrap gap-3 mt-4">
              <Link to="/map" className="btn btn-primary btn-lg bg-info">
                {t("home.features.map") ?? "Mapa"}
              </Link>

              <Link to="/list" className="btn btn-outline btn-lg">
                {t("home.features.list") ?? "Lista"}
              </Link>
            </div>

            <ul className="mt-6 grid gap-3">
              {howItWorks.slice(0, 3).map((it: string, idx: number) => (
                <li key={idx} className="flex items-start gap-3">
                  <div className="flex-none w-8 h-8 rounded-full bg-info text-base-100 flex items-center justify-center font-semibold">{idx + 1}</div>
                  <div className="text-sm text-gray-700">{it}</div>
                </li>
              ))}
            </ul>
          </div>

          <div className="relative flex justify-center items-center">
            <div className="w-full max-w-md rounded-xl overflow-hidden shadow-2xl transform hover:scale-[1.01] transition">
              <img src="/images/map-preview.png" alt={t("home.heroAlt") ?? "Podgląd mapy FuelStats"} className="object-cover w-full h-80 md:h-96" />

              <div className="absolute left-4 bottom-4 bg-base-200/90 backdrop-blur-sm p-3 rounded-lg">
                <div className="text-sm font-semibold">FuelStats</div>
                <div className="text-xs text-gray-600">{t("home.subtitle")}</div>
              </div>
            </div>

            {/* floating station card visible on md+ */}
            <div className="hidden md:block absolute -right-8 top-8 w-64 p-3 rounded-xl bg-base-200 shadow-lg animate-float">
              <div className="flex items-center gap-3">
                <img src="/images/stacjaOrlen.png" alt="Stacja przykładowa" className="w-12 h-12 rounded-sm object-cover" />
                <div>
                  <div className="text-sm font-semibold">Orlen — ul. Przykładowa 12</div>
                  <div className="text-xs text-gray-500">Diesel: 6.19 zł · PB95: 7.09 zł</div>
                </div>
              </div>
            </div>
          </div>
        </section>

        {/* Features */}
        <section className="mt-12 grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="p-6 rounded-xl bg-base-200 shadow hover:shadow-lg transition">
            <h3 className="font-semibold mb-2">{t("home.features.map")}</h3>
            <p className="text-sm text-gray-600">{t("home.features.mapDesc") ?? "Szukaj cen paliw na mapie i porównuj stacje."}</p>
          </div>

          <div className="p-6 rounded-xl bg-base-200 shadow hover:shadow-lg transition">
            <h3 className="font-semibold mb-2">{t("home.features.list")}</h3>
            <p className="text-sm text-gray-600">{t("home.features.listDesc") ?? "Lista najbliższych stacji i szybki podgląd cen."}</p>
          </div>

          <div className="p-6 rounded-xl bg-base-200 shadow hover:shadow-lg transition">
            <h3 className="font-semibold mb-2">{t("home.features.dashboard")}</h3>
            <p className="text-sm text-gray-600">{t("home.features.dashboardDesc") ?? "Twoje ulubione stacje, historia i alerty cenowe."}</p>
          </div>
        </section>

        {/* CTA strip */}
        <section className="mt-12 rounded-xl overflow-hidden bg-gradient-to-r from-base-100 to-base-300 shadow-inner p-6 flex flex-col md:flex-row items-center justify-between gap-4">
          <div>
            <div className="text-lg font-semibold">{t("home.title")}</div>
            <div className="text-sm text-gray-600">{t("home.subtitle")}</div>
          </div>
          <div className="flex gap-3">
            <Link to="/register" className="btn btn-primary bg-info">
              {t("home.ctaRegister")}
            </Link>
            <Link to="/login" className="btn btn-primary bg-info">
              {t("home.ctaLogin")}
            </Link>
          </div>
        </section>
      </main>

      <FooterHome />

      {/* small style for floating animation */}
      <style>{`
        @keyframes floatY { 0% { transform: translateY(0); } 50% { transform: translateY(-8px); } 100% { transform: translateY(0); } }
        .animate-float { animation: floatY 4s ease-in-out infinite; }
      `}</style>
    </div>
  );
}
