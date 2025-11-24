import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";

const API_BASE = "http://localhost:5111";

function parseJwt(token: string | null) {
    if (!token) return null;
    try {
        const payload = token.split(".")[1];
        const decoded = atob(payload);
        try {
            return JSON.parse(decodeURIComponent(escape(decoded)));
        } catch {
            return JSON.parse(decoded);
        }
    } catch (e) {
        return null;
    }
}

type Station = {
    id?: string;
    brandName?: string;
    name?: string;
    street?: string;
    houseNumber?: string | number;
    postalCode?: string;
    latitude?: number;
    longitude?: number;
    city?: string;
    imageUrl?: string;
    address?: string;
    distanceMeters?: number;
    fuelPrices?: Record<string, number | string> | null;
    pricePb95?: number | null;
    priceDiesel?: number | null;
    priceLpg?: number | null;
};

type SortColumn = "distance" | "price" | null;

export default function ListPage() {
    const [email, setEmail] = React.useState<string | null>(null);
    const [stations, setStations] = React.useState<Station[] | null>(null);
    const [loading, setLoading] = React.useState(true);
    const [error, setError] = React.useState<string | null>(null);

    // Filters
    const [selectedFuelTypes, setSelectedFuelTypes] = React.useState<string[]>([]);
    const [minPrice, setMinPrice] = React.useState<string>("");
    const [maxPrice, setMaxPrice] = React.useState<string>("");
    const [brandName, setBrandName] = React.useState<string>("");
    const [distance, setDistance] = React.useState<string>("");

    // Sorting
    const [sortColumn, setSortColumn] = React.useState<SortColumn>(null);
    const [sortDirection, setSortDirection] = React.useState<"asc" | "desc">("asc");

    // Location
    const [userCoords, setUserCoords] = React.useState<{ lat: number; lon: number } | null>(null);

    // Pagination
    const [pageNumber, setPageNumber] = React.useState<number>(1);
    const [pageSize, setPageSize] = React.useState<number>(10);
    const [totalPages, setTotalPages] = React.useState<number>(1);
    const [totalCount, setTotalCount] = React.useState<number | null>(null);

    const fuelTypes = ["PB95", "PB98", "ON", "LPG", "CNG"];

    // Auth check on mount
    React.useEffect(() => {
        (async () => {
            const token = localStorage.getItem("token");
            const expiration = localStorage.getItem("token_expiration");

            if (token && expiration && new Date(expiration) > new Date()) {
                const decoded = parseJwt(token);
                const userEmail = decoded?.email || decoded?.sub || null;
                setEmail(userEmail ?? "Zalogowany użytkownik");
                return;
            }

            // Try refresh
            try {
                const refreshRes = await fetch(`${API_BASE}/api/refresh`, {
                    method: "POST",
                    headers: { Accept: "application/json" },
                    credentials: "include",
                });

                if (refreshRes.ok) {
                    setEmail("Zalogowany użytkownik");
                } else {
                    if (typeof window !== "undefined") window.location.href = "/login";
                }
            } catch (err) {
                console.error("Błąd podczas /api/refresh:", err);
                if (typeof window !== "undefined") window.location.href = "/login";
            }
        })();
    }, []);

    // Fetch stations when filters or pagination changes
    React.useEffect(() => {
        if (email) {
            fetchStations();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [email, pageNumber, pageSize, sortColumn, sortDirection]);

    async function fetchStations() {
        setLoading(true);
        setError(null);

        const token = localStorage.getItem("token");

        // Build request body according to your API
        const requestBody = {
            LocationLatitude: userCoords?.lat ?? null,
            LocationLongitude: userCoords?.lon ?? null,
            Distance: distance ? parseFloat(distance) : null,
            FuelType: selectedFuelTypes.length > 0 ? selectedFuelTypes : null,
            MinPrice: minPrice ? parseFloat(minPrice) : null,
            MaxPrice: maxPrice ? parseFloat(maxPrice) : null,
            BrandName: brandName || null,
            SortingByDisance: sortColumn === "distance" ? true : null,
            SortingByPrice: sortColumn === "price" ? true : null,
            SortingDirection: sortColumn ? sortDirection : null, // tylko jeśli sortujemy
            Pagging: {
                PageNumber: pageNumber,
                PageSize: pageSize,
            },
        };

        try {
            const headers: Record<string, string> = {
                "Content-Type": "application/json",
                Accept: "application/json",
            };
            if (token) headers["Authorization"] = `Bearer ${token}`;

            console.log("Fetching stations with body:", requestBody);

            const res = await fetch(`${API_BASE}/api/station/list`, {
                method: "POST",
                headers,
                credentials: "include",
                body: JSON.stringify(requestBody),
            });

            if (!res.ok) {
                const errorText = await res.text();
                console.error("API Error:", res.status, errorText);

                let errorMessage = `Błąd ${res.status}: `;
                try {
                    const errorJson = JSON.parse(errorText);
                    errorMessage += errorJson.message || errorJson.errors?.join(", ") || "Nieznany błąd";
                } catch {
                    errorMessage += errorText || "Nieznany błąd";
                }

                setError(errorMessage);
                setStations([]);
                return;
            }

            const data = await res.json();
            console.log("API Response:", data);

            const items = Array.isArray(data.items) ? data.items : [];

            setStations(normalizeStations(items));
            setTotalCount(data.totalCount ?? items.length);
            setTotalPages(data.totalPages ?? 1);
            setPageNumber(data.pageNumber ?? 1);

        } catch (err: any) {
            console.error("Błąd pobierania stacji:", err);
            setError("Nie udało się pobrać listy stacji. Sprawdź konsolę.");
            setStations([]);
        } finally {
            setLoading(false);
        }
    }

    function normalizeStations(data: any[]): Station[] {
        return data.map((s: any) => {
            let fuelPrices: Record<string, number> | null = null;

            if (Array.isArray(s.fuelPrice)) {
                const map: Record<string, number> = {};
                for (const item of s.fuelPrice) {
                    const code = item.fuelCode ?? "";
                    const price = typeof item.price === "string"
                        ? parseFloat(item.price.replace(",", "."))
                        : Number(item.price);
                    if (code && !Number.isNaN(price)) {
                        map[code.toUpperCase()] = price;
                    }
                }
                if (Object.keys(map).length > 0) fuelPrices = map;
            }

            return {
                id: s.id ?? undefined,
                brandName: s.brandName ?? undefined,
                name: s.brandName ?? s.name ?? undefined,
                street: s.street ?? undefined,
                houseNumber: s.houseNumber ?? undefined,
                postalCode: s.postalCode ?? undefined,
                latitude: s.latitude ?? undefined,
                longitude: s.longitude ?? undefined,
                city: s.city ?? undefined,
                imageUrl: s.imageUrl ?? undefined,
                fuelPrices: fuelPrices,
                pricePb95: fuelPrices?.["PB95"] ?? null,
                priceDiesel: fuelPrices?.["ON"] ?? null,
                priceLpg: fuelPrices?.["LPG"] ?? null,
            };
        });
    }

    function toggleFuelType(type: string) {
        setSelectedFuelTypes(prev =>
            prev.includes(type) ? prev.filter(t => t !== type) : [...prev, type]
        );
    }

    function handleSearch() {
        setPageNumber(1);
        fetchStations();
    }

    function getUserLocation() {
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(
                (position) => {
                    setUserCoords({
                        lat: position.coords.latitude,
                        lon: position.coords.longitude
                    });
                },
                (error) => {
                    setError("Nie udało się pobrać lokalizacji: " + error.message);
                }
            );
        } else {
            setError("Geolokalizacja nie jest wspierana przez twoją przeglądarkę");
        }
    }

    function formatPriceValue(val: number | null | undefined) {
        if (val == null || Number.isNaN(val)) return "-";
        return `${Number(val).toFixed(2)} zł`;
    }

    function goToPage(p: number) {
        if (p < 1) p = 1;
        if (p > totalPages) p = totalPages;
        setPageNumber(p);
        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    function renderPageButtons() {
        const pages: number[] = [];
        const windowSize = 5;
        let start = Math.max(1, pageNumber - Math.floor(windowSize / 2));
        let end = start + windowSize - 1;
        if (end > totalPages) {
            end = totalPages;
            start = Math.max(1, end - windowSize + 1);
        }
        for (let i = start; i <= end; i++) pages.push(i);

        return (
            <div className="flex items-center gap-2">
                <button
                    className="btn btn-sm"
                    onClick={() => goToPage(1)}
                    disabled={pageNumber === 1}
                >
                    «1
                </button>
                <button
                    className="btn btn-sm"
                    onClick={() => goToPage(pageNumber - 1)}
                    disabled={pageNumber === 1}
                >
                    ←
                </button>

                {pages.map((p) => (
                    <button
                        key={p}
                        className={`btn btn-sm ${p === pageNumber ? "btn-active" : ""}`}
                        onClick={() => goToPage(p)}
                    >
                        {p}
                    </button>
                ))}

                <button
                    className="btn btn-sm"
                    onClick={() => goToPage(pageNumber + 1)}
                    disabled={pageNumber === totalPages}
                >
                    →
                </button>
                <button
                    className="btn btn-sm"
                    onClick={() => goToPage(totalPages)}
                    disabled={pageNumber === totalPages}
                >
                    {totalPages} »
                </button>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-base-200 text-base-content">
            <Header />

            <main className="mx-auto max-w-7xl px-4 py-8">
                <div className="flex items-center justify-between mb-6">
                    <h1 className="text-2xl md:text-3xl font-bold">Lista stacji benzynowych</h1>
                    <a href="/dashboard" className="btn btn-outline">
                        ← Powrót do dashboardu
                    </a>
                </div>

                {/* Filters */}
                <section className="bg-base-300 p-6 rounded-xl shadow-md mb-6">
                    <h2 className="text-xl font-semibold mb-4">Filtry</h2>

                    {/* Location */}
                    <div className="mb-4">
                        <label className="block text-sm font-medium mb-2">Lokalizacja</label>
                        <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
                            <input
                                type="number"
                                placeholder="Szerokość"
                                value={userCoords?.lat ?? ""}
                                onChange={(e) => setUserCoords(prev => ({
                                    lat: parseFloat(e.target.value),
                                    lon: prev?.lon ?? 0
                                }))}
                                className="input input-bordered input-sm"
                            />
                            <input
                                type="number"
                                placeholder="Długość"
                                value={userCoords?.lon ?? ""}
                                onChange={(e) => setUserCoords(prev => ({
                                    lat: prev?.lat ?? 0,
                                    lon: parseFloat(e.target.value)
                                }))}
                                className="input input-bordered input-sm"
                            />
                            <input
                                type="number"
                                placeholder="Odległość (km)"
                                value={distance}
                                onChange={(e) => setDistance(e.target.value)}
                                className="input input-bordered input-sm"
                            />
                            <button onClick={getUserLocation} className="btn btn-sm btn-outline">
                                📍 Użyj mojej lokalizacji
                            </button>
                        </div>
                    </div>

                    {/* Fuel Types */}
                    <div className="mb-4">
                        <label className="block text-sm font-medium mb-2">Rodzaj paliwa</label>
                        <div className="flex flex-wrap gap-2">
                            {fuelTypes.map(type => (
                                <button
                                    key={type}
                                    onClick={() => toggleFuelType(type)}
                                    className={`btn btn-sm ${
                                        selectedFuelTypes.includes(type)
                                            ? "btn-primary"
                                            : "btn-outline"
                                    }`}
                                >
                                    {type}
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Price Range */}
                    <div className="mb-4">
                        <label className="block text-sm font-medium mb-2">Zakres cen</label>
                        <div className="grid grid-cols-2 gap-3">
                            <input
                                type="number"
                                placeholder="Min cena"
                                value={minPrice}
                                onChange={(e) => setMinPrice(e.target.value)}
                                className="input input-bordered input-sm"
                                step="0.01"
                            />
                            <input
                                type="number"
                                placeholder="Max cena"
                                value={maxPrice}
                                onChange={(e) => setMaxPrice(e.target.value)}
                                className="input input-bordered input-sm"
                                step="0.01"
                            />
                        </div>
                    </div>

                    {/* Brand */}
                    <div className="mb-4">
                        <label className="block text-sm font-medium mb-2">Marka</label>
                        <input
                            type="text"
                            placeholder="np. Orlen, Shell, BP..."
                            value={brandName}
                            onChange={(e) => setBrandName(e.target.value)}
                            className="input input-bordered input-sm w-full"
                        />
                    </div>

                    {/* Sorting */}
                    <div className="mb-4">
                        <label className="block text-sm font-medium mb-2">Sortowanie</label>
                        <div className="flex gap-3">
                            <button
                                onClick={() => {
                                    setSortColumn("distance");
                                    setSortDirection("asc");
                                }}
                                className={`btn btn-sm ${sortColumn === "distance" ? "btn-primary" : "btn-outline"}`}
                            >
                                Odległość
                            </button>
                            <button
                                onClick={() => {
                                    setSortColumn("price");
                                    setSortDirection("asc");
                                }}
                                className={`btn btn-sm ${sortColumn === "price" ? "btn-primary" : "btn-outline"}`}
                            >
                                Cena
                            </button>
                            <select
                                value={sortDirection}
                                onChange={(e) => setSortDirection(e.target.value as "asc" | "desc")}
                                className="select select-sm select-bordered"
                            >
                                <option value="asc">Rosnąco</option>
                                <option value="desc">Malejąco</option>
                            </select>
                        </div>
                    </div>

                    <button onClick={handleSearch} className="btn btn-primary w-full">
                        🔍 Szukaj stacji
                    </button>
                </section>

                {/* Results */}
                <section className="bg-base-300 p-6 rounded-xl shadow-md">
                    <div className="flex justify-between items-center mb-4">
                        <h2 className="text-xl font-semibold">
                            Wyniki ({totalCount ?? 0})
                        </h2>
                        <div className="flex items-center gap-2">
                            <label className="text-sm">Wyników na stronę:</label>
                            <select
                                value={pageSize}
                                onChange={(e) => {
                                    setPageSize(Number(e.target.value));
                                    setPageNumber(1);
                                }}
                                className="select select-sm select-bordered"
                            >
                                {[10, 20, 30, 50].map(n => (
                                    <option key={n} value={n}>{n}</option>
                                ))}
                            </select>
                        </div>
                    </div>

                    {loading ? (
                        <div className="text-center py-8">
                            <span className="loading loading-spinner loading-lg"></span>
                            <p className="mt-2">Ładowanie stacji...</p>
                        </div>
                    ) : error ? (
                        <div className="alert alert-error">
                            <span>{error}</span>
                        </div>
                    ) : stations && stations.length > 0 ? (
                        <>
                            <div className="overflow-x-auto">
                                <table className="table table-zebra w-full">
                                    <thead>
                                    <tr>
                                        <th>Nazwa</th>
                                        <th>PB95</th>
                                        <th>Diesel</th>
                                        <th>LPG</th>
                                        <th>Miasto</th>
                                        <th>Ulica</th>
                                        <th>Akcje</th>
                                    </tr>
                                    </thead>
                                    <tbody>
                                    {stations.map((s, idx) => (
                                        <tr key={s.id ?? idx}>
                                            <td className="font-medium">{s.brandName ?? s.name ?? "-"}</td>
                                            <td>{formatPriceValue(s.pricePb95)}</td>
                                            <td>{formatPriceValue(s.priceDiesel)}</td>
                                            <td>{formatPriceValue(s.priceLpg)}</td>
                                            <td>{s.city ?? "-"}</td>
                                            <td>
                                                {s.street ?? "-"}
                                                {s.houseNumber ? ` ${s.houseNumber}` : ""}
                                            </td>
                                            <td>
                                                <div className="flex gap-2">
                                                    <a
                                                        href={`/map?lat=${s.latitude ?? ""}&lon=${s.longitude ?? ""}`}
                                                        className="btn btn-xs btn-outline"
                                                    >
                                                        🗺️ Mapa
                                                    </a>
                                                </div>
                                            </td>
                                        </tr>
                                    ))}
                                    </tbody>
                                </table>
                            </div>

                            <div className="mt-6 flex justify-between items-center">
                                {renderPageButtons()}
                                <div className="text-sm text-gray-400">
                                    Strona {pageNumber} / {totalPages}
                                </div>
                            </div>
                        </>
                    ) : (
                        <div className="text-center py-8 text-gray-400">
                            Brak stacji spełniających kryteria wyszukiwania
                        </div>
                    )}
                </section>
            </main>

            <Footer />
        </div>
    );
}