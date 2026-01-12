import { useEffect } from "react";
import { useTranslation } from "react-i18next";
import Header from "../components/header";
import Footer from "../components/footer";

export default function PrivacyPolicy() {
  const { t } = useTranslation();

  useEffect(() => {
    document.title = t("privacy.title") + " - FuelStats";
  }, [t]);

  return (
    <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
      <Header />

      <main className="mx-auto max-w-4xl px-4 py-10 flex-grow w-full">
        <div className="bg-base-300 p-8 rounded-xl shadow-md">
          <h1 className="text-3xl font-bold mb-6">{t("privacy.title")}</h1>

          <div className="space-y-6">
            <section>
              <h2 className="text-xl font-semibold mb-2">{t("privacy.intro_header")}</h2>
              <p>{t("privacy.intro_text")}</p>
            </section>

            <section>
              <h2 className="text-xl font-semibold mb-2">{t("privacy.admin_header")}</h2>
              <p>{t("privacy.admin_text")}</p>
            </section>

            <section>
              <h2 className="text-xl font-semibold mb-2">{t("privacy.data_header")}</h2>
              <p>{t("privacy.data_text")}</p>
              <ul className="list-disc list-inside ml-4 mt-2 opacity-80">
                <li>{t("privacy.data_point_1")}</li>
                <li>{t("privacy.data_point_2")}</li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold mb-2">{t("privacy.purpose_header")}</h2>
              <p>{t("privacy.purpose_text")}</p>
            </section>

            <section>
              <h2 className="text-xl font-semibold mb-2">{t("privacy.cookies_header")}</h2>
              <p>{t("privacy.cookies_text")}</p>
            </section>

            <section>
              <h2 className="text-xl font-semibold mb-2">{t("privacy.rights_header")}</h2>
              <p>{t("privacy.rights_text")}</p>
            </section>

            <section>
              <h2 className="text-xl font-semibold mb-2">{t("privacy.contact_header")}</h2>
              <p>{t("privacy.contact_text")} <strong>fuellysupport@fuelly.com.pl</strong></p>
            </section>
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
}