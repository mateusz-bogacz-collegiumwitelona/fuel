import i18n from "i18next";
import { initReactI18next } from "react-i18next";

if (typeof window !== "undefined" && !i18n.isInitialized) {
  (async () => {
    const HttpBackend = (await import("i18next-http-backend")).default;
    const LanguageDetector = (await import("i18next-browser-languagedetector")).default;

    i18n
      .use(HttpBackend)
      .use(LanguageDetector)
      .use(initReactI18next)
      .init({
        fallbackLng: "pl",
        supportedLngs: ["pl", "en"],
        ns: ["messages"],
        defaultNS: "messages",
        backend: {
          loadPath: "/locales/{{lng}}/{{ns}}.json",
        },
        detection: {
          order: ["localStorage", "querystring", "navigator"],
          caches: ["localStorage"],
        },
        interpolation: { escapeValue: false },
        react: { useSuspense: false },
      });

    if (typeof document !== "undefined") {
      document.documentElement.lang = i18n.language || "pl";
    }
  })();
}

export default i18n;