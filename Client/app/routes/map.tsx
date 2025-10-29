import { useEffect, useState } from "react";
import "leaflet/dist/leaflet.css";
import Header from "../components/header";
import Footer from "../components/footer";

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

  const brandColors: Record<string, string> = {
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

  useEffect(() => {
    if (typeof window === "undefined") return;

    const token = localStorage.getItem("token");

    fetch("http://localhost:5111/api/station/map/all", {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      credentials: "include",
    })
      .then((r) => {
        if (!r.ok) throw new Error(`Błąd serwera: ${r.status}`);
        return r.json();
      })
      .then((data: Station[]) => setStations(data))
      .catch((e) => console.error("Błąd pobierania stacji:", e));
  }, []);

  const { MapContainer, TileLayer, Marker, Popup } = MapComponents;
  if (!MapContainer || !L) return null; 


  const getMarkerIcon = (brand: string) => {
    const color = brandColors[brand] || "gray";
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
          <h1 className="text-2xl md:text-3xl font-bold">
            Mapa stacji benzynowych
          </h1>
            <a href="/dashboard"
            className="btn btn-active btn-info">
            ← Powrót do dashboardu
            </a>
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


      <Footer />
    </div>
  );
}
