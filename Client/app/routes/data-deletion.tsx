import { useEffect } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import Header from "../components/header";
import Footer from "../components/footer";

export default function DataDeletion() {
  const { t } = useTranslation();

  useEffect(() => {
    document.title = t("deletion.title") + " - FuelStats";
  }, [t]);

  return (
    <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
      <Header />

      <main className="mx-auto max-w-3xl px-4 py-10 flex-grow w-full">
        <div className="bg-base-300 p-8 rounded-xl shadow-md">
          <h1 className="text-3xl font-bold mb-6">{t("deletion.title")}</h1>
          
          <p className="mb-6 text-lg">{t("deletion.intro")}</p>

          <div className="space-y-4">
            <div className="flex gap-4 items-start">
              <div className="bg-primary text-primary-content w-8 h-8 flex items-center justify-center rounded-full font-bold flex-shrink-0">
                1
              </div>
              <div>
                <h3 className="font-bold text-lg">{t("deletion.step1_title")}</h3>
                <p className="opacity-80">{t("deletion.step1_desc")}</p>
              </div>
            </div>

            <div className="flex gap-4 items-start">
              <div className="bg-primary text-primary-content w-8 h-8 flex items-center justify-center rounded-full font-bold flex-shrink-0">
                2
              </div>
              <div>
                <h3 className="font-bold text-lg">{t("deletion.step2_title")}</h3>
                <p className="opacity-80">{t("deletion.step2_desc")}</p>
              </div>
            </div>

            <div className="flex gap-4 items-start">
              <div className="bg-primary text-primary-content w-8 h-8 flex items-center justify-center rounded-full font-bold flex-shrink-0">
                3
              </div>
              <div>
                <h3 className="font-bold text-lg">{t("deletion.step3_title")}</h3>
                <p className="opacity-80">{t("deletion.step3_desc")}</p>
              </div>
            </div>
          </div>

          <div className="divider my-8"></div>

          <div className="alert bg-base-100 shadow-lg">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" className="stroke-info shrink-0 w-6 h-6"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
            <div>
              <h3 className="font-bold">{t("deletion.manual_title")}</h3>
              <div className="text-xs">{t("deletion.manual_desc")} <strong>fuellysupport@fuelly.com.pl</strong></div>
            </div>
          </div>
          
          <div className="mt-6 text-center">
             <Link to="/login" className="btn btn-primary">
                {t("login.title")}
             </Link>
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
}