import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";
import { API_BASE } from "../components/api";
import { useUserGuard } from "../components/useUserGuard";

type RequestItem = {
  id: string;
  createdAt: string;
  title: string;
  status: "pending" | "accepted" | "rejected" | string;
};

type Station = {
  id?: string;
  name: string;
  street: string;
  houseNumber: number;
  postalcode: string;
  latitude?: number;
  longitude?: number;
  city?: string;
  imageUrl?: string;
  address?: string;
  distanceMeters?: number;
  fuelPrices?: Record<string, number | string>;
};

export default function Dashboard() {
  const { state, email } = useUserGuard();

  const [requests, setRequests] = React.useState<RequestItem[] | null>(null);
  const [requestsLoading, setRequestsLoading] = React.useState(true);

  const [stations, setStations] = React.useState<Station[] | null>(null);
  const [stationsLoading, setStationsLoading] = React.useState(true);
  const [stationsError, setStationsError] = React.useState<string | null>(null);

  const [stats, setStats] = React.useState<any>(null);
  const [statsLoading, setStatsLoading] = React.useState(true);
  const [statsError, setStatsError] = React.useState<string | null>(null);

  // po potwierdzeniu, że user jest zalogowany, dopiero ładujemy dane
  React.useEffect(() => {
    if (state !== "allowed") return;

    (async () => {
      await Promise.all([
        fetchRequests(),
        fetchProposalStats(),
        fetchNearestStations(),
      ]);
    })();
  }, [state]);

  async function fetchRequests() {
    setRequestsLoading(true);
    try {
      const headers: Record<string, string> = { Accept: "application/json" };

      const res = await fetch(`${API_BASE}/api/user/requests`, {
        headers,
        credentials: "include",
      });
      if (!res.ok) throw new Error("fetch-error");
      const data = await res.json();
      setRequests(data);
    } catch (err) {
      console.warn(
        "Nie udało się pobrać zgłoszeń — używam danych przykładowych.",
        err,
      );
      setRequests([
        {
          id: "1",
          createdAt: new Date(
            Date.now() - 1000 * 60 * 60 * 24 * 2,
          ).toISOString(),
          title: "Propozycja: korekta ceny na stacji X",
          status: "pending",
        },
        {
          id: "2",
          createdAt: new Date(
            Date.now() - 1000 * 60 * 60 * 24 * 10,
          ).toISOString(),
          title: "Propozycja: dodanie nowej stacji Y",
          status: "accepted",
        },
      ]);
    } finally {
      setRequestsLoading(false);
    }
  }

  async function fetchNearestStations() {
    setStationsLoading(true);
    setStationsError(null);

    const tryFetchWithCoords = async (lat: number, lon: number) => {
      try {
        const headers: Record<string, string> = {
          "Content-Type": "application/json",
          Accept: "application/json",
        };

        let res = await fetch(`${API_BASE}/api/station/map/nearest`, {
          method: "POST",
          headers,
          body: JSON.stringify({ latitude: lat, longitude: lon }),
          credentials: "include",
        });

        if (!res.ok) {
          res = await fetch(
            `${API_BASE}/api/station/map/nearest?lat=${encodeURIComponent(
              lat,
            )}&lon=${encodeURIComponent(lon)}`,
            {
              headers,
              credentials: "include",
            },
          );
        }

        if (!res.ok)
          throw new Error(`stations-fetch-failed (${res.status})`);
        const data = await res.json();

        if (Array.isArray(data)) {
          setStations(data);
        } else if (data?.stations && Array.isArray(data.stations)) {
          setStations(data.stations);
        } else {
          console.warn("Nieoczekiwany format danych stacji:", data);
          setStations([]);
        }
      } catch (err) {
        console.error("Błąd pobierania stacji z coords:", err);
        throw err;
      }
    };

    if ("geolocation" in navigator) {
      navigator.geolocation.getCurrentPosition(
        async (pos) => {
          const lat = pos.coords.latitude;
          const lon = pos.coords.longitude;
          try {
            await tryFetchWithCoords(lat, lon);
          } catch (err) {
            setStationsError(
              "Nie udało się pobrać najbliższych stacji z backendu.",
            );
            setStations(null);
          } finally {
            setStationsLoading(false);
          }
        },
        async (err) => {
          console.warn("Geolocation zablokowana/nieudana:", err);
          try {
            const headers: Record<string, string> = {
              Accept: "application/json",
            };

            const res = await fetch(`${API_BASE}/api/station/map/nearest`, {
              headers,
              credentials: "include",
            });
            if (!res.ok) throw new Error("no-coords-fetch-failed");
            const data = await res.json();
            if (Array.isArray(data)) setStations(data);
            else if (data?.stations && Array.isArray(data.stations))
              setStations(data.stations);
            else setStations([]);
          } catch (err2) {
            console.error(
              "Fallback bez geolokacji nie powiódł się:",
              err2,
            );
            setStationsError(
              "Brak dostępu do lokalizacji i nie udało się pobrać stacji.",
            );
            setStations(null);
          } finally {
            setStationsLoading(false);
          }
        },
        { enableHighAccuracy: false, timeout: 10_000, maximumAge: 60_000 },
      );
    } else {
      try {
        const headers: Record<string, string> = {
          Accept: "application/json",
        };

        const res = await fetch(`${API_BASE}/api/station/map/nearest`, {
          headers,
          credentials: "include",
        });
        if (!res.ok) throw new Error("no-geolocation");
        const data = await res.json();
        if (Array.isArray(data)) setStations(data);
        else if (data?.stations && Array.isArray(data.stations))
          setStations(data.stations);
        else setStations([]);
      } catch (err) {
        console.error(
          "Brak geolokacji i pobranie stacji nie powiodło się:",
          err,
        );
        setStationsError(
          "Twoja przeglądarka nie wspiera lokalizacji i nie udało się pobrać stacji.",
        );
        setStations(null);
      } finally {
        setStationsLoading(false);
      }
    }
  }

  async function fetchProposalStats() {
    setStatsLoading(true);
    setStatsError(null);

    const tryEndpoints = [
      "/api/proposals/statistics",
      "/api/proposal-statistic",
      "/api/proposal-statistics",
      "/api/proposals/statistic",
    ];

    for (const ep of tryEndpoints) {
      try {
        const headers: Record<string, string> = {
          Accept: "application/json",
        };

        const res = await fetch(`${API_BASE}${ep}`, {
          headers,
          credentials: "include",
        });

        if (!res.ok) {
          if (res.status === 404) continue;
          throw new Error(`stats-fetch-error ${res.status}`);
        }

        const data = await res.json();

        if (data && typeof data === "object" && !Array.isArray(data)) {
          const normalized = {
            total:
              data.total ??
              data.totalProposals ??
              data.total_proposals ??
              data.count ??
              data.itemsCount ??
              0,
            accepted:
              data.approved ??
              data.approvedProposals ??
              data.approved_proposals ??
              data.accepted ??
              data.acceptedProposals ??
              0,
            rejected:
              data.rejected ??
              data.rejectedProposals ??
              data.rejected_proposals ??
              data.denied ??
              0,
            acceptedRate:
              data.acceptedRate ??
              data.acceptanceRate ??
              data.accepted_rate ??
              data.approvedRate ??
              data.acceptedRate ??
              null,
          };
          setStats(normalized);
          setStatsLoading(false);
          return;
        }

        if (Array.isArray(data)) {
          const total = data.length;
          const accepted = data.filter(
            (x: any) => x.status === "accepted",
          ).length;
          const rejected = data.filter(
            (x: any) => x.status === "rejected",
          ).length;
          const acceptedRate =
            total > 0 ? Math.round((accepted / total) * 100) : null;
          setStats({ total, accepted, rejected, acceptedRate });
          setStatsLoading(false);
          return;
        }

        console.warn("Nieoczekiwany format statystyk:", data);
        setStats(null);
        setStatsLoading(false);
        return;
      } catch (err) {
        console.warn(`Błąd pobierania statystyk z ${ep}:`, err);
      }
    }

    setStatsError("Nie udało się pobrać statystyk z serwera.");
    setStatsLoading(false);
  }

  const handleLogout = () => {
    localStorage.removeItem("token");
    localStorage.removeItem("token_expiration");
    if (typeof window !== "undefined") window.location.href = "/login";
  };

  const formatDate = (iso?: string) =>
    iso ? new Date(iso).toLocaleString() : "-";

  const formatDistance = (m?: string | number) => {
    if (m == null) return "-";
    const mm = Number(m);
    if (Number.isNaN(mm)) return "-";
    if (mm >= 1000) return `${(mm / 1000).toFixed(2)} km`;
    return `${Math.round(mm)} m`;
  };

    return (
        <div className="min-h-screen bg-base-200 text-base-content">
            <Header />

            <main className="mx-auto max-w-350 px-1 py-8">
                <h1 className="text-2xl md:text-3xl font-bold mb-4">Witaj, jesteś zalogowany!</h1>
                {email && <p className="mb-4 text-sm text-gray-400">Zalogowany jako: {email}</p>}

                <section className="mb-8">
                    <div className="carousel w-full">
                        <div id="slide1" className="carousel-item relative w-full">
                            <img src="images/stacjaOrlen.png" className="w-full" alt="stacja Orlen" />
                            <div className="absolute left-5 right-5 top-1/2 flex -translate-y-1/2 transform justify-between">
                                <a href="#slide4" className="btn btn-circle">❮</a>
                                <a href="#slide2" className="btn btn-circle">❯</a>
                            </div>
                        </div>
                        <div id="slide2" className="carousel-item relative w-full">
                            <img src="images/stacjaMoya.png" className="w-full" alt="stacja Moya" />
                            <div className="absolute left-5 right-5 top-1/2 flex -translate-y-1/2 transform justify-between">
                                <a href="#slide1" className="btn btn-circle">❮</a>
                                <a href="#slide3" className="btn btn-circle">❯</a>
                            </div>
                        </div>
                        <div id="slide3" className="carousel-item relative w-full">
                            <img src="images/stacjaBp.png" className="w-full" alt="stacja BP" />
                            <div className="absolute left-5 right-5 top-1/2 flex -translate-y-1/2 transform justify-between">
                                <a href="#slide2" className="btn btn-circle">❮</a>
                                <a href="#slide4" className="btn btn-circle">❯</a>
                            </div>
                        </div>
                    </div>
                </section>

                <section className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
                    <a href="/map" className="block rounded-xl overflow-hidden shadow-lg">
                        <div className="relative h-56 md:h-72 bg-gray-700 flex items-center justify-center">
                            <img src="/images/map-preview.png" alt="map" className="object-cover w-full h-full" />
                            <div className="absolute inset-0 bg-black/30"></div>
                            <div className="absolute z-10 text-2xl font-bold">Mapa stacji</div>
                        </div>
                    </a>

                    <a href="/list" className="block rounded-xl overflow-hidden shadow-lg">
                        <div className="relative h-56 md:h-72 bg-gray-700 flex items-center justify-center">
                            <img src="/images/list-preview.png" alt="list" className="object-cover w-full h-full" />
                            <div className="absolute inset-0 bg-black/30"></div>
                            <div className="absolute z-10 text-2xl font-bold">Lista stacji</div>
                        </div>
                    </a>
                </section>

                <section className="bg-base-300 p-6 rounded-xl shadow-md mb-8">
                    <h2 className="text-xl font-semibold mb-10">Najbliższe stacje</h2>

                    {stationsLoading ? (
                        <div>Ładowanie najbliższych stacji... (upewnij się, że zezwoliłeś na dostęp do lokalizacji)</div>
                    ) : stationsError ? (
                        <div className="text-red-400">{stationsError}</div>
                    ) : stations && stations.length > 0 ? (
                        <div className="grid md:grid-cols-3 gap-30">
                            {stations.map((s, idx) => (
                                <div
                                    key={s.id ?? `${s.name}-${idx}`}
                                    className="card bg-base-100 w-96 shadow-sm"
                                >
                                    <div className="card-body">
                                        <h2 className="card-title">{s.name}</h2>

                                        {s.name && (
                                            <p className="text-sm text-gray-200">{`nazwa stacji = ${s.name}`}</p>
                                        )}

                                        <p className="text-sm text-gray-600">
                                            {`ulica = ${s.street ?? "-"}`}{s.houseNumber !== undefined && s.houseNumber !== null ? ` ${s.houseNumber}` : ""}
                                        </p>

                                        {s.city && (
                                            <p className="text-sm text-gray-600">{`miasto = ${s.city}`}</p>
                                        )}

                                        <p className="text-sm text-gray-600">{`kod pocztowy = ${s.postalcode ?? "-"}`}</p>

                                        {s.distanceMeters !== undefined && s.distanceMeters !== null && (
                                            <p className="text-sm text-gray-500">Odległość: {formatDistance(s.distanceMeters)}</p>
                                        )}

                                        <div className="card-actions justify-mid mt-2">
                                            <a
                                                href={`/map?lat=${s.latitude ?? ""}&lon=${s.longitude ?? ""}`}
                                                className="btn btn-outline"
                                            >
                                                Pokaż na mapie
                                            </a>
                                            <a
                                                href={`/list#${encodeURIComponent(s.id ?? s.name ?? String(idx))}`}
                                                className="btn btn-outline btn-primary"
                                            >
                                                Szczegóły stacji
                                            </a>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    ) : (
                        <div className="text-base-content">Brak dostępnych stacji.</div>
                    )}
                </section>

                <section className="bg-base-300 p-6 rounded-xl shadow-md">
                    <h2 className="text-xl font-semibold mb-4">Twoje statystyki zgłoszeń</h2>

                    {statsLoading ? (
                        <div>Ładowanie statystyk...</div>
                    ) : statsError ? (
                        <div className="text-error">{statsError}</div>
                    ) : stats ? (
                        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                            <div className="p-4 bg-base-100 rounded text-center">
                                <div className="text-3xl text-base-content font-bold">{stats.total ?? requests?.length ?? 0}</div>
                                <div className="text-sm text-base-content mt-1">Wszystkie zgłoszenia</div>
                            </div>
                            <div className="p-4 bg-base-100 rounded text-center">
                                <div className="text-3xl text-success font-bold">{stats.accepted ?? 0}</div>
                                <div className="text-sm text-success mt-1">Zaakceptowane</div>
                            </div>
                            <div className="p-4 bg-base-100 rounded text-error text-center">
                                <div className="text-3xl font-bold">{stats.rejected ?? 0}</div>
                                <div className="text-sm mt-1">Odrzucone</div>
                            </div>
                            <div className="p-4 bg-base-100 rounded text-info text-center">
                                <div className="text-3xl font-bold">{stats.acceptedRate != null ? `${stats.acceptedRate}%` : "-"}</div>
                                <div className="text-sm mt-1">Wskaźnik akceptacji</div>
                            </div>
                        </div>
                    ) : (
                        <div className="text-gray-300">Brak statystyk.</div>
                    )}

                    <div className="mt-6">
                        <h3 className="font-semibold mb-3">Twoje zgłoszenia (lista)</h3>
                        {requestsLoading ? (
                            <div>Ładowanie...</div>
                        ) : requests && requests.length > 0 ? (
                            <div className="flex flex-col gap-3">
                                {requests.map((r) => (
                                    <div key={r.id} className="p-4 bg-base-100 rounded flex items-center justify-between">
                                        <div>
                                            <div className="font-medium">{r.title}</div>
                                            <div className="text-sm text-base-content">{formatDate(r.createdAt)}</div>
                                        </div>

                                        <div className="text-sm">
                                            <span
                                                className={`badge ${r.status === "accepted" ? "badge-success" : r.status === "rejected" ? "badge-error" : "badge-ghost"
                                                    }`}
                                            >
                                                {r.status}
                                            </span>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        ) : (
                            <div className="text-base-content">Nie masz jeszcze zgłoszeń.</div>
                        )}
                    </div>
                </section>
            </main>

            <Footer />
        </div>
    );
}
