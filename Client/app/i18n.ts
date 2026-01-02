import i18n from "i18next";
import { initReactI18next } from "react-i18next";

type MaybePromise<T> = T | Promise<T>;

let initPromise: Promise<typeof i18n> | null = null;

if (typeof window !== "undefined" && !i18n.isInitialized) {
  initPromise = (async () => {
    const HttpBackend = (await import("i18next-http-backend")).default;
    const LanguageDetector = (await import("i18next-browser-languagedetector")).default;

    const devCacheBust =
      process.env.NODE_ENV === "development" ? `?v=${Date.now()}` : "";

    await i18n
      .use(HttpBackend)
      .use(LanguageDetector)
      .use(initReactI18next)
      .init({
        //basic settings
        fallbackLng: "pl",
        supportedLngs: ["pl", "en"],
        ns: ["messages"],
        defaultNS: "messages",
        backend: {
          loadPath: `/locales/{{lng}}/{{ns}}.json${devCacheBust}`,
        },
        detection: {
          order: ["localStorage", "querystring", "navigator"],
          caches: ["localStorage"],
        },

        interpolation: { escapeValue: false },
        debug: process.env.NODE_ENV === "development",

        react: { useSuspense: true },
      });

    // change language in <html lang=...>
    if (typeof document !== "undefined") {
      document.documentElement.lang = i18n.language || "pl";
    }

    if (process.env.NODE_ENV === "development" && typeof window !== "undefined") {
      (window as any).__I18N_RELOAD = async () => {
        await i18n.reloadResources();
        await i18n.changeLanguage(i18n.language);
        return true;
      };
    }

    return i18n;
  })();
} else {
  initPromise = Promise.resolve(i18n);
}

export const i18nReady: Promise<typeof i18n> = initPromise!;

export default i18n;
