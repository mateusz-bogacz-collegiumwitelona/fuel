import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router";
import Header from "../components/header";
import Footer from "../components/footer";
import "leaflet/dist/leaflet.css";
import { API_BASE } from "../components/api";
import { useTranslation } from "react-i18next";
// IMPORTUJEMY NOWY MODAL
import { ProposalModal } from "../components/proposal-modal";

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
  const { t } = useTranslation();
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

  // Stan dla modala
  const [isProposalOpen, setIsProposalOpen] = useState(false);

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
      (key) => key.toLowerCase() === brand.toLowerCase(),
    );
    const color = normalizedBrand
      ? brandColors[normalizedBrand]
      : brandColors.Default;

    return new L.Icon({
      iconUrl:
        "https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-" +
        color +
        ".png",
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
      setError(t("station.missing_url_data"));
      setLoading(false);
      return;
    }

    const fetchProfile = async () => {
      try {
        setLoading(true);
        setError(null);

        const qs = new URLSearchParams({
          brandName,
          street,
          houseNumber,
          city,
        } as any);

        const response = await fetch(
          `${API_BASE}/api/station/profile?${qs.toString()}`,
          {
            method: "GET",
            headers: {
              Accept: "application/json",
            },
            credentials: "include",
          },
        );

        if (!response.ok) {
          throw new Error(`${t("station.fetch_error_prefix")} ${response.status}`);
        }

        const data: StationProfile = await response.json();
        setStation(data);
      } catch (e: any) {
        console.error(t("station.fetch_error_prefix"), e);
        setError(e?.message ? `${t("station.fetch_error_prefix")} ${e.message}` : t("station.fetch_error_fallback"));
      } finally {
        setLoading(false);
      }
    };

    fetchProfile();
  }, [brandName, street, houseNumber, city, t]);

  const { MapContainer, TileLayer, Marker, Popup } = MapComponents;

  const buildGoogleMapsUrl = (st: StationProfile) => {
    const query = `${st.brandName} ${st.street} ${st.houseNumber}, ${st.city}`;
    return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(
      query,
    )}`;
  };

  const buildGoogleMapsDirectionsUrl = (lat: number, lng: number) => {
    return `https://www.google.com/maps/dir/?api=1&destination=${lat},${lng}`;
  };

  return (
    <div className="min-h-screen bg-base-200 text-base-content relative">
      <Header />

      <main className="mx-auto max-w-6xl px-4 py-5">
        <div className="mb-5 flex items-center justify-between gap-4">
          <h1 className="text-2xl md:text-3xl font-bold text-right">
            {t("station.title")}
          </h1>
          <button
            className="btn btn-outline btn-sm"
            onClick={() => navigate(-1)}
          >
            {t("station.back")}
          </button>
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
              <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
                <div>
                  <h2 className="text-2xl font-semibold mb-2">
                    {station.brandName}
                  </h2>
                  <p className="text-sm md:text-base">
                    {station.city}, {station.street} {station.houseNumber}
                  </p>
                </div>
                
                {/* NOWY PRZYCISK - ZGŁOŚ ZMIANĘ */}
                <button 
                    className="btn btn-primary shadow-lg"
                    onClick={() => setIsProposalOpen(true)}
                >
                    {t("proposal.form_title") || "Zgłoś aktualizację cen"}
                </button>
              </div>

              <div className="mt-3 flex gap-2 flex-wrap">
                <a
                  href={buildGoogleMapsUrl(station)}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="btn btn-sm btn-outline mt-3"
                >
                  {t("station.open_google_maps")}
                </a>

                <a
                  href={buildGoogleMapsDirectionsUrl(station.latitude, station.longitude)}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="btn btn-sm btn-outline mt-3"
                >
                  {t("station.directions")}
                </a>
              </div>
            </section>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <section className="bg-base-300 rounded-xl p-4 shadow-md ">
                <h3 className="text-lg font-semibold mb-3">{t("station.map_title")}</h3>
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
                        attribution="© OpenStreetMap contributors"
                      />
                      <Marker
                        position={[station.latitude, station.longitude]}
                        icon={getMarkerIcon(station.brandName)}
                      >
                        <Popup>
                          <strong>{station.brandName}</strong>
                          <br />
                          {station.city}, {station.street} {" "}
                          {station.houseNumber}
                        </Popup>
                      </Marker>
                    </MapContainer>
                  ) : (
                    <div className="flex items-center justify-center h-full text-sm text-base-content/70">
                      {t("station.map_loading")}
                    </div>
                  )}
                </div>
              </section>

              <section className="bg-base-300 rounded-xl p-6 shadow-md">
                <h3 className="text-lg font-semibold mb-3">{t("station.fuel_prices_title")}</h3>

                {station.fuelPrice && station.fuelPrice.length > 0 ? (
                  <div className="overflow-x-auto">
                    <table className="table table-zebra w-full">
                      <thead>
                        <tr>
                          <th>{t("station.fuel_code")}</th>
                          <th>{t("station.price_pln")}</th>
                          <th>{t("station.valid_from")}</th>
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
                    {t("station.no_price_data")}
                  </p>
                )}
              </section>
            </div>
          </div>
        )}
      </main>

      <Footer />

      {/* RENDEROWANIE MODALA */}
      <ProposalModal 
        isOpen={isProposalOpen}
        onClose={() => setIsProposalOpen(false)}
        station={station}
      />

    </div>
  );
}