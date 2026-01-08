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
  React.useEffect(() => {
    document.title = t("map.title") + " - FuelStats";
  }, [t]);

  const [stations, setStations] = useState<Station[]>([]);
  const [allStations, setAllStations] = useState<Station[]>([]);
  

  const [searchTerm, setSearchTerm] = useState("");
  const [searchRadius, setSearchRadius] = useState<string>("50"); 
  const [userLocation, setUserLocation] = useState<{lat: number, lng: number} | null>(null);

  const [isClient, setIsClient] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [isLocating, setIsLocating] = useState(false);

  useEffect(() => {
    setIsClient(true);
    fetchStations();
  }, []);

  const calculateDistance = (lat1: number, lon1: number, lat2: number, lon2: number) => {
    const R = 6371; 
    const dLat = (lat2 - lat1) * (Math.PI / 180);
    const dLon = (lon2 - lon1) * (Math.PI / 180);
    const a =
      Math.sin(dLat / 2) * Math.sin(dLat / 2) +
      Math.cos(lat1 * (Math.PI / 180)) *
        Math.cos(lat2 * (Math.PI / 180)) *
        Math.sin(dLon / 2) *
        Math.sin(dLon / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c; 
  };

  const fetchStations = async (lat?: number, lng?: number) => {
    setIsLoading(true);
    try {
      const finalLat = lat ?? userLocation?.lat ?? null;
      const finalLng = lng ?? userLocation?.lng ?? null;
      const dist = (finalLat && finalLng && searchRadius) ? parseFloat(searchRadius) : null;

      const body = {
        brandName: [],
        locationLatitude: finalLat,
        locationLongitude: finalLng,
        distance: dist, 
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

      if (finalLat && finalLng && dist) {
        data = data.filter(s => {
            if (!s.latitude || !s.longitude) return false;
            const distanceKm = calculateDistance(finalLat, finalLng, s.latitude, s.longitude);
            return distanceKm <= dist;
        });
      }

      
      setAllStations(data);
      
      if (searchTerm) {
        filterData(data, searchTerm);
      } else {
        setStations(data);
      }

      if (finalLat && finalLng && data.length === 0) {
        setTimeout(() => alert(t("map.no_stations_found", { distance: dist })), 100);
      }

    } catch (e: any) {
      console.error(e);
      alert(t("map.error_prefix") + " " + e.message);
    } finally {
      setIsLoading(false);
      setIsLocating(false);
    }
  };

  const filterData = (data: Station[], term: string) => {
    const q = term.trim().toLowerCase();
    if (!q) {
      setStations(data);
      return;
    }
    const filtered = data.filter((s) => {
      const brand = (s.brandName ?? "").toLowerCase();
      const city = (s.city ?? "").toLowerCase();
      const street = (s.street ?? "").toLowerCase();
      return brand.includes(q) || city.includes(q) || street.includes(q);
    });
    setStations(filtered);
  };

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setSearchTerm(val);
    filterData(allStations, val);
  };

  const handleSearchClick = () => {
    fetchStations(); 
  };

  const handleUseMyLocation = () => {
    if (typeof navigator === "undefined" || !("geolocation" in navigator)) {
      alert(t("map.geolocation_not_supported"));
      return;
    }

    setIsLocating(true);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        const coords = { lat: pos.coords.latitude, lng: pos.coords.longitude };
        
        setUserLocation(coords);
        setSearchTerm("");
        fetchStations(coords.lat, coords.lng);
      },
      (err) => {
        console.warn(err);
        setIsLocating(false);
        alert(t("map.location_permission_error"));
      },
      { timeout: 10000, enableHighAccuracy: true }
    );
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
          
          <div className="relative w-64 md:w-80 flex-grow md:flex-grow-0">
            <input
              type="text"
              placeholder={t("map.search_placeholder") || "Nazwa, miasto, ulica..."}
              className="input input-bordered bg-base-100 w-full pr-10"
              value={searchTerm}
              onChange={handleSearchChange}
            />
            <button 
                className={`absolute right-2 top-1/2 -translate-y-1/2 btn btn-ghost btn-circle btn-sm tooltip tooltip-left ${userLocation ? 'text-green-600' : 'text-base-content/60'}`}
                data-tip={userLocation ? t("map.location_active") : t("map.use_my_location")}
                onClick={handleUseMyLocation}
                disabled={isLocating}
                type="button"
            >
                {isLocating ? (
                    <span className="loading loading-spinner loading-xs"></span>
                ) : (
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                    </svg>
                )}
            </button>
          </div>

          <div className={`flex items-center gap-2 transition-opacity duration-300 ${userLocation ? 'opacity-100' : 'opacity-50 pointer-events-none grayscale'}`}>
             <span className="text-sm font-semibold whitespace-nowrap">{t("map.distance_label")}:</span>
             <div className="join">
                <input 
                    type="number" 
                    min="1" 
                    max="500"
                    className="input input-bordered w-20 join-item text-center px-1" 
                    value={searchRadius}
                    onChange={(e) => setSearchRadius(e.target.value)}
                />
                <div className="btn btn-disabled join-item bg-base-200 border-base-content/20 text-base-content">
                    {t("map.km")}
                </div>
             </div>
          </div>

          <button 
            className="btn bg-blue-600 hover:bg-blue-500 text-white font-medium min-w-[100px] ml-auto md:ml-0" 
            onClick={handleSearchClick}
            disabled={isLoading}
          >
            {isLoading && !isLocating ? <span className="loading loading-spinner loading-xs"></span> : t("map.search")}
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
            
            <div className="absolute top-3 right-3 z-[400] bg-white/90 dark:bg-black/80 backdrop-blur px-3 py-1 rounded shadow text-sm font-semibold">
               {t("map.results_count")}: {stations.length}
            </div>
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
}