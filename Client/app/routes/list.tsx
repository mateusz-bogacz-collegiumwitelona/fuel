import * as React from "react";
import { Link } from "react-router";
import Header from "../components/header";
import Footer from "../components/footer";
import { useTranslation } from "react-i18next";
import { API_BASE } from "../components/api";

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
};

type SortColumn = "distance" | "price" | null;

export default function ListPage() {
    const { t } = useTranslation();
  React.useEffect(() => {
    document.title = t("list.stationlist") + " - FuelStats";
  }, [t]);
    const [email, setEmail] = React.useState<string | null>(null);
    const [stations, setStations] = React.useState<Station[] | null>(null);
    const [loading, setLoading] = React.useState(true);
    const [error, setError] = React.useState<string | null>(null);

    // Filters
    const [minPrice, setMinPrice] = React.useState<string>("");
    const [maxPrice, setMaxPrice] = React.useState<string>("");
    const [brandName, setBrandName] = React.useState<string>("");
    const [distance, setDistance] = React.useState<string>("");

    // Sorting
    const [sortColumn, setSortColumn] = React.useState<SortColumn>(null);
    const [sortDirection, setSortDirection] = React.useState<"asc" | "desc">("asc");
    // For price-sorting / price-filtering the API requires a fuel type. This is single-select.
    const [priceFuel, setPriceFuel] = React.useState<string | null>(null);
    const [priceDropdownOpen, setPriceDropdownOpen] = React.useState<boolean>(false);

    // Columns control - which fuel columns to show (scalable table)
    const [visibleFuelColumns, setVisibleFuelColumns] = React.useState<string[]>(["PB95", "ON", "LPG"]);

    // Location
    const [userCoords, setUserCoords] = React.useState<{ lat: number; lon: number } | null>(null);
    // Address autocomplete / geocoding
    const [address, setAddress] = React.useState<string>("");
    const [addressSuggestions, setAddressSuggestions] = React.useState<Array<{ display_name: string; lat: string; lon: string }>>([]);
    const [addressLoading, setAddressLoading] = React.useState<boolean>(false);
    const geocodeTimeoutRef = React.useRef<number | null>(null);

    // Available lists from API (fuel types & brands)
    const [availableFuelTypes, setAvailableFuelTypes] = React.useState<string[]>([]);
    const [availableBrands, setAvailableBrands] = React.useState<string[]>([]);
    const [brandsOpen, setBrandsOpen] = React.useState<boolean>(false);

    // Fetch available fuel types and brands once
    React.useEffect(() => {
        (async () => {
            try {
                const token = localStorage.getItem('token');
                const fetchOptions: any = {
                    headers: { Accept: 'application/json' },
                    credentials: 'include',
                };
                if (token) fetchOptions.headers.Authorization = `Bearer ${token}`;

                // Fuel codes
                try {
                    const fRes = await fetch(`${API_BASE}/api/station/fuel-codes`, fetchOptions);
                    const fText = await fRes.text();

                    if (!fRes.ok) {
                        if (fRes.status === 404) setAvailableFuelTypes([]);
                    } else {
                        let fJson: any;
                        try { fJson = JSON.parse(fText); } catch { fJson = fText; }

                        let arr: any[] = [];
                        if (Array.isArray(fJson)) arr = fJson;
                        else if (Array.isArray(fJson.data)) arr = fJson.data;
                        else if (Array.isArray(fJson.items)) arr = fJson.items;

                        arr = arr.map((x: any) => typeof x === 'string' ? x : (x.code || x.name || x.value || x));
                        arr = Array.from(new Set(arr.filter(Boolean)));
                        setAvailableFuelTypes(arr);

                        // keep visible columns in sync (don't overwrite user's choice)
                        setVisibleFuelColumns(prev => {
                            if (!prev || prev.length === 0) return arr.slice(0, 3);
                            return prev.filter(p => arr.includes(p)).concat(arr.filter(a => !prev.includes(a)).slice(0, Math.max(0, 3 - prev.length)));
                        });
                    }
                } catch (e) {
                    console.error('[ListPage] error fetching fuel-codes', e);
                    setAvailableFuelTypes([]);
                }

                // Brands
                try {
                    const bRes = await fetch(`${API_BASE}/api/station/all-brands`, fetchOptions);
                    const bText = await bRes.text();

                    if (!bRes.ok) {
                        if (bRes.status === 404) setAvailableBrands([]);
                    } else {
                        let bJson: any;
                        try { bJson = JSON.parse(bText); } catch { bJson = bText; }

                        let arr: any[] = [];
                        if (Array.isArray(bJson)) arr = bJson;
                        else if (Array.isArray(bJson.data)) arr = bJson.data;
                        else if (Array.isArray(bJson.items)) arr = bJson.items;

                        arr = arr.map((x: any) => typeof x === 'string' ? x : (x.brandName || x.name || x.value || x));
                        arr = Array.from(new Set(arr.filter(Boolean)));
                        setAvailableBrands(arr);
                    }
                } catch (e) {
                    console.error('[ListPage] error fetching all-brands', e);
                    setAvailableBrands([]);
                }

            } catch (e) {
                console.error('Error fetching fuel types/brands', e);
                setAvailableFuelTypes([]);
                setAvailableBrands([]);
            }
        })();
    }, []);

    async function geocodeAddress(query: string) {
        if (!query || query.trim().length === 0) {
            setAddressSuggestions([]);
            return [] as any[];
        }
        setAddressLoading(true);
        try {
            const res = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&addressdetails=0&limit=5`);
            if (!res.ok) return [];
            const json = await res.json();
            const arr = Array.isArray(json) ? json : [];
            setAddressSuggestions(arr);
            return arr;
        } catch (e) {
            console.error("Geocode error", e);
            return [];
        } finally {
            setAddressLoading(false);
        }
    }

    function selectAddress(suggestion: any) {
        setAddress(suggestion.display_name || "");
        setUserCoords({ lat: parseFloat(suggestion.lat), lon: parseFloat(suggestion.lon) });
        setAddressSuggestions([]);
    }

    async function applyAddress() {
        if (userCoords) return;
        if (!address) return;
        const suggestions = await geocodeAddress(address);
        const first = suggestions[0];
        if (first) {
            selectAddress(first);
        } else {
            setError("Nie znaleziono adresu.");
        }
    }

    // Pagination
    const [pageNumber, setPageNumber] = React.useState<number>(1);
    const [pageSize, setPageSize] = React.useState<number>(10);
    const [totalPages, setTotalPages] = React.useState<number>(1);
    const [totalCount, setTotalCount] = React.useState<number | null>(null);

    // Collapsible filters
    const [filtersOpen, setFiltersOpen] = React.useState<boolean>(false);

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
    }, [email, pageNumber, pageSize, sortColumn, sortDirection, priceFuel, userCoords]);

    async function fetchStations() {
        setLoading(true);
        setError(null);

        const token = localStorage.getItem("token");

        // Enforce API requirements: min/max price require a fuel type
        if ((minPrice || maxPrice) && !priceFuel) {
            setError("Filtr ceny wymaga wybranego paliwa w sekcji sortowania (wybierz paliwo przy sortowaniu po cenie).\n");
            setLoading(false);
            return;
        }

        // Build request body according to API swagger (camelCase keys)
        const requestBody: any = {
            locationLatitude: userCoords?.lat ?? null,
            locationLongitude: userCoords?.lon ?? null,
            distance: distance ? parseFloat(distance) : null,
            fuelType: priceFuel ? [priceFuel] : undefined,
            minPrice: minPrice ? parseFloat(minPrice) : null,
            maxPrice: maxPrice ? parseFloat(maxPrice) : null,
            brandName: brandName || null,
            sortingByDisance: sortColumn === "distance" ? (true) : null,
            sortingByPrice: sortColumn === "price" ? (true) : null,
            sortingDirection: sortColumn ? sortDirection : null,
            pagging: {
                pageNumber: pageNumber,
                pageSize: pageSize,
            },
        };

        // remove undefined fields (API tolerates nulls but keep payload cleaner)
        Object.keys(requestBody).forEach(k => {
            if (requestBody[k] === undefined) delete requestBody[k];
        });

        try {
            const headers: Record<string, string> = {
                "Content-Type": "application/json",
                Accept: "application/json",
            };
            if (token) headers["Authorization"] = `Bearer ${token}`;

            const res = await fetch(`${API_BASE}/api/station/list`, {
                method: "POST",
                headers,
                credentials: "include",
                body: JSON.stringify(requestBody),
            });

            if (!res.ok) {
                const errorText = await res.text();
                let errorMessage = `Błąd ${res.status}: `;
                try {
                    const errorJson = JSON.parse(errorText);
                    errorMessage += errorJson.message || errorJson.errors?.join(", ") || "Nieznany błąd";
                } catch {
                    errorMessage += errorText || "Nieznany błąd";
                }

                setError(errorMessage);
                setStations([]);
                setLoading(false);
                return;
            }

            const data = await res.json();
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
                    const code = (item.fuelCode ?? "").toString();
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
            } as Station;
        });
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

    // UI helpers for sorting toggles
    function toggleDistanceSort() {
        if (sortColumn === "distance") {
            setSortDirection(prev => prev === "asc" ? "desc" : "asc");
        } else {
            setSortColumn("distance");
            setSortDirection("asc");
            // keep the selected fuel persistent — if none is selected, default to the first available fuel
            if (!priceFuel && availableFuelTypes && availableFuelTypes.length > 0) {
                setPriceFuel(availableFuelTypes[0]);
            }
        }
    }

    function handlePriceSortSelect(fuel: string) {
        if (sortColumn === "price" && priceFuel === fuel) {
            setSortDirection(prev => prev === "asc" ? "desc" : "asc");
        } else {
            setSortColumn("price");
            setPriceFuel(fuel);
            setSortDirection("asc");
        }
    }

    return (
        <div className="min-h-screen bg-base-200 text-base-content">
            <Header />

            <main className="mx-auto max-w-7xl px-4 py-8">
                <div className="flex items-center justify-between mb-6">
                    <h1 className="text-2xl md:text-3xl font-bold">{t("list.stationlist")}</h1>
                    <a href="/dashboard" className="btn btn-outline">
                        {t("list.dashboardback")}
                    </a>
                </div>

                <section className="mb-6">
                    <div className="flex items-center justify-between mb-3">
                        <button
                            onClick={() => setFiltersOpen(prev => !prev)}
                            aria-expanded={filtersOpen}
                            className="btn btn-outline btn-sm">

                            {t("list.filters")} {filtersOpen ? '↑' : '↓'}
                        </button>
                    </div>

                    <div className={`overflow-hidden transition-all duration-300 bg-base-300 rounded-xl shadow-md ${filtersOpen ? 'p-6 max-h-[2000px] opacity-100' : 'p-0 max-h-0 opacity-0'}`}>

                        <div className="mb-4">
                            <label className="block text-sm font-medium mb-2">{t("list.address")}</label>
                            <div className="relative">
                                <input
                                    type="text"
                                    placeholder={t("list.typeaddress")}
                                    value={address}
                                    onChange={(e) => {
                                        const v = e.target.value;
                                        setAddress(v);
                                        if (geocodeTimeoutRef.current) window.clearTimeout(geocodeTimeoutRef.current);
                                        geocodeTimeoutRef.current = window.setTimeout(() => geocodeAddress(v), 500) as unknown as number;
                                    }}
                                    className="input input-bordered input-sm w-full"
                                />

                                {addressLoading && (
                                    <span className="loading loading-spinner loading-sm absolute right-2 top-2"></span>
                                )}

                                {addressSuggestions.length > 0 && (
                                    <ul className="absolute z-50 bg-base-100 w-full mt-1 rounded-md shadow-lg max-h-52 overflow-auto">
                                        {addressSuggestions.map((s, i) => (
                                            <li
                                                key={i}
                                                className="p-2 hover:bg-base-200 cursor-pointer"
                                                onClick={() => selectAddress(s)}
                                            >
                                                {s.display_name}
                                            </li>
                                        ))}
                                    </ul>
                                )}
                            </div>

                            <div className="mt-2 flex gap-2">
                                <button
                                    type="button"
                                    className="btn btn-sm btn-outline"
                                    onClick={() => geocodeAddress(address)}
                                >
                                    {t("list.searchaddress")}
                                </button>
                                <button
                                    type="button"
                                    className="btn btn-sm"
                                    onClick={() => applyAddress()}
                                >
                                    {t("list.useaddress")}
                                </button>
                                <button onClick={() => {
                                    if (navigator.geolocation) {
                                        navigator.geolocation.getCurrentPosition((pos) => setUserCoords({ lat: pos.coords.latitude, lon: pos.coords.longitude }), (err) => setError("Nie udało się pobrać lokalizacji: " + err.message));
                                    } else setError("Geolokalizacja nie jest wspierana przez twoją przeglądarkę");
                                }} className="btn btn-sm btn-outline ml-auto">
                                    {t("list.usealocalization")}
                                </button>
                            </div>
                        </div>

                        <div className="mb-4">
                            <label className="block text-sm font-medium mb-2">{t("list.mark")}</label>
                            <div className="relative inline-block w-full">
                                <button
                                    type="button"
                                    className="btn w-full justify-between"
                                    onClick={() => setBrandsOpen(b => !b)}
                                >
                                    {brandName || t("list.choosemark")} {brandsOpen ? '▲' : '▼'}
                                </button>

                                {brandsOpen && (
                                    <div className="dropdown-content mt-2 w-full rounded-box bg-base-100 shadow-sm p-2 z-50">
                                        <input
                                            type="text"
                                            placeholder="Filtruj marki..."
                                            className="input input-sm mb-2 w-full"
                                            value={""}
                                            onChange={() => { /* simple: brand filtering handled clientside via availableBrands list */ }}
                                        />
                                        <ul className="max-h-40 overflow-auto">
                                            {availableBrands.map((b) => (
                                                <li key={b}>
                                                    <button
                                                        type="button"
                                                        className={`w-full text-left ${brandName === b ? 'font-semibold' : ''}`}
                                                        onClick={() => { setBrandName(b); setBrandsOpen(false); }}
                                                    >
                                                        {b}
                                                    </button>
                                                </li>
                                            ))}
                                            {availableBrands.length === 0 && (
                                                <li className="p-2 text-sm text-gray-500">{t("list.nomark")}</li>
                                            )}
                                        </ul>
                                    </div>
                                )}
                            </div>
                        </div>

                        <div className="mb-4">
                            <label className="block text-sm font-medium mb-2">{t("list.pricerange")}</label>
                            <div className="grid grid-cols-2 gap-3">
                                <input
                                    type="number"
                                    placeholder={t("list.minprice")}
                                    value={minPrice}
                                    onChange={(e) => setMinPrice(e.target.value)}
                                    className="input input-bordered input-sm"
                                    step="0.01"
                                />
                                <input
                                    type="number"
                                    placeholder={t("list.maxprice")}
                                    value={maxPrice}
                                    onChange={(e) => setMaxPrice(e.target.value)}
                                    className="input input-bordered input-sm"
                                    step="0.01"
                                />
                            </div>
                        </div>

                        <button onClick={() => { setPageNumber(1); fetchStations(); }} className="btn btn-primary w-full">
                            {t("list.searchstation")}
                        </button>

                    </div>
                </section>

                <section className="bg-base-300 p-6 rounded-xl shadow-md">
                    <div className="flex justify-between items-center mb-4">
                        <h2 className="text-xl font-semibold">
                            {t("list.results")} ({totalCount ?? 0})
                        </h2>
                        <div className="flex items-center gap-2">
                            <label className="text-sm">{t("list.muchresults")}</label>
                            <select
                                value={pageSize}
                                onChange={(e) => { setPageSize(Number(e.target.value)); setPageNumber(1); }}
                                className="select select-sm select-bordered"
                            >
                                {[10, 20, 30, 50].map(n => (
                                    <option key={n} value={n}>{n}</option>
                                ))}
                            </select>
                        </div>
                    </div>

                    {/* Sorting area - includes fuel selection for price sorting */}
                    <div className="mb-4 p-3 border rounded-md bg-base-200">
                        <div className="flex items-center gap-3 flex-wrap">
                            <button onClick={toggleDistanceSort} className={`btn btn-sm ${sortColumn === "distance" ? "btn-primary" : "btn-outline"}`}>
                                {t("list.distance")} {sortColumn === "distance" ? (sortDirection === "asc" ? '↑' : '↓') : ''}
                                {priceFuel && (
                                    <span className="ml-2 badge badge-sm">{priceFuel}</span>
                                )}
                            </button>

                            <div className="relative">
                                <button
                                    onClick={() => setPriceDropdownOpen(v => !v)}
                                    className={`btn btn-sm ${sortColumn === "price" ? "btn-primary" : "btn-outline"}`}
                                >
                                    {t("list.price")} {sortColumn === "price" && priceFuel
                                        ? `(${priceFuel}) ${sortDirection === 'asc' ? '↑' : '↓'}`
                                        : ''}
                                </button>

                                {/* dropdown of single fuel choices for price-sort */}
                                {priceDropdownOpen && (
                                    <div className="absolute mt-2 w-44 rounded-box bg-base-100 shadow-sm p-2 z-50">
                                        {availableFuelTypes.length === 0 ? (
                                            <div className="p-2 text-sm">{t("list.nofuel")}</div>
                                        ) : (
                                            availableFuelTypes.map(ft => (
                                                <button
                                                    key={ft}
                                                    className={`w-full text-left py-1 ${priceFuel === ft ? 'font-semibold' : ''}`}
                                                    onClick={() => handlePriceSortSelect(ft)}
                                                >
                                                    {priceFuel === ft ? '✓ ' : ''}{ft}
                                                </button>
                                            ))
                                        )}
                                    </div>
                                )}
                            </div>

                            <div className="flex items-center gap-2 ml-auto">
                                <label className="text-sm mr-2">{t("list.showFuels")}</label>
                                <div className="flex gap-1 items-center">
                                    {availableFuelTypes.slice(0, 6).map(ft => (
                                        <button key={ft} className={`btn btn-xs ${visibleFuelColumns.includes(ft) ? 'btn-primary' : 'btn-outline'}`} onClick={() => {
                                            setVisibleFuelColumns(prev => prev.includes(ft) ? prev.filter(p => p !== ft) : [...prev, ft]);
                                        }}>{ft}</button>
                                    ))}
                                </div>
                            </div>
                        </div>
                    </div>

                    {loading ? (
                        <div className="text-center py-8">
                            <span className="loading loading-spinner loading-lg"></span>
                            <p className="mt-2">{t("list.stationloading")}</p>
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
                                            <th>{t("list.name")}</th>
                                            {/* dynamic fuel columns */}
                                            {visibleFuelColumns.map(f => (
                                                <th key={f}>{f}</th>
                                            ))}

                                            <th>{t("list.city")}</th>
                                            <th>{t("list.street")}</th>
                                            <th>{t("list.action")}</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {stations.map((s, idx) => (
                                            <tr key={s.id ?? idx}>
                                                <td className="font-medium">{s.brandName ?? s.name ?? "-"}</td>

                                                {visibleFuelColumns.map(f => (
                                                    <td key={f}>{formatPriceValue(s.fuelPrices?.[f])}</td>
                                                ))}

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
                                                            {t("list.map")}
                                                        </a>

                                                        <Link
                                                            to={`/station/${encodeURIComponent(String(s.brandName ?? ''))}/${encodeURIComponent(String(s.city ?? ''))}/${encodeURIComponent(String(s.street ?? ''))}/${encodeURIComponent(String(s.houseNumber ?? ''))}`}
                                                            className="btn btn-xs btn-outline"
                                                        >
                                                            {t("map.seedetails")}
                                                        </Link>
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
                                    {t("list.page")} {pageNumber} / {totalPages}
                                </div>
                            </div>
                        </>
                    ) : (
                        <div className="text-center py-8 text-gray-400">
                            {t("list.nostation2")}
                        </div>
                    )}
                </section>
            </main>

            <Footer />
        </div>
    );
}