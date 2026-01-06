import { useEffect, useState, lazy, Suspense } from "react";
import Header from "../components/header";
import Footer from "../components/footer";
import { API_BASE } from "../components/api";
import { useTranslation } from "react-i18next";

const GlobalMapContent = lazy(() => import("../components/GlobalMapContent"));

type Station = {
  brandName: string;
  street: string;
  houseNumber: string;
  city: string;
  postcode: string;
  latitude: number;
  longitude: number;
};

export default function MapView(): JSX.Element {
  const { t } = useTranslation();

  const [stations, setStations] = useState<Station[]>([]);
  const [allStations, setAllStations] = useState<Station[]>([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [isClient, setIsClient] = useState(false);

  useEffect(() => {
    setIsClient(true);
    fetchStations();
  }, []);

  const fetchStations = async () => {
    try {
      const body = {
        brandName: [] as string[],
        locationLatitude: null as number | null,
        locationLongitude: null as number | null,
        distance: null as number | null,
      };

      const response = await fetch(`${API_BASE}/api/station/map/all`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        credentials: "include",
        body: JSON.stringify(body),
      });

      if (!response.ok) throw new Error(`Błąd serwera: ${response.status}`);

      const data: Station[] = await response.json();
      console.log("Odebrano dane:", data);
      setAllStations(data);
      setStations(data);
    } catch (e) {
      console.error("Błąd pobierania stacji:", e);
    }
  };

  const handleSearch = () => {
    const q = searchTerm.trim().toLowerCase();

    if (!q) {
      setStations(allStations);
      return;
    }

    const filtered = allStations.filter((s) =>
      (s.brandName ?? "").toLowerCase().includes(q),
    );
    setStations(filtered);
  };

  return (
    <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
      <Header />

      <main className="mx-auto max-w-6xl px-4 py-8 flex-grow w-full">
        <div className="flex items-center justify-between mb-6">
          <h1 className="text-2xl md:text-3xl font-bold">{t("map.fuelstationmap")}</h1>
          <a href="/dashboard" className="btn btn-outline">
            {t("map.dashboardback")}
          </a>
        </div>

        <div className="bg-base-300 p-4 rounded-xl shadow-md mb-6 flex flex-wrap gap-3 items-center">
          <input
            type="text"
            placeholder={t("map.stationname")}
            className="input input-bordered bg-base-100 text-base-content w-64 placeholder-gray-400"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />

          <button className="btn bg-blue-600 hover:bg-blue-500 text-white font-medium" onClick={handleSearch}>
            {t("map.search")}
          </button>
        </div>

        <div className="bg-base-300 rounded-xl shadow-lg overflow-hidden border border-base-content/10">
          <div className="h-[70vh] w-full relative">
            {isClient ? (
              <Suspense
                fallback={
                  <div className="flex h-full w-full items-center justify-center bg-base-200 text-base-content/50">
                    <span className="loading loading-spinner loading-lg"></span>
                  </div>
                }
              >
                <GlobalMapContent 
                    stations={stations} 
                    searchLabel={t("map.seedetails")} 
                />
              </Suspense>
            ) : (
              <div className="flex h-full w-full items-center justify-center bg-base-200 text-base-content/50">
                Ładowanie mapy...
              </div>
            )}
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
}