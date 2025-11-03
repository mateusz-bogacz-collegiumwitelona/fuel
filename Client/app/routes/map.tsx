import { useEffect, useState } from "react";
import "leaflet/dist/leaflet.css";

type Station = {
  brandName: string;
  address: string;
  latitude: number;
  longitude: number;
};

export default function MapView() {
  const [stations, setStations] = useState<Station[]>([]);
  const [MapComponents, setMapComponents] = useState<{
    MapContainer?: any;
    TileLayer?: any;
    Marker?: any;
    Popup?: any;
  }>({});
  const [L, setL] = useState<any>(null);

  const [filters, setFilters] = useState({
    brandName: [] as string[],
    locationLatitude: null as number | null,
    locationLongitude: null as number | null,
    distance: null as number | null,
  });

  const brandColors: Record<string, string> = {
    Default:"black",
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
      const [rl, leaflet] = await Promise.all([
        import("react-leaflet"),
        import("leaflet"),
      ]);

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

          if (filters.brandName.length > 0) {
      const normalized =
        filters.brandName[0].charAt(0).toUpperCase() +
        filters.brandName[0].slice(1).toLowerCase();
      filters.brandName = [normalized];
    }

      const response = await fetch("http://localhost:5111/api/station/map/all", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        credentials: "include",
        body: JSON.stringify(filters),
      });

      if (!response.ok) throw new Error(`Błąd serwera: ${response.status}`);

      const data = await response.json();
      console.log("Odebrano dane:", data);
      setStations(data);
    } catch (e) {
      console.error("Błąd pobierania stacji:", e);
    }
  };

  useEffect(() => {
    fetchStations();
  }, []);

  const { MapContainer, TileLayer, Marker, Popup } = MapComponents;
  if (!MapContainer || !L) return null;

const getMarkerIcon = (brand: string) => { 
const normalizedBrand = Object.keys(brandColors).find( (key) => key.toLowerCase() === brand.toLowerCase() ); 
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
    <div className="min-h-screen bg-gray-900 text-white">

      <header className="w-full bg-gray-800 shadow-sm">
        <div className="mx-auto max-w-6xl px-4 py-3 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="text-xl font-bold">FuelStats</div>
            <nav className="hidden md:flex gap-2 items-center">
              <a href="/dashboard" className="btn btn-ghost btn-sm">Dashboard</a>
              <a href="/map" className="btn btn-ghost btn-sm btn-active">Mapa</a>
              <a href="/list" className="btn btn-ghost btn-sm">Lista</a>
            </nav>
          </div>
        </div>
      </header>


      <main className="mx-auto max-w-6xl px-4 py-8">
        <div className="flex items-center justify-between mb-6">
          <h1 className="text-2xl md:text-3xl font-bold">
            Mapa stacji benzynowych
          </h1>
            <a href="/dashboard"
            className="btn bg-blue-600 hover:bg-blue-500 text-white font-semibold text-base px-5 py-2 rounded-l shadow-lg transition-all duration-200">
            ← Powrót do dashboardu
            </a>
        </div>

<div className="bg-gray-800 p-4 rounded-xl shadow-md mb-6 flex flex-wrap gap-3 items-center">
  <input
    type="text"
    placeholder="Wpisz nazwę stacji (np. Orlen)"
    className="input input-bordered bg-gray-700 text-white w-64 placeholder-gray-400"
    onChange={(e) =>
      setFilters((f) => ({
        ...f,
        brandName: e.target.value
          ? [e.target.value.trim().toLowerCase()]
          : [],
      }))
    }
  />

  <button
    className="btn bg-blue-600 hover:bg-blue-500 text-white font-medium"
    onClick={fetchStations}
  >
  Szukaj
  </button>
</div>

        <div className="bg-gray-800 rounded-xl shadow-lg overflow-hidden border border-gray-700">
          <div className="h-[70vh] w-full">
            <MapContainer
              center={[52.2297, 21.0122]}
              zoom={7}
              style={{ height: "100%", width: "100%" }}
            >
              <TileLayer
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                attribution="&copy; OpenStreetMap contributors"
              />
              {stations.map((s, i) => (
                <Marker
                  key={i}
                  position={[s.latitude, s.longitude]}
                  icon={getMarkerIcon(s.brandName)}
                >
                  <Popup>
                    <strong>{s.brandName}</strong>
                    <br />
                    {s.address}
                  </Popup>
                </Marker>
              ))}
            </MapContainer>
          </div>
        </div>
      </main>


      <footer className="mt-12 py-6 text-center text-sm text-gray-400">
        © FuelStats
      </footer>
    </div>
  );
}