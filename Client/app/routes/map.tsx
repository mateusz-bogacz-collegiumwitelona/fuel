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
  const [markerIcon, setMarkerIcon] = useState<any>(null);
  const [status, setStatus] = useState<
    "loading" | "error" | "unauthorized" | "ready"
  >("loading");

  useEffect(() => {
    if (typeof window === "undefined") return;

    (async () => {
      const [rl, L] = await Promise.all([
        import("react-leaflet"),
        import("leaflet"),
      ]);

      const icon = L.icon({
        iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
        shadowUrl:
          "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
        iconSize: [25, 41],
        iconAnchor: [12, 41],
        popupAnchor: [1, -34],
        shadowSize: [41, 41],
      });

      setMarkerIcon(icon);
      setMapComponents({
        MapContainer: rl.MapContainer,
        TileLayer: rl.TileLayer,
        Marker: rl.Marker,
        Popup: rl.Popup,
      });
    })();
  }, []);

  useEffect(() => {
    const fetchStations = async () => {
      try {
        const res = await fetch("http://localhost:5111/api/station/map/all", {
          credentials: "include",
        });

        // jeśli backend zwrócił redirect / login HTML
        const contentType = res.headers.get("content-type");
        if (res.status === 401 || res.status === 403) {
          setStatus("unauthorized");
          return;
        }
        if (contentType && !contentType.includes("application/json")) {
          setStatus("unauthorized");
          return;
        }
        if (!res.ok) throw new Error("Błąd połączenia z serwerem");

        const data = await res.json();
        setStations(data);
        setStatus("ready");
      } catch (err) {
        console.error("Błąd pobierania stacji:", err);
        setStatus("error");
      }
    };

    fetchStations();
  }, []);

  const { MapContainer, TileLayer, Marker, Popup } = MapComponents;
  if (!MapContainer) return <p style={{ textAlign: "center" }}>Ładowanie mapy...</p>;

  if (status === "loading")
    return <p style={{ textAlign: "center" }}>Ładowanie danych...</p>;

  if (status === "unauthorized")
    return (
      <div style={{ textAlign: "center", marginTop: "2rem" }}>
        <p style={{ color: "red", fontWeight: "bold" }}>
          Sesja wygasła lub brak autoryzacji.
        </p>
        <p>
          <a
            href="http://localhost:5111/Account/Login"
            style={{
              color: "blue",
              textDecoration: "underline",
              cursor: "pointer",
            }}
          >
            Zaloguj się ponownie
          </a>
          , aby zobaczyć stacje.
        </p>
      </div>
    );

  if (status === "error")
    return (
      <p style={{ color: "red", textAlign: "center", marginTop: "2rem" }}>
        Wystąpił błąd podczas pobierania danych z serwera.
      </p>
    );

  return (
    <div style={{ height: "100vh", width: "100vw" }}>
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
          <Marker key={i} position={[s.latitude, s.longitude]} icon={markerIcon}>
            <Popup>
              <strong>{s.brandName}</strong>
              <br />
              {s.address}
            </Popup>
          </Marker>
        ))}
      </MapContainer>
    </div>
  );
}
