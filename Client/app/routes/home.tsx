import React, { useEffect } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import HeaderHome from "../components/HeaderHome";
import FooterHome from "../components/FooterHome";

async function loadExternalTranslations() {
  const langs = ["pl", "en"];

  for (const lng of langs) {
    try {
      const homeRes = await fetch(`/locales/${lng}/home.json`);
      if (homeRes.ok) {
        const homeJson = await homeRes.json();
        window?.i18next?.addResourceBundle?.(
          lng,
          "home",
          homeJson,
          true,
          true
        );
      }
    } catch {}

    try {
      const btnRes = await fetch(`/locales/${lng}/buttons.json`);
      if (btnRes.ok) {
        const btnJson = await btnRes.json();
        window?.i18next?.addResourceBundle?.(
          lng,
          "buttons",
          btnJson,
          true,
          true
        );
      }
    } catch {}
  }
}

export default function Home(): JSX.Element {
  const { t, i18n } = useTranslation();

    React.useEffect(() => {
      document.title = "FuelStats";
    }, [t]);

  useEffect(() => {
    if (typeof window !== "undefined") {
      loadExternalTranslations();
    }

    if (i18n?.language && typeof document !== "undefined") {
      document.documentElement.lang = i18n.language;
    }
  }, [i18n]);

  // ✅ BEZPIECZNE POBRANIE TABLICY
  const howItWorksRaw = t("home.howItWorks", { returnObjects: true });

  const howItWorks: string[] = Array.isArray(howItWorksRaw)
    ? howItWorksRaw
    : [];

  return (
    <div className="min-h-screen bg-base-100 text-base-content">
      <HeaderHome />

      <main className="mx-auto max-w-6xl px-4 py-12">
        {/* Hero */}
        <section className="grid grid-cols-1 md:grid-cols-2 gap-8 items-center">
          <div className="space-y-4">
            <h1 className="text-3xl md:text-5xl font-extrabold leading-tight">
              {t("home.title")}
            </h1>

            <p className="text-base md:text-lg text-gray-600">
              {t("home.subtitle")}
            </p>

            <div className="flex flex-wrap gap-3 mt-4">
              <Link to="/map" className="btn btn-primary btn-lg bg-info">
                {t("home.features.map") ?? "Mapa"}
              </Link>

              <Link to="/list" className="btn btn-outline btn-lg">
                {t("home.features.list") ?? "Lista"}
              </Link>
            </div>

            <ul className="mt-6 grid gap-3">
              {howItWorks.slice(0, 3).map((it, idx) => (
                <li key={idx} className="flex items-start gap-3">
                  <div className="flex-none w-8 h-8 rounded-full bg-info text-base-100 flex items-center justify-center font-semibold">
                    {idx + 1}
                  </div>
                  <div className="text-sm text-gray-700">{it}</div>
                </li>
              ))}
            </ul>
          </div>

          <div className="relative flex justify-center items-center">
            <div className="w-full max-w-md rounded-xl overflow-hidden shadow-2xl transform hover:scale-[1.01] transition">
              <img
                src="/images/map-preview.png"
                alt={
                  t("home.heroAlt") ??
                  "Podgląd mapy FuelStats"
                }
                className="object-cover w-full h-80 md:h-96"
              />

              <div className="absolute left-4 bottom-4 bg-base-200/90 backdrop-blur-sm p-3 rounded-lg">
                <div className="text-sm font-semibold">FuelStats</div>
                <div className="text-xs text-gray-600">
                  {t("home.subtitle")}
                </div>
              </div>
            </div>

            <div className="hidden md:block absolute -right-8 top-8 w-64 p-3 rounded-xl bg-base-200 shadow-lg animate-float">
              <div className="flex items-center gap-3">
                <img
                  src="/images/stacjaOrlen.png"
                  alt="Stacja przykładowa"
                  className="w-12 h-12 rounded-sm object-cover"
                />
                <div>
                  <div className="text-sm font-semibold">
                    Orlen — ul. Przykładowa 12
                  </div>
                  <div className="text-xs text-gray-500">
                    Diesel: 6.19 zł · PB95: 7.09 zł
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        {/* Features */}
        <section className="mt-12 grid grid-cols-1 md:grid-cols-3 gap-6">
          {[
            ["map", "mapDesc"],
            ["list", "listDesc"],
            ["dashboard", "dashboardDesc"],
          ].map(([key, desc]) => (
            <div
              key={key}
              className="p-6 rounded-xl bg-base-200 shadow hover:shadow-lg transition"
            >
              <h3 className="font-semibold mb-2">
                {t(`home.features.${key}`)}
              </h3>
              <p className="text-sm text-gray-600">
                {t(`home.features.${desc}`)}
              </p>
            </div>
          ))}
        </section>

        {/* CTA */}
        <section className="mt-12 rounded-xl overflow-hidden bg-gradient-to-r from-base-100 to-base-300 shadow-inner p-6 flex flex-col md:flex-row items-center justify-between gap-4">
          <div>
            <div className="text-lg font-semibold">
              {t("home.title")}
            </div>
            <div className="text-sm text-gray-600">
              {t("home.subtitle")}
            </div>
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

      <style>{`
        @keyframes floatY {
          0% { transform: translateY(0); }
          50% { transform: translateY(-8px); }
          100% { transform: translateY(0); }
        }
        .animate-float {
          animation: floatY 4s ease-in-out infinite;
        }
      `}</style>
    </div>
  );
}