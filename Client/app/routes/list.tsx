import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";

const API_BASE = "http://localhost:5111";

function parseJwt(token: string | null) {
  if (!token) return null;
  try {
    return JSON.parse(atob(token.split(".")[1]));
  } catch (e) {
    return null;
  }
}

type Station = {
  id?: string;
  brandName?: string; // sometimes backend uses brandName
  name?: string; // sometimes name
  street?: string;
  houseNumber?: string | number;
  postalCode?: string;
  latitude?: number;
  longitude?: number;
  city?: string;
  imageUrl?: string;
  address?: string;
  distanceMeters?: number; // if backend provides
  fuelPrices?: Record<string, number | string> | null;
  // fallback price fields (in case backend shaped differently)
  pricePb95?: number | null;
  priceDiesel?: number | null;
  priceLpg?: number | null;
};

type SortColumn =
  | "name"
  | "city"
  | "street"
  | "benzyna"
  | "diesel"
  | "lpg"
  | "distance"
  | null;

export default function ListPage() {
  const [email, setEmail] = React.useState<string | null>(null);
  const [stations, setStations] = React.useState<Station[] | null>(null);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);

  const [sortColumn, setSortColumn] = React.useState<SortColumn>(null);
  const [sortDirection, setSortDirection] = React.useState<"asc" | "desc">("asc");

  const [query, setQuery] = React.useState("");
  const [userCoords, setUserCoords] = React.useState<{ lat: number; lon: number } | null>(null);

  React.useEffect(() => {
    const token = localStorage.getItem("token");
    const expiration = localStorage.getItem("token_expiration");

    if (!token || !expiration || new Date(expiration) <= new Date()) {
      if (typeof window !== "undefined") window.location.href = "/login";
      return;
    }

    const decoded = parseJwt(token);
    const userEmail = decoded?.email || decoded?.sub || null;
    setEmail(userEmail ?? "Zalogowany użytkownik");

    fetchAllStations(token);

    // request geolocation so we can compute distances client-side if backend doesn't provide them
    if ("geolocation" in navigator) {
      navigator.geolocation.getCurrentPosition(
        (pos) => {
          setUserCoords({ lat: pos.coords.latitude, lon: pos.coords.longitude });
        },
        () => {
          setUserCoords(null);
        },
        { timeout: 10_000 }
      );
    }
  }, []);

  async function fetchAllStations(token: string) {
    setLoading(true);
    setError(null);

    try {
      const res = await fetch(`${API_BASE}/api/station/map/all`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
          Accept: "application/json",
        },
        // empty filters -> return all stations according to controller docs
        body: JSON.stringify({ brandName: [], locationLatitude: null, locationLongitude: null, distance: null }),
      });

      if (!res.ok) {
        // try a GET fallback (some backends expose other endpoints)
        const fallback = await fetch(`${API_BASE}/api/station/map/all`, {
          headers: {
            Authorization: `Bearer ${token}`,
            Accept: "application/json",
          },
        });
        if (!fallback.ok) throw new Error(`fetch-failed ${res.status}`);
        const data2 = await fallback.json();
        setStations(normalizeStations(data2));
        return;
      }

      const data = await res.json();
      setStations(normalizeStations(data));
    } catch (err) {
      console.error("Błąd pobierania stacji:", err);
      setError("Nie udało się pobrać listy stacji z serwera.");
      setStations([]);
    } finally {
      setLoading(false);
    }
  }

  function normalizeStations(data: any): Station[] {
    // backend might return array or { stations: [...] }
    const arr = Array.isArray(data) ? data : Array.isArray(data?.stations) ? data.stations : [];

    return arr.map((s: any) => {
      // try to unify naming
      const fuelPrices = s.fuelPrices ?? s.prices ?? null;

      const normalized: Station = {
        id: s.id ?? s.stationId ?? undefined,
        name: s.brandName ?? s.name ?? s.stationName ?? undefined,
        brandName: s.brandName ?? s.name ?? undefined,
        street: s.street ?? (s.address ? s.address.split(",")[0] : undefined) ?? undefined,
        houseNumber: s.houseNumber ?? s.houseNumberString ?? s.no ?? undefined,
        postalCode: s.postalCode ?? s.postalcode ?? s.postal ?? undefined,
        latitude: s.latitude ?? s.lat ?? undefined,
        longitude: s.longitude ?? s.lon ?? s.lng ?? undefined,
        city: s.city ?? s.town ?? s.locationCity ?? undefined,
        imageUrl: s.imageUrl ?? s.image ?? undefined,
        address: s.address ?? undefined,
        distanceMeters: s.distanceMeters ?? s.distanceInMeters ?? (typeof s.distance === "number" && s.distance > 1000 ? s.distance : undefined),
        fuelPrices: fuelPrices,
        pricePb95: s.pricePb95 ?? s.pb95 ?? null,
        priceDiesel: s.priceDiesel ?? s.on ?? null,
        priceLpg: s.priceLpg ?? s.lpg ?? null,
      };

      return normalized;
    });
  }

  function formatDistance(m?: number) {
    if (m == null || Number.isNaN(m)) return "-";
    if (m >= 1000) return `${(m / 1000).toFixed(2)} km`;
    return `${Math.round(m)} m`;
  }

  function haversineDistanceMeters(lat1: number, lon1: number, lat2: number, lon2: number) {
    const toRad = (deg: number) => (deg * Math.PI) / 180;
    const R = 6371000; // meters
    const dLat = toRad(lat2 - lat1);
    const dLon = toRad(lon2 - lon1);
    const a =
      Math.sin(dLat / 2) * Math.sin(dLat / 2) +
      Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLon / 2) * Math.sin(dLon / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c;
  }

  function extractPrice(st: Station, candidates: string[]): number | null {
    // check dedicated fields first
    if (candidates.includes("benzyna") && typeof st.pricePb95 === "number") return Number(st.pricePb95);
    if (candidates.includes("diesel") && typeof st.priceDiesel === "number") return Number(st.priceDiesel);
    if (candidates.includes("lpg") && typeof st.priceLpg === "number") return Number(st.priceLpg);

    const fp = st.fuelPrices;
    if (!fp) return null;

    // normalize keys to lowercase
    const lcMap: Record<string, number> = {};
    Object.entries(fp).forEach(([k, v]) => {
      const val = typeof v === "string" ? parseFloat(v.replace(",", ".")) : Number(v);
      if (!Number.isNaN(val)) lcMap[k.toLowerCase()] = val;
    });

    // try to find best matching key
    for (const cand of candidates) {
      // exact match
      if (lcMap[cand.toLowerCase()] !== undefined) return lcMap[cand.toLowerCase()];
    }

    // try contains
    for (const key of Object.keys(lcMap)) {
      for (const cand of candidates) {
        if (key.includes(cand.toLowerCase())) return lcMap[key];
      }
    }

    // fallback: return the smallest numeric price available in fuelPrices
    const nums = Object.values(lcMap).filter((n) => typeof n === "number");
    if (nums.length === 0) return null;
    return Math.min(...(nums as number[]));
  }

  const benzynaCandidates = ["pb95", "pb98", "benzyna", "petrol", "gasoline"];
  const dieselCandidates = ["diesel", "on", "olej", "olejnap", "oil"];
  const lpgCandidates = ["lpg", "gaz"];

  const sortedAndFiltered = React.useMemo(() => {
    if (!stations) return [] as Station[];

    // compute distances if possible
    const withComputed = stations.map((s) => {
      const newS = { ...s } as Station;
      if ((newS.distanceMeters === undefined || newS.distanceMeters === null) && userCoords && newS.latitude && newS.longitude) {
        newS.distanceMeters = Math.round(haversineDistanceMeters(userCoords.lat, userCoords.lon, newS.latitude, newS.longitude));
      }
      return newS;
    });

    // filter by query
    const q = query.trim().toLowerCase();
    let out = withComputed.filter((s) => {
      if (!q) return true;
      return (
        (s.name ?? "").toLowerCase().includes(q) ||
        (s.city ?? "").toLowerCase().includes(q) ||
        (s.street ?? "").toLowerCase().includes(q)
      );
    });

    if (!sortColumn) return out;

    const dir = sortDirection === "asc" ? 1 : -1;

    out.sort((a, b) => {
      switch (sortColumn) {
        case "name": {
          const aa = (a.name ?? "").toLowerCase();
          const bb = (b.name ?? "").toLowerCase();
          if (aa === bb) return 0;
          return aa > bb ? dir : -dir;
        }
        case "city": {
          const aa = (a.city ?? "").toLowerCase();
          const bb = (b.city ?? "").toLowerCase();
          if (aa === bb) return 0;
          return aa > bb ? dir : -dir;
        }
        case "street": {
          const aa = (a.street ?? "").toLowerCase();
          const bb = (b.street ?? "").toLowerCase();
          if (aa === bb) return 0;
          return aa > bb ? dir : -dir;
        }
        case "benzyna": {
          const pa = extractPrice(a, benzynaCandidates);
          const pb = extractPrice(b, benzynaCandidates);
          if (pa == null && pb == null) return 0;
          if (pa == null) return 1 * dir; // put nulls at the bottom for asc
          if (pb == null) return -1 * dir;
          return (pa - pb) * dir;
        }
        case "diesel": {
          const pa = extractPrice(a, dieselCandidates);
          const pb = extractPrice(b, dieselCandidates);
          if (pa == null && pb == null) return 0;
          if (pa == null) return 1 * dir;
          if (pb == null) return -1 * dir;
          return (pa - pb) * dir;
        }
        case "lpg": {
          const pa = extractPrice(a, lpgCandidates);
          const pb = extractPrice(b, lpgCandidates);
          if (pa == null && pb == null) return 0;
          if (pa == null) return 1 * dir;
          if (pb == null) return -1 * dir;
          return (pa - pb) * dir;
        }
        case "distance": {
          const da = a.distanceMeters ?? Number.POSITIVE_INFINITY;
          const db = b.distanceMeters ?? Number.POSITIVE_INFINITY;
          return (da - db) * dir;
        }
        default:
          return 0;
      }
    });

    return out;
  }, [stations, query, sortColumn, sortDirection, userCoords]);

  function toggleSort(col: SortColumn) {
    if (sortColumn === col) {
      // flip direction
      setSortDirection((d) => (d === "asc" ? "desc" : "asc"));
    } else {
      setSortColumn(col);
      setSortDirection("asc"); // first click -> asc ("↓" as requested)
    }
  }

  function showArrow(col: SortColumn) {
    if (sortColumn !== col) return null;
    return sortDirection === "asc" ? "↓" : "↑";
  }

  function formatPriceValue(val: number | null | undefined) {
    if (val == null || Number.isNaN(val)) return "-";
    return `${Number(val).toFixed(2)} zł`;
  }

  return (
    <div className="min-h-screen bg-base-200 text-base-content">
      <Header />

      <main className="mx-auto max-w-6xl px-4 py-8">
        <div className="flex items-center justify-between mb-6">
          <h1 className="text-2xl md:text-3xl font-bold">
            Lista stacji benzynowych
          </h1>
            <a href="/dashboard"
            className="btn btn-active btn-info">
            ← Powrót do dashboardu
            </a>
        </div>

        <section className="bg-base-300 p-4 rounded-xl shadow-md mb-6">
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 mb-4">
            <div className="flex items-center gap-3">
              <input
                className="p-2 rounded-md bg-base-100 border border-gray-600 outline-none"
                placeholder="Szukaj po nazwie, mieście lub ulicy..."
                value={query}
                onChange={(e) => setQuery(e.target.value)}
              />
            </div>

            <div className="text-sm text-gray-400">Znaleziono: {stations ? sortedAndFiltered.length : "-"}</div>
          </div>

          {loading ? (
            <div>Ładowanie listy stacji...</div>
          ) : error ? (
            <div className="text-red-400">{error}</div>
          ) : stations && stations.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="table table-compact w-full">
                <thead>
                  <tr>
                    <th className="cursor-pointer" onClick={() => toggleSort("name")}>Nazwa {showArrow("name")}</th>
                    <th className="cursor-pointer" onClick={() => toggleSort("benzyna")}>Cena benzyny {showArrow("benzyna")}</th>
                    <th className="cursor-pointer" onClick={() => toggleSort("diesel")}>Cena diesel {showArrow("diesel")}</th>
                    <th className="cursor-pointer" onClick={() => toggleSort("lpg")}>Cena LPG {showArrow("lpg")}</th>
                    <th className="cursor-pointer" onClick={() => toggleSort("distance")}>Odległość {showArrow("distance")}</th>
                    <th className="cursor-pointer" onClick={() => toggleSort("city")}>Miasto {showArrow("city")}</th>
                    <th className="cursor-pointer" onClick={() => toggleSort("street")}>Ulica {showArrow("street")}</th>
                    <th>Akcje</th>
                  </tr>
                </thead>
                <tbody>
                  {sortedAndFiltered.map((s, idx) => {
                    const name = s.name ?? s.brandName ?? "-";
                    const benz = extractPrice(s, benzynaCandidates);
                    const dies = extractPrice(s, dieselCandidates);
                    const lpg = extractPrice(s, lpgCandidates);
                    const distance = s.distanceMeters ?? null;

                    return (
                      <tr key={s.id ?? `${name}-${idx}`}>
                        <td>
                          <a
                            className="font-medium hover:underline cursor-pointer"
                            onClick={() => toggleSort("name")}
                          >
                            {name}
                          </a>
                        </td>
                        <td>{formatPriceValue(benz)}</td>
                        <td>{formatPriceValue(dies)}</td>
                        <td>{formatPriceValue(lpg)}</td>
                        <td>{formatDistance(distance ?? undefined)}</td>
                        <td>{s.city ?? "-"}</td>
                        <td>{`${s.street ?? "-"}${s.houseNumber ? " " + s.houseNumber : ""}`}</td>
                        <td>
                          <div className="flex gap-2">
                            <a
                              href={`/map?lat=${s.latitude ?? ""}&lon=${s.longitude ?? ""}`}
                              className="btn btn-xs btn-outline"
                            >
                              Pokaż na mapie
                            </a>
                            <a
                              href={`/list#${encodeURIComponent(s.id ?? name ?? String(idx))}`}
                              className="btn btn-xs btn-outline btn-primary"
                            >
                              Szczegóły
                            </a>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="text-gray-300">Brak dostępnych stacji.</div>
          )}
        </section>
      </main>

      <Footer />
    </div>
  );
}
