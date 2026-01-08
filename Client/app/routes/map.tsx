import { useEffect, useState, lazy, Suspense, useRef } from "react";
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
  const [availableBrands, setAvailableBrands] = useState<string[]>([]);
  const [selectedBrands, setSelectedBrands] = useState<string[]>([]);
  const [searchTerm, setSearchTerm] = useState("");

  const [isClient, setIsClient] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const dropdownRef = useRef<HTMLDetailsElement>(null);

  useEffect(() => {
    setIsClient(true);
    fetchBrands();   
    fetchStations(); 
  }, []);

  const fetchBrands = async () => {
    try {
      const response = await fetch(`${API_BASE}/api/station/all-brands`, {
        method: "GET",
        headers: { Accept: "application/json" },
        credentials: "include",
      });
      if (response.ok) {
        const data = await response.json();
        setAvailableBrands(data);
      }
    } catch (e) {
      console.error(e);
    }
  };

  const fetchStations = async () => {
    setIsLoading(true);
    if (dropdownRef.current) {
      dropdownRef.current.removeAttribute("open");
    }

    try {
      const body = {
        brandName: selectedBrands.length > 0 ? selectedBrands : [],
        locationLatitude: null,
        locationLongitude: null,
        distance: null, 
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

      if (!response.ok) throw new Error(`${t("map.server_error")}: ${response.status}`);

      let data: Station[] = await response.json();

      if (searchTerm.trim()) {
         const q = searchTerm.trim().toLowerCase();
         data = data.filter(s => {
            const city = (s.city ?? "").toLowerCase();
            const street = (s.street ?? "").toLowerCase();
            const brand = (s.brandName ?? "").toLowerCase();
            return city.includes(q) || street.includes(q) || brand.includes(q);
         });
      }
      
      setStations(data);

    } catch (e: any) {
      console.error(e);
      alert(t("map.error_prefix") + " " + e.message);
    } finally {
      setIsLoading(false);
    }
  };

  const toggleBrand = (brand: string) => {
    setSelectedBrands(prev => 
      prev.includes(brand) 
        ? prev.filter(b => b !== brand)
        : [...prev, brand]
    );
  };

  const handleSearchClick = () => {
    fetchStations(); 
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') fetchStations();
  };

  return (
    <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
      <Header />

      <main className="mx-auto max-w-7xl px-4 py-8 flex-grow w-full">
        <div className="flex items-center justify-between mb-6">
          <h1 className="text-2xl md:text-3xl font-bold">{t("map.fuelstationmap")}</h1>
          <a href="/dashboard" className="btn btn-outline">
            {t("map.dashboardback")}
          </a>
        </div>

        <div className="bg-base-300 p-4 rounded-xl shadow-md mb-6 flex flex-col md:flex-row gap-4 items-stretch md:items-center">
          
          <div className="flex-grow form-control">
            <input
              type="text"
              placeholder={t("map.search_placeholder") || "Miasto, ulica..."}
              className="input input-bordered w-full"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              onKeyDown={handleKeyDown}
            />
          </div>

          <div className="relative">
             <details className="dropdown" ref={dropdownRef}>
                <summary className="btn bg-base-100 border-base-content/20 w-full md:w-56 justify-between">
                  {selectedBrands.length === 0 
                    ? (t("map.all_brands") || "Wszystkie marki") 
                    : `${selectedBrands.length} wybranych`}
                  <svg className="fill-current" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path d="M7 10l5 5 5-5z"/></svg>
                </summary>
                <ul className="p-2 shadow menu dropdown-content z-[1] bg-base-100 rounded-box w-64 max-h-60 overflow-y-auto block">
                  {availableBrands.length > 0 ? availableBrands.map((brand) => (
                    <li key={brand}>
                      <label className="label cursor-pointer justify-start gap-3">
                        <input 
                          type="checkbox" 
                          className="checkbox checkbox-sm checkbox-primary" 
                          checked={selectedBrands.includes(brand)}
                          onChange={() => toggleBrand(brand)}
                        />
                        <span className="label-text">{brand}</span>
                      </label>
                    </li>
                  )) : (
                    <li className="text-sm p-2 text-base-content/50">Ładowanie marek...</li>
                  )}
                </ul>
             </details>
          </div>

          <button 
            className="btn btn-primary min-w-[120px]" 
            onClick={handleSearchClick}
            disabled={isLoading}
          >
            {isLoading ? <span className="loading loading-spinner loading-xs"></span> : t("map.search")}
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
                    enableDetailsLink={true}
                />
              </Suspense>
            ) : (
              <div className="flex h-full w-full items-center justify-center bg-base-200 text-base-content/50">
                {t("map.loading")}
              </div>
            )}
            
            <div className="absolute top-3 right-3 z-[400] bg-white/90 dark:bg-black/80 backdrop-blur px-3 py-1.5 rounded-lg shadow border border-base-content/20 text-sm font-semibold">
               {t("map.results_count")}: {stations.length}
            </div>
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
}