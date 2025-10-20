import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";

// ====================
// Dashboard z zapytaniami do backendu:
// - /api/station/map/nearest   (pobranie najbliższych stacji)
// - /api/proposal-statistic    (pobranie statystyk zgłoszeń) - fallback: /api/proposal-statistics
// Bazowy URL - dopasowany do login.tsx (localhost:5111)
// ====================

const API_BASE = "http://localhost:5111";

function parseJwt(token: string | null) {
    if (!token) return null;
    try {
        return JSON.parse(atob(token.split(".")[1]));
    } catch (e) {
        return null;
    }
}

type RequestItem = {
    id: string;
    createdAt: string;
    title: string;
    status: "pending" | "accepted" | "rejected" | string;
};

type Station = {
    id?: string;
    name: string;
    latitude?: number;
    longitude?: number;
    distanceMeters?: number; // jeśli backend zwraca dystans
    address?: string;
    fuelPrices?: Record<string, number> | null;
};

export default function Dashboard() {
    const [email, setEmail] = React.useState<string | null>(null);

    // zgłoszenia (tak jak wcześniej)
    const [requests, setRequests] = React.useState<RequestItem[] | null>(null);
    const [requestsLoading, setRequestsLoading] = React.useState(true);

    // najbliższe stacje
    const [stations, setStations] = React.useState<Station[] | null>(null);
    const [stationsLoading, setStationsLoading] = React.useState(true);
    const [stationsError, setStationsError] = React.useState<string | null>(null);

    // statystyki / metrics
    const [stats, setStats] = React.useState<any>(null);
    const [statsLoading, setStatsLoading] = React.useState(true);
    const [statsError, setStatsError] = React.useState<string | null>(null);

    React.useEffect(() => {
        const token = localStorage.getItem("token");
        const expiration = localStorage.getItem("token_expiration");

        // Jeśli brak tokena lub wygasł -> przekieruj do logowania
        if (!token || !expiration || new Date(expiration) <= new Date()) {
            if (typeof window !== "undefined") window.location.href = "/login";
            return;
        }

        // dekodowanie emailu z tokena, jak wcześniej
        const decoded = parseJwt(token);
        const userEmail = decoded?.email || decoded?.sub || null;
        setEmail(userEmail ?? "Zalogowany użytkownik");

        // równoległe pobranie: zgłoszeń, statystyk i stacji
        fetchRequests(token);
        fetchProposalStats(token);
        fetchNearestStations(token);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    // ---------------------------
    // Fetch - zgłoszenia (istniejąca logika)
    // ---------------------------
    async function fetchRequests(token: string) {
        setRequestsLoading(true);
        try {
            const res = await fetch(`${API_BASE}/api/user/requests`, {
                headers: {
                    Authorization: `Bearer ${token}`,
                    Accept: "application/json",
                },
            });
            if (!res.ok) throw new Error("fetch-error");
            const data = await res.json();
            setRequests(data);
        } catch (err) {
            console.warn("Nie udało się pobrać zgłoszeń — używam danych przykładowych.", err);
            setRequests([
                {
                    id: "1",
                    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 2).toISOString(),
                    title: "Propozycja: korekta ceny na stacji X",
                    status: "pending",
                },
                {
                    id: "2",
                    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 10).toISOString(),
                    title: "Propozycja: dodanie nowej stacji Y",
                    status: "accepted",
                },
            ]);
        } finally {
            setRequestsLoading(false);
        }
    }

    // ---------------------------
    // Fetch - najbliższe stacje
    // Strategia:
    // 1) Pobieramy geolokalizację przez browser (navigator.geolocation)
    // 2) Robimy POST z body { latitude, longitude } do /api/station/map/nearest
    // 3) Jeśli POST nie działa (405/404/422) robimy GET z query params
    // 4) Jeśli geolokacja jest zablokowana -> fallback: pobranie "ogólnych" stacji (bez coords) lub komunikat
    // ---------------------------
    async function fetchNearestStations(token: string) {
        setStationsLoading(true);
        setStationsError(null);

        const tryFetchWithCoords = async (lat: number, lon: number) => {
            try {
                // 1) próbujemy POST (wielu backendów przyjmuje POST z ciałem)
                let res = await fetch(`${API_BASE}/api/station/map/nearest`, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        Authorization: `Bearer ${token}`,
                        Accept: "application/json",
                    },
                    body: JSON.stringify({ latitude: lat, longitude: lon }),
                });

                // jeśli POST zwróci 404/405 lub inny błąd - spróbuj GET z query
                if (!res.ok) {
                    // fallback: GET (niektóre API oczekują query params)
                    res = await fetch(
                        `${API_BASE}/api/station/map/nearest?lat=${encodeURIComponent(
                            lat
                        )}&lon=${encodeURIComponent(lon)}`,
                        {
                            headers: {
                                Authorization: `Bearer ${token}`,
                                Accept: "application/json",
                            },
                        }
                    );
                }

                if (!res.ok) throw new Error(`stations-fetch-failed (${res.status})`);
                const data = await res.json();

                // oczekujemy, że data to tablica obiektów stacji
                if (Array.isArray(data)) {
                    setStations(data);
                } else if (data?.stations && Array.isArray(data.stations)) {
                    setStations(data.stations);
                } else {
                    // jeśli format inny - opakuj w jedną tablicę lub ustaw błąd
                    console.warn("Nieoczekiwany format danych stacji:", data);
                    setStations([]);
                }
            } catch (err) {
                console.error("Błąd pobierania stacji z coords:", err);
                throw err;
            }
        };

        // jeśli przeglądarka wspiera geolokację - poproś użytkownika
        if ("geolocation" in navigator) {
            navigator.geolocation.getCurrentPosition(
                async (pos) => {
                    const lat = pos.coords.latitude;
                    const lon = pos.coords.longitude;
                    try {
                        await tryFetchWithCoords(lat, lon);
                    } catch (err) {
                        setStationsError("Nie udało się pobrać najbliższych stacji z backendu.");
                        setStations(null);
                    } finally {
                        setStationsLoading(false);
                    }
                },
                async (err) => {
                    console.warn("Geolocation zablokowana/nieudana:", err);
                    // fallback: spróbuj pobrać bez coords (niektóre API zwracają 'popularne' stacje)
                    try {
                        const res = await fetch(`${API_BASE}/api/station/map/nearest`, {
                            headers: {
                                Authorization: `Bearer ${token}`,
                                Accept: "application/json",
                            },
                        });
                        if (!res.ok) throw new Error("no-coords-fetch-failed");
                        const data = await res.json();
                        if (Array.isArray(data)) setStations(data);
                        else if (data?.stations && Array.isArray(data.stations)) setStations(data.stations);
                        else setStations([]);
                    } catch (err2) {
                        console.error("Fallback bez geolokacji nie powiódł się:", err2);
                        setStationsError("Brak dostępu do lokalizacji i nie udało się pobrać stacji.");
                        setStations(null);
                    } finally {
                        setStationsLoading(false);
                    }
                },
                { enableHighAccuracy: false, timeout: 10_000, maximumAge: 60_000 }
            );
        } else {
            // geolokacja nieobsługiwana — spróbuj bez coords
            try {
                const res = await fetch(`${API_BASE}/api/station/map/nearest`, {
                    headers: {
                        Authorization: `Bearer ${token}`,
                        Accept: "application/json",
                    },
                });
                if (!res.ok) throw new Error("no-geolocation");
                const data = await res.json();
                if (Array.isArray(data)) setStations(data);
                else if (data?.stations && Array.isArray(data.stations)) setStations(data.stations);
                else setStations([]);
            } catch (err) {
                console.error("Brak geolokacji i pobranie stacji nie powiodło się:", err);
                setStationsError("Twoja przeglądarka nie wspiera lokalizacji i nie udało się pobrać stacji.");
                setStations(null);
            } finally {
                setStationsLoading(false);
            }
        }
    }

    // ---------------------------
    // Fetch - statystyki zgłoszeń
    // Próba: GET /api/proposal-statistic
    // Fallback: GET /api/proposal-statistics
    // Jeśli serwer zwróci tablicę propozycji, policzymy statystyki lokalnie
    // ---------------------------
    async function fetchProposalStats(token: string) {
        setStatsLoading(true);
        setStatsError(null);

        const tryEndpoints = ["/api/proposal-statistic", "/api/proposal-statistics"];

        for (const ep of tryEndpoints) {
            try {
                const res = await fetch(`${API_BASE}${ep}`, {
                    headers: {
                        Authorization: `Bearer ${token}`,
                        Accept: "application/json",
                    },
                });

                if (!res.ok) {
                    // jeśli 404 spróbuj kolejny endpoint
                    if (res.status === 404) continue;
                    throw new Error(`stats-fetch-error ${res.status}`);
                }

                const data = await res.json();

                // Jeżeli backend już zwraca gotowe metrics (np. { total: 10, accepted: 3, pending: 7 })
                if (data && typeof data === "object" && !Array.isArray(data)) {
                    setStats(data);
                    setStatsLoading(false);
                    return;
                }

                // Jeżeli backend zwraca tablicę zgłoszeń - policz statystyki
                if (Array.isArray(data)) {
                    const total = data.length;
                    const accepted = data.filter((x: any) => x.status === "accepted").length;
                    const pending = data.filter((x: any) => x.status === "pending").length;
                    const rejected = data.filter((x: any) => x.status === "rejected").length;
                    setStats({ total, accepted, pending, rejected });
                    setStatsLoading(false);
                    return;
                }

                // inne formaty
                console.warn("Nieoczekiwany format statystyk:", data);
                setStats(null);
                setStatsLoading(false);
                return;
            } catch (err) {
                console.warn(`Błąd pobierania statystyk z ${ep}:`, err);
                // spróbuj kolejny endpoint
            }
        }

        // jeśli żaden endpoint nie zadziałał:
        setStatsError("Nie udało się pobrać statystyk z serwera.");
        setStatsLoading(false);
    }

    const handleLogout = () => {
        localStorage.removeItem("token");
        localStorage.removeItem("token_expiration");
        if (typeof window !== "undefined") window.location.href = "/login";
    };

    const formatDate = (iso?: string) => (iso ? new Date(iso).toLocaleString() : "-");

    // Helper do pokazywania dystansu w czytelnym formacie
    const formatDistance = (m?: number) => {
        if (m == null) return "-";
        if (m >= 1000) return `${(m / 1000).toFixed(2)} km`;
        return `${Math.round(m)} m`;
    };

    return (
       <div className="min-h-screen bg-gray-900 text-white">
    <Header />

            <main className="mx-auto max-w-6xl px-4 py-8">
                <h1 className="text-2xl md:text-3xl font-bold mb-4">Witaj, jesteś zalogowany!</h1>

                {/* --- Karuzela (jak wcześniej) --- */}
                <section className="mb-8">
                    <div className="carousel w-full rounded-lg shadow-lg overflow-hidden">
                        <div id="slide1" className="carousel-item relative w-full">
                            <img src="/images/stacjaBp.png" alt="slide1" className="w-full" />
                            <div className="absolute left-4 bottom-4 bg-black/50 p-2 rounded">BP</div>
                        </div>
                        <div id="slide2" className="carousel-item relative w-full">
                            <img src="/images/stacjaMoya.png" alt="slide2" className="w-full" />
                            <div className="absolute left-4 bottom-4 bg-black/50 p-2 rounded">Moya</div>
                        </div>
                        <div id="slide3" className="carousel-item relative w-full">
                            <img src="/images/stacjaOrlen.png" alt="slide3" className="w-full" />
                            <div className="absolute left-4 bottom-4 bg-black/50 p-2 rounded">Orlen</div>
                        </div>
                    </div>
                </section>

                {/* --- Kafle Map / List --- */}
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

                {/* --- Najbliższe stacje --- */}
                <section className="bg-gray-800 p-6 rounded-xl shadow-md mb-8">
                    <h2 className="text-xl font-semibold mb-4">Najbliższe stacje</h2>

                    {stationsLoading ? (
                        <div>Ładowanie najbliższych stacji... (upewnij się, że zezwoliłeś na dostęp do lokalizacji)</div>
                    ) : stationsError ? (
                        <div className="text-red-400">{stationsError}</div>
                    ) : stations && stations.length > 0 ? (
                        <div className="grid md:grid-cols-2 gap-4">
                            {stations.map((s, idx) => (
                                <div key={s.id ?? `${s.name}-${idx}`} className="p-4 bg-gray-700 rounded flex items-center gap-4">
                                    <div className="flex-1">
                                        <div className="font-medium text-lg">{s.name}</div>
                                        {s.address && <div className="text-sm text-gray-300">{s.address}</div>}
                                        <div className="text-sm text-gray-400 mt-1">Odległość: {formatDistance(s.distanceMeters)}</div>
                                        {s.fuelPrices && (
                                            <div className="text-sm text-gray-300 mt-2">
                                                Ceny:{" "}
                                                {Object.entries(s.fuelPrices)
                                                    .map(([t, p]) => `${t}: ${p}`)
                                                    .join(" • ")}
                                            </div>
                                        )}
                                    </div>
                                    <div className="flex flex-col gap-2">
                                        {/* link do mapy z query params (możesz zamienić na Link) */}
                                        <a
                                            href={`/map?lat=${s.latitude ?? ""}&lon=${s.longitude ?? ""}`}
                                            className="btn btn-sm"
                                        >
                                            Pokaż na mapie
                                        </a>
                                        <a href={`/list#${s.id ?? ""}`} className="btn btn-ghost btn-sm">
                                            Szczegóły
                                        </a>
                                    </div>
                                </div>
                            ))}
                        </div>
                    ) : (
                        <div className="text-gray-300">Brak dostępnych stacji.</div>
                    )}
                </section>

                {/* --- Statystyki zgłoszeń użytkownika --- */}
                <section className="bg-gray-800 p-6 rounded-xl shadow-md">
                    <h2 className="text-xl font-semibold mb-4">Twoje statystyki zgłoszeń</h2>

                    {statsLoading ? (
                        <div>Ładowanie statystyk...</div>
                    ) : statsError ? (
                        <div className="text-red-400">{statsError}</div>
                    ) : stats ? (
                        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                            <div className="p-4 bg-gray-700 rounded text-center">
                                <div className="text-3xl font-bold">{stats.total ?? requests?.length ?? 0}</div>
                                <div className="text-sm text-gray-300 mt-1">Wszystkie zgłoszenia</div>
                            </div>
                            <div className="p-4 bg-gray-700 rounded text-center">
                                <div className="text-3xl font-bold">{stats.accepted ?? 0}</div>
                                <div className="text-sm text-gray-300 mt-1">Zaakceptowane</div>
                            </div>
                            <div className="p-4 bg-gray-700 rounded text-center">
                                <div className="text-3xl font-bold">{stats.pending ?? 0}</div>
                                <div className="text-sm text-gray-300 mt-1">W oczekiwaniu</div>
                            </div>
                            <div className="p-4 bg-gray-700 rounded text-center">
                                <div className="text-3xl font-bold">{stats.rejected ?? 0}</div>
                                <div className="text-sm text-gray-300 mt-1">Odrzucone</div>
                            </div>
                        </div>
                    ) : (
                        <div className="text-gray-300">Brak statystyk.</div>
                    )}

                    {/* opcjonalnie pokaż listę zgłoszeń (jak wcześniej) */}
                    <div className="mt-6">
                        <h3 className="font-semibold mb-3">Twoje zgłoszenia (lista)</h3>
                        {requestsLoading ? (
                            <div>Ładowanie...</div>
                        ) : requests && requests.length > 0 ? (
                            <div className="flex flex-col gap-3">
                                {requests.map((r) => (
                                    <div key={r.id} className="p-4 bg-gray-700 rounded flex items-center justify-between">
                                        <div>
                                            <div className="font-medium">{r.title}</div>
                                            <div className="text-sm text-gray-300">{formatDate(r.createdAt)}</div>
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
                            <div className="text-gray-300">Nie masz jeszcze zgłoszeń.</div>
                        )}
                    </div>
                </section>
            </main>

            <Footer />
  </div>
    );
}
