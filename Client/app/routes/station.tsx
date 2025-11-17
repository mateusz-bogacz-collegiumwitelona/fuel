import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import Header from "../components/header";
import Footer from "../components/footer";
import "leaflet/dist/leaflet.css";

type FuelPrice = {
  fuelCode: string;
  price: number;
  validFrom: string;
};

type StationProfile = {
  brandName: string;
  street: string;
  houseNumber: string;
  city: string;
  postalCode: string;
  latitude: number;
  longitude: number;
  fuelPrice: FuelPrice[];
};

export default function StationProfilePage() {
  const { brandName, city, street, houseNumber } = useParams<{
    brandName: string;
    city: string;
    street: string;
    houseNumber: string;
  }>();

  const navigate = useNavigate();

  const [station, setStation] = useState<StationProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);


  const [MapComponents, setMapComponents] = useState<{
    MapContainer?: any;
    TileLayer?: any;
    Marker?: any;
    Popup?: any;
  }>({});
  const [L, setL] = useState<any>(null);

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

  const getMarkerIcon = (brand: string) => {
    if (!L) return undefined;
    const normalizedBrand = Object.keys(brandColors).find(
      (key) => key.toLowerCase() === brand.toLowerCase()
    );
    const color = normalizedBrand
      ? brandColors[normalizedBrand]
      : brandColors.Default;

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

  useEffect(() => {
    if (!brandName || !street || !houseNumber || !city) {
      setError("Brak danych stacji w adresie URL.");
      setLoading(false);
      return;
    }

    const fetchProfile = async () => {
      try {
        setLoading(true);
        setError(null);

        const token = localStorage.getItem("token");

        const qs = new URLSearchParams({
          brandName,
          street,
          houseNumber,
          city,
        });

        const response = await fetch(
          `http://localhost:5111/api/station/profile?${qs.toString()}`,
          {
            method: "GET",
            headers: {
              Accept: "application/json",
              ...(token ? { Authorization: `Bearer ${token}` } : {}),
            },
            credentials: "include",
          }
        );

        if (!response.ok) {
          throw new Error(`Błąd serwera: ${response.status}`);
        }

        const data: StationProfile = await response.json();
        setStation(data);
      } catch (e: any) {
        console.error("Błąd pobierania profilu stacji:", e);
        setError(e.message ?? "Nie udało się pobrać danych stacji.");
      } finally {
        setLoading(false);
      }
    };

    fetchProfile();
  }, [brandName, street, houseNumber, city]);

  const { MapContainer, TileLayer, Marker, Popup } = MapComponents;

  return (
    <div className="min-h-screen bg-base-200 text-base-content">
      <Header />

      <main className="mx-auto max-w-6xl px-4 py-8">
        <div className="mb-6 flex items-center justify-between gap-4">
          <button
            className="btn btn-outline btn-sm"
            onClick={() => navigate(-1)}
          >
            ← Wróć
          </button>
          <h1 className="text-2xl md:text-3xl font-bold text-right">
            Szczegóły stacji
          </h1>
        </div>

        {loading && (
          <div className="flex justify-center py-10">
            <span className="loading loading-spinner loading-lg" />
          </div>
        )}

        {!loading && error && (
          <div className="alert alert-error shadow-lg">
            <span>{error}</span>
          </div>
        )}

        {!loading && !error && station && (
          <div className="space-y-6">
            <section className="bg-base-300 rounded-xl p-6 shadow-md">
              <h2 className="text-2xl font-semibold mb-2">
                {station.brandName}
              </h2>
              <p className="text-sm md:text-base">
                {station.city}, {station.street} {station.houseNumber}
              </p>

            </section>


            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <section className="bg-base-300 rounded-xl p-4 shadow-md ">
                <h3 className="text-lg font-semibold mb-3">Mapa stacji</h3>
                <div className="h-72 rounded-lg overflow-hidden bg-base-100">
                  {MapContainer && L ? (
                    <MapContainer
                      center={[station.latitude, station.longitude]}
                      zoom={15}
                      scrollWheelZoom={false}
                      style={{ height: "100%", width: "100%" }}
                    >
                      <TileLayer
                        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                        attribution="&copy; OpenStreetMap contributors"
                      />
                      <Marker
                        position={[station.latitude, station.longitude]}
                        icon={getMarkerIcon(station.brandName)}
                      >
                        <Popup>
                          <strong>{station.brandName}</strong>
                          <br />
                          {station.city}, {station.street}{" "}
                          {station.houseNumber}
                        </Popup>
                      </Marker>
                    </MapContainer>
                  ) : (
                    <div className="flex items-center justify-center h-full text-sm text-base-content/70">
                      Ładowanie mapy...
                    </div>
                  )}
                </div>
              </section>


              <section className="bg-base-300 rounded-xl p-6 shadow-md">
                <h3 className="text-lg font-semibold mb-3">Ceny paliw</h3>

                {station.fuelPrice && station.fuelPrice.length > 0 ? (
                  <div className="overflow-x-auto">
                    <table className="table table-zebra w-full">
                      <thead>
                        <tr>
                          <th>Kod paliwa</th>
                          <th>Cena [zł]</th>
                          <th>Ważne od</th>
                        </tr>
                      </thead>
                      <tbody>
                        {station.fuelPrice.map((fp, idx) => (
                          <tr key={idx}>
                            <td>{fp.fuelCode}</td>
                            <td>{fp.price.toFixed(2)}</td>
                            <td>
                              {fp.validFrom
                                ? new Date(fp.validFrom).toLocaleDateString()
                                : "-"}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <p className="text-sm text-base-content/70">
                    Brak danych o cenach paliw dla tej stacji.
                  </p>
                )}
              </section>
            </div>
          </div>
        )}
      </main>

      <Footer />
    </div>
  );
}
