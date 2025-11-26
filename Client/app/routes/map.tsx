import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import "leaflet/dist/leaflet.css";
import Header from "../components/header";
import Footer from "../components/footer";
import { useTranslation } from "react-i18next";

type Station = {
  brandName: string;
  street: string;
  houseNumber: string;
  city: string;
  postcode: string;
  latitude: number;
  longitude: number;
};

export default function MapView() {
  const { t, i18n } = useTranslation();
  const [stations, setStations] = useState<Station[]>([]);
  const [allStations, setAllStations] = useState<Station[]>([]);
  const [MapComponents, setMapComponents] = useState<{
    MapContainer?: any;
    TileLayer?: any;
    Marker?: any;
    Popup?: any;
  }>({});
  const [L, setL] = useState<any>(null);

  const [searchTerm, setSearchTerm] = useState("");

  const brandColors: Record<string, string> = {
    Default: "black",
    Orlen: "red",
    BP: "green",
    Shell: "yellow",
    "Circle K": "orange",
    Moya: "blue",
    Lotos: "gold",
  };

  useEffect(() => {
    if (typeof window === "undefined") return;

    (async () => {
      const [rl, leaflet] = await Promise.all([import("react-leaflet"), import("leaflet")]);

      setL(leaflet);
      setMapComponents({
        MapContainer: rl.MapContainer,
        TileLayer: rl.TileLayer,
        Marker: rl.Marker,
        Popup: rl.Popup,
      });
    })();
  }, []);

  const fetchStations = async () => {
    try {
      const token = localStorage.getItem("token");

      const body = {
        brandName: [] as string[],
        locationLatitude: null as number | null,
        locationLongitude: null as number | null,
        distance: null as number | null,
      };

      const response = await fetch("http://localhost:5111/api/station/map/all", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
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

  useEffect(() => {
    fetchStations();
  }, []);

  const handleSearch = () => {
    const q = searchTerm.trim().toLowerCase();

    if (!q) {
      setStations(allStations);
      return;
    }

    const filtered = allStations.filter((s) => s.brandName.toLowerCase().includes(q));
    setStations(filtered);
  };

  const { MapContainer, TileLayer, Marker, Popup } = MapComponents;
  if (!MapContainer || !L) return null;

  const getMarkerIcon = (brand: string) => {
    const normalizedBrand = Object.keys(brandColors).find(
      (key) => key.toLowerCase() === brand.toLowerCase()
    );
    const color = normalizedBrand ? brandColors[normalizedBrand] : brandColors.Default;
    return new L.Icon({
      iconUrl: `https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-${color}.png`,
      shadowUrl:
        "https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-shadow.png",
      iconSize: [25, 41],
      iconAnchor: [12, 41],
      popupAnchor: [1, -34],
      shadowSize: [41, 41],
    });
  };

  return (
    <div className="min-h-screen bg-base-200 text-base-content">
      <Header />

      <main className="mx-auto max-w-6xl px-4 py-8">
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

        <div className="bg-base-800 rounded-xl shadow-lg overflow-hidden border border-base-700">
          <div className="h-[70vh] w-full">
            <MapContainer center={[52.2297, 21.0122]} zoom={7} style={{ height: "100%", width: "100%" }}>
              <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" attribution="&copy; OpenStreetMap contributors" />
              {stations.map((s, i) => (
                <Marker key={i} position={[s.latitude, s.longitude]} icon={getMarkerIcon(s.brandName)}>
                  <Popup>
                    <div className="space-y-1">
                      <div>
                        <strong>{s.brandName}</strong>
                        <br />
                        <strong>
                          {s.city}, {s.street} {s.houseNumber}
                        </strong>
                      </div>
                      <Link
                        to={`/station/${encodeURIComponent(s.brandName)}/${encodeURIComponent(s.city)}/${encodeURIComponent(
                          s.street
                        )}/${encodeURIComponent(s.houseNumber)}`}
                        className="btn btn-xs btn-base mt-2"
                      >
                        {t("map.seedetails")}
                      </Link>
                    </div>
                  </Popup>
                </Marker>
              ))}
            </MapContainer>
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
}
