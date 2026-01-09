import { useEffect, useState, lazy, Suspense, useRef } from "react";
import Header from "../components/header";
import Footer from "../components/footer";
import { API_BASE } from "../components/api";
import { useTranslation } from "react-i18next";
import { useUserGuard } from "../components/useUserGuard"; 

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
  const { state } = useUserGuard(); 

  useEffect(() => {
    document.title = t("map.fuelstationmap", "Mapa stacji") + " - FuelStats";
  }, [t]);
  
  const [stations, setStations] = useState<Station[]>([]);
  const [availableBrands, setAvailableBrands] = useState<string[]>([]);
  
  const [selectedBrands, setSelectedBrands] = useState<string[]>([]);
  const [userLocation, setUserLocation] = useState<{lat: number, lng: number} | null>(null);
  const [searchRadius, setSearchRadius] = useState<number>(20); 

  const [isClient, setIsClient] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [isLocating, setIsLocating] = useState(false);

  const dropdownRef = useRef<HTMLDetailsElement>(null);

  useEffect(() => {
    setIsClient(true);
    if (state === "allowed") {
      fetchBrands();
      fetchStations(); 
    }
  }, [state]);

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

  const fetchStations = async (overrideLoc?: {lat: number, lng: number}) => {
    setIsLoading(true);
    if (dropdownRef.current) dropdownRef.current.removeAttribute("open");

    try {
      const loc = overrideLoc || userLocation;

      const body = {
        brandName: selectedBrands.length > 0 ? selectedBrands : [],
        locationLatitude: loc ? loc.lat : null,
        locationLongitude: loc ? loc.lng : null,
        distance: loc ? searchRadius : null, 
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

      if (response.status === 404) {
        setStations([]);
        setIsLoading(false);
        return; 
      }

      if (!response.ok) throw new Error(`${t("map.server_error")}: ${response.status}`);

      const data: Station[] = await response.json();
      setStations(data);

      if (loc && data.length === 0) {
        alert(t("map.no_stations_found", { distance: searchRadius }));
      }

    } catch (e: any) {
      console.error(e);
      alert(t("map.error_prefix") + " " + e.message);
    } finally {
      setIsLoading(false);
      setIsLocating(false);
    }
  };

  const handleUseMyLocation = () => {
    if (!navigator.geolocation) {
      alert(t("map.geolocation_not_supported"));
      return;
    }

    setIsLocating(true);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        const coords = { lat: pos.coords.latitude, lng: pos.coords.longitude };
        setUserLocation(coords);
        fetchStations(coords); 
      },
      (err) => {
        console.warn(err);
        setIsLocating(false);
        alert(t("map.location_permission_error"));
      },
      { enableHighAccuracy: false, timeout: 10000 }
    );
  };

  const clearLocation = () => {
    setUserLocation(null);
    setTimeout(() => fetchStations(), 0); 
  };

  const toggleBrand = (brand: string) => {
    setSelectedBrands(prev => 
      prev.includes(brand) ? prev.filter(b => b !== brand) : [...prev, brand]
    );
  };
  if (state === "checking") {
    return (
      <div className="min-h-screen bg-base-200 flex items-center justify-center">
        <span className="loading loading-spinner loading-lg" />
      </div>
    );
  }

  if (state !== "allowed") {
    return null;
  }

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

        <div className="bg-base-300 p-4 rounded-xl shadow-md mb-6 flex flex-col md:flex-row gap-4 items-stretch md:items-center flex-wrap">

          <div className="relative">
             <details className="dropdown" ref={dropdownRef}>
                <summary className="btn bg-base-100 border-base-content/20 w-full md:w-56 justify-between">
                  {selectedBrands.length === 0 
                    ? (t("map.all_brands")) 
                    : t("map.selected_count", { count: selectedBrands.length})}
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

          <div className="flex items-center gap-2">
            {!userLocation ? (
              <button 
                className="btn btn-outline gap-2"
                onClick={handleUseMyLocation}
                disabled={isLocating}
              >
                {isLocating ? <span className="loading loading-spinner loading-xs"/> : (
                   <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" /><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" /></svg>
                )}
                {t("map.use_my_location")}
              </button>
            ) : (
              <div className="flex items-center gap-2 bg-base-100 px-3 py-2 rounded-lg border border-base">
                <span className="text-base text-sm font-bold flex items-center gap-1">
                    {t("map.location_active")}
                </span>
                <button onClick={clearLocation} className="btn btn-xs btn-ghost text-base">X</button>
              </div>
            )}
          </div>

            {userLocation && (
            <div className="flex items-center gap-2 bg-base-100 px-3 py-2 rounded-lg border border-base-content/10">
              <span className="text-sm font-semibold whitespace-nowrap">{t("map.distance_label")}:</span>
              <input 
                type="number" 
                min="1" 
                max="500"
                className="input input-bordered input-sm w-20 text-center" 
                value={searchRadius}
                onChange={(e) => setSearchRadius(Number(e.target.value))}
              />
              <span className="text-sm font-bold">km</span>
            </div>
          )}

          <button 
            className="btn btn-primary min-w-[120px] ml-auto md:ml-0" 
            onClick={() => fetchStations()}
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
                    center={userLocation ? [userLocation.lat, userLocation.lng] : undefined}
                    zoom={userLocation ? 11 : undefined}
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