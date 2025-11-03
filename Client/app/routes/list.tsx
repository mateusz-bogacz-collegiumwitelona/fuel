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
  const [sortDirection, setSortDirection] = React.useState<"asc" | "desc">("desc"); // first click -> desc

  const [query, setQuery] = React.useState("");
  const [userCoords, setUserCoords] = React.useState<{ lat: number; lon: number } | null>(null);

  // pagination state (backend-driven)
  const [pageNumber, setPageNumber] = React.useState<number>(1);
  const [pageSize, setPageSize] = React.useState<number>(10);
  const [totalPages, setTotalPages] = React.useState<number>(1);
  const [totalCount, setTotalCount] = React.useState<number | null>(null);

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

    // fetch initial page
    fetchStations(token, pageNumber, pageSize);

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
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // refetch when pageNumber or pageSize change (after initial auth check, we re-read token inside)
  React.useEffect(() => {
    const token = localStorage.getItem("token");
    if (!token) return;
    fetchStations(token, pageNumber, pageSize);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageNumber, pageSize]);

  async function fetchStations(token: string, pageNum: number, pageSz: number) {
  setLoading(true);
  setError(null);

  // podstawowe body - bazowane na swaggerze (wersja "standard")
  const baseBody = {
    brandName: null,
    locationLatitude: null,
    locationLongitude: null,
    distance: null,
    fuelType: [],
    minPrice: null,
    maxPrice: null,
    sortingByDisance: false,   // some swagger examples use this (typo)
    sortingByPrice: false,
    sortingDirection: sortDirection,
    pagging: {
      pageNumber: pageNum,
      pageSize: pageSz,
    },
  };

  // warianty kompatybilności (jeśli backend oczekuje innych nazw)
  const altBodies = [
    // poprawne spellingi
    {
      ...baseBody,
      sortingByDistance: baseBody.sortingByDisance,
      pagging: undefined,
      paging: { pageNumber: pageNum, pageSize: pageSz },
    },
    // podajemy zarówno pagging i paging na wypadek
    {
      ...baseBody,
      paging: { pageNumber: pageNum, pageSize: pageSz },
    },
    // wersja minimalna (czasami walidator nie lubi nullów)
    {
      fuelType: [],
      pagging: { pageNumber: pageNum, pageSize: pageSz },
    },
  ];

  async function tryPost(bodyObj: any) {
    const res = await fetch(`${API_BASE}/api/station/list`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
        Accept: "application/json",
      },
      body: JSON.stringify(bodyObj),
    });
    return res;
  }

  try {
    // 1) Spróbuj podstawowego body
    let res = await tryPost(baseBody);

    // 2) Jeśli 400/422/404 -> spróbuj alternatyw
    if (!res.ok && (res.status === 400 || res.status === 422 || res.status === 404)) {
      console.warn("Primary POST failed, trying alternative bodies, status:", res.status);
      // pokaż też treść odpowiedzi (jeśli serwer zwraca json/text)
      try {
        const txt = await res.text();
        console.warn("Primary body response text:", txt);
      } catch (e) {}
      let ok = false;
      for (const alt of altBodies) {
        try {
          const altRes = await tryPost(alt);
          if (altRes.ok) {
            res = altRes;
            ok = true;
            break;
          } else {
            const t = await altRes.text();
            console.warn("Alt POST failed status:", altRes.status, "body tried:", alt, "response:", t);
          }
        } catch (e) {
          console.error("Alt POST threw", e);
        }
      }
      if (!ok && !res.ok) {
        // spróbuj GET jako ostatnia opcja
        try {
          const fallback = await fetch(`${API_BASE}/api/station/list`, {
            headers: { Authorization: `Bearer ${token}`, Accept: "application/json" },
          });
          if (fallback.ok) {
            const data2 = await fallback.json();
            applyListResponse(data2);
            return;
          } else {
            const txt = await fallback.text();
            throw new Error(`Fallback GET failed: ${fallback.status} ${txt}`);
          }
        } catch (e) {
          throw e;
        }
      }
    }

    if (!res.ok) {
      // jeśli tu dotarliśmy, to mamy res nie-ok i nie udał się fallback - wypisz szczegóły
      const text = await res.text().catch(() => "<brak treści>");
      console.error("fetchStations: non-ok response:", res.status, text);
      setError(`Serwer zwrócił błąd: ${res.status}. Sprawdź konsolę network / logs backendu.`);
      setStations([]);
      setTotalCount(null);
      setTotalPages(1);
      return;
    }

    // OK
    const data = await res.json();
    applyListResponse(data);
  } catch (err: any) {
    console.error("Błąd pobierania stacji:", err);
    // pokaż szczegół jeśli to fetch/CORS – CORS zwykle blokuje dostęp do odpowiedzi i rzuca TypeError
    if (err instanceof TypeError) {
      setError("Błąd sieci / CORS: sprawdź konsolę network (może brakuje Access-Control-Allow-Origin na backendzie).");
    } else {
      setError("Nie udało się pobrać listy stacji z serwera. Sprawdź konsolę w devtools i logi serwera.");
    }
    setStations([]);
    setTotalCount(null);
    setTotalPages(1);
  } finally {
    setLoading(false);
  }
}

 function applyListResponse(data: any) {
  // swagger sample uses { items: [...], pageNumber, pageSize, totalCount, totalPages }
  const items = Array.isArray(data.items)
    ? data.items
    : Array.isArray(data)
    ? data
    : Array.isArray(data?.stations)
    ? data.stations
    : [];

  // totalCount: prefer number from server, otherwise fallback to items length
  const totalCountVal = typeof data.totalCount === "number" ? data.totalCount : items.length;

  // pageSize: prefer server pageSize if valid, otherwise fallback to items length or 1
  const pageSizeVal =
    typeof data.pageSize === "number" && data.pageSize > 0 ? data.pageSize : Math.max(1, items.length || 1);

  // totalPages: prefer server value if valid, otherwise compute from totalCount/pageSize
  const totalPagesVal =
    typeof data.totalPages === "number" && data.totalPages > 0
      ? data.totalPages
      : Math.max(1, Math.ceil(totalCountVal / pageSizeVal));

  setStations(normalizeStations(items));
  setTotalCount(totalCountVal);
  setTotalPages(totalPagesVal);
  setPageNumber(typeof data.pageNumber === "number" && data.pageNumber > 0 ? data.pageNumber : 1);
}


  function normalizeStations(data: any): Station[] {
    // backend might return array or { stations: [...] } or { items: [...] }
    const arr = Array.isArray(data) ? data : Array.isArray(data?.stations) ? data.stations : Array.isArray(data?.items) ? data.items : [];

    return arr.map((s: any) => {
      // try to unify naming
      const fuelPrices = s.fuelPrices ?? s.prices ?? s.fuelPrice ?? s.fuelPriceList ?? null;

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
        distanceMeters: s.distanceMeters ?? s.distanceInMeters ?? (typeof s.distance === "number" ? s.distance : undefined),
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

    // filter by query (client-side; will be replaced later with backend filtering if needed)
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
      setSortDirection("desc"); // first click -> desc with "↓" per your request
    }
  }

  function showArrow(col: SortColumn) {
    if (sortColumn !== col) return null;
    // per request: first click sorts descending and shows "↓", second click ascending shows "↑"
    return sortDirection === "desc" ? "↓" : "↑";
  }

  function formatPriceValue(val: number | null | undefined) {
    if (val == null || Number.isNaN(val)) return "-";
    return `${Number(val).toFixed(2)} zł`;
  }

  function onPageSizeChange(newSize: number) {
    setPageSize(newSize);
    setPageNumber(1); // reset to first page when size changes
  }

  function goToPage(p: number) {
    if (p < 1) p = 1;
    if (p > totalPages) p = totalPages;
    setPageNumber(p);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  // small helper to render page buttons (shows a window of pages)
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
        <button className="btn btn-sm" onClick={() => goToPage(1)} disabled={pageNumber === 1}>
          «1
        </button>
        <button className="btn btn-sm" onClick={() => goToPage(pageNumber - 1)} disabled={pageNumber === 1}>
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

        <button className="btn btn-sm" onClick={() => goToPage(pageNumber + 1)} disabled={pageNumber === totalPages}>
          →
        </button>
        <button className="btn btn-sm" onClick={() => goToPage(totalPages)} disabled={pageNumber === totalPages}>
          {totalPages} »
        </button>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-base-200 text-base-content">
      <Header />

      <main className="mx-auto max-w-6xl px-4 py-8">
        <div className="flex items-center justify-between mb-6">
          <h1 className="text-2xl md:text-3xl font-bold">Lista stacji benzynowych</h1>
          <a href="/dashboard" className="btn btn-outline">
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

            <div className="flex items-center gap-4">
              <div className="text-sm text-gray-400">Znaleziono: {totalCount ?? (stations ? stations.length : "-")}</div>

              <div className="flex items-center gap-2 text-sm">
                <label>Stacji na stronę:</label>
                <select
                  className="select select-sm"
                  value={pageSize}
                  onChange={(e) => onPageSizeChange(Number(e.target.value))}
                >
                  {[10, 20, 30, 40, 50].map((n) => (
                    <option key={n} value={n}>
                      {n}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          </div>

          {loading ? (
            <div>Ładowanie listy stacji...</div>
          ) : error ? (
            <div className="text-red-400">{error}</div>
          ) : stations && stations.length > 0 ? (
            <>
              <div className="overflow-x-auto">
                <table className="table table-compact w-full">
                  <thead>
                    <tr>
                      <th className="cursor-pointer" onClick={() => toggleSort("name")}>
                        Nazwa {showArrow("name")}
                      </th>
                      <th className="cursor-pointer" onClick={() => toggleSort("benzyna")}>
                        Cena benzyny {showArrow("benzyna")}
                      </th>
                      <th className="cursor-pointer" onClick={() => toggleSort("diesel")}>
                        Cena diesel {showArrow("diesel")}
                      </th>
                      <th className="cursor-pointer" onClick={() => toggleSort("lpg")}>
                        Cena LPG {showArrow("lpg")}
                      </th>
                      <th className="cursor-pointer" onClick={() => toggleSort("distance")}>
                        Odległość {showArrow("distance")}
                      </th>
                      <th className="cursor-pointer" onClick={() => toggleSort("city")}>
                        Miasto {showArrow("city")}
                      </th>
                      <th className="cursor-pointer" onClick={() => toggleSort("street")}>
                        Ulica {showArrow("street")}
                      </th>
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
                            <a className="font-medium hover:underline cursor-pointer" onClick={() => toggleSort("name")}>
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

              <div className="mt-4 flex items-center justify-between">
                <div>{renderPageButtons()}</div>
                <div className="text-sm text-gray-400">
                  Strona {pageNumber} / {totalPages} — {totalCount ?? (stations ? stations.length : 0)} wyników
                </div>
              </div>
            </>
          ) : (
            <div className="text-gray-300">Brak dostępnych stacji.</div>
          )}
        </section>
      </main>

      <Footer />
    </div>
  );
}
