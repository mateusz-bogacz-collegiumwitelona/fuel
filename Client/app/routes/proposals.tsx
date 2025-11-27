import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";
import { useNavigate } from "react-router";


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

type ProposalType = "add" | "edit";

type FuelRow = { fuelCode: string; price: string };

type StationIdentifier = {
  brandName?: string;
  street?: string;
  houseNumber?: string;
  city?: string;
};

type StationProfile = {
  brandName?: string;
  street?: string;
  houseNumber?: string | number;
  city?: string;
  postalCode?: string;
  latitude?: number;
  longitude?: number;
  fuelPrice?: { fuelCode: string; price: number; validFrom?: string }[];
};

// all fuels (dropdown)
const AVAILABLE_FUEL_TYPES = ["PB95", "PB98", "ON", "LPG", "E85"];

export default function ProposalPage() {
  const navigate = useNavigate();

  const [userEmail, setUserEmail] = React.useState<string | null>(null);

  const [type, setType] = React.useState<ProposalType>("add");

  // Fields for 'add' proposal (and also used to propose new values for edit)
  const [brandName, setBrandName] = React.useState("");
  const [street, setStreet] = React.useState("");
  const [houseNumber, setHouseNumber] = React.useState<string | number>("");
  const [city, setCity] = React.useState("");
  const [postalCode, setPostalCode] = React.useState("");
  const [latitude, setLatitude] = React.useState<string | number>("");
  const [longitude, setLongitude] = React.useState<string | number>("");

  // Fuel rows for proposed prices
  const [fuelRows, setFuelRows] = React.useState<FuelRow[]>([{ fuelCode: "PB95", price: "" }]);

  // For 'edit' proposals: choose existing station from dropdown
  const [stationsDropdown, setStationsDropdown] = React.useState<StationProfile[]>([]);
  const [selectedStationIndex, setSelectedStationIndex] = React.useState<number | null>(null);
  const [fetchedStation, setFetchedStation] = React.useState<StationProfile | null>(null);

  // Address autocomplete / geocoding for adding station (users prefer address input)
  const [address, setAddress] = React.useState<string>("");
  const [addressSuggestions, setAddressSuggestions] = React.useState<Array<{ display_name: string; lat: string; lon: string }>>([]);
  const [addressLoading, setAddressLoading] = React.useState<boolean>(false);
  const geocodeTimeoutRef = React.useRef<number | null>(null);

  const [fetchingStations, setFetchingStations] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [submitting, setSubmitting] = React.useState(false);
  const [successMsg, setSuccessMsg] = React.useState<string | null>(null);

  React.useEffect(() => {
    const token = localStorage.getItem("token");
    const decoded = parseJwt(token);
    const email = decoded?.email || decoded?.sub || null;
    if (email) setUserEmail(email);

    // load initial dropdown (first page, up to 100)
    loadStationsForDropdown();
  }, []);

  async function loadStationsForDropdown(brandFilter?: string) {
    setFetchingStations(true);
    setError(null);

    try {
      const token = localStorage.getItem("token");
      const body: any = {
        locationLatitude: null,
        locationLongitude: null,
        distance: null,
        fuelType: null,
        minPrice: null,
        maxPrice: null,
        brandName: brandFilter || null,
        sortingByDisance: null,
        sortingByPrice: null,
        sortingDirection: null,
        pagging: { pageNumber: 1, pageSize: 100 },
      };

      const res = await fetch(`${API_BASE}/api/station/list`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        credentials: "include",
        body: JSON.stringify(body),
      });

      if (!res.ok) {
        const txt = await res.text();
        throw new Error(`Błąd pobierania listy stacji: ${res.status} - ${txt}`);
      }

      const data = await res.json();
      const items = Array.isArray(data.items) ? data.items : (Array.isArray(data.data) ? data.data : []);
      setStationsDropdown(items);
    } catch (e: any) {
      console.error(e);
      setError(e?.message ?? "Nie udało się załadować listy stacji");
      setStationsDropdown([]);
    } finally {
      setFetchingStations(false);
    }
  }

  function addFuelRow() {
    setFuelRows((p) => [...p, { fuelCode: "", price: "" }]);
  }
  function removeFuelRow(idx: number) {
    setFuelRows((p) => p.filter((_, i) => i !== idx));
  }
  function updateFuelRow(idx: number, row: Partial<FuelRow>) {
    setFuelRows((p) => p.map((r, i) => (i === idx ? { ...r, ...row } : r)));
  }

  // Geocoding (Nominatim) for address -> lat/lon
  async function geocodeAddress(query: string) {
    if (!query || query.trim().length === 0) {
      setAddressSuggestions([]);
      return;
    }
    setAddressLoading(true);
    try {
      const res = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&addressdetails=0&limit=5`);
      if (!res.ok) return;
      const json = await res.json();
      setAddressSuggestions(Array.isArray(json) ? json : []);
    } catch (e) {
      console.error("Geocode error", e);
    } finally {
      setAddressLoading(false);
    }
  }

  function selectAddress(suggestion: any) {
    setAddress(suggestion.display_name || "");
    setLatitude(parseFloat(suggestion.lat));
    setLongitude(parseFloat(suggestion.lon));
    setAddressSuggestions([]);
  }

  function handleAddressChange(v: string) {
    setAddress(v);
    if (geocodeTimeoutRef.current) window.clearTimeout(geocodeTimeoutRef.current);
    geocodeTimeoutRef.current = window.setTimeout(() => geocodeAddress(v), 500);
  }

  function onSelectExistingStation(idxStr: string) {
    if (!idxStr) {
      setSelectedStationIndex(null);
      setFetchedStation(null);
      return;
    }
    const idx = Number(idxStr);
    const st = stationsDropdown[idx];
    if (!st) return;
    setSelectedStationIndex(idx);
    setFetchedStation(st);

    // Prefill proposed values with current station values (user can change them)
    setBrandName(st.brandName ?? "");
    setStreet(st.street ?? "");
    setHouseNumber(st.houseNumber ?? "");
    setCity(st.city ?? "");
    setPostalCode(st.postalCode ?? "");
    setLatitude(st.latitude ?? "");
    setLongitude(st.longitude ?? "");
    if (Array.isArray(st.fuelPrice)) {
      setFuelRows(st.fuelPrice.map(f => ({ fuelCode: f.fuelCode, price: String(f.price) })));
    }
  }

  function validateForm() {
    if (type === "edit") {
      if (selectedStationIndex === null) {
        setError("Wybierz stację z listy, której dotyczy zmiana.");
        return false;
      }
    }

    if (!brandName || !street || !city) {
      setError("Uzupełnij przynajmniej markę, ulicę i miasto proponowanej stacji.");
      return false;
    }

    for (const r of fuelRows) {
      if (!r.fuelCode) continue;
      if (r.price && isNaN(Number(r.price))) {
        setError(`Nieprawidłowa cena paliwa: ${r.fuelCode || "(brak kodu)"}`);
        return false;
      }
    }

    return true;
  }

  async function handleSubmit(e?: React.FormEvent) {
    if (e) e.preventDefault();
    setError(null);
    setSuccessMsg(null);

    if (!validateForm()) return;

    setSubmitting(true);

    try {
      const token = localStorage.getItem("token");
      const payload: any = {
        type,
        createdBy: userEmail || null,
        proposed: {
          brandName: brandName || undefined,
          street: street || undefined,
          houseNumber: houseNumber || undefined,
          city: city || undefined,
          postalCode: postalCode || undefined,
          latitude: latitude ? Number(latitude) : undefined,
          longitude: longitude ? Number(longitude) : undefined,
          fuelTypes: fuelRows
            .filter((r) => r.fuelCode)
            .map((r) => ({ code: r.fuelCode, price: r.price ? Number(r.price) : null })),
        },
      };

      if (type === "edit") {
        const target = stationsDropdown[selectedStationIndex as number];
        payload.target = {
          brandName: target.brandName,
          street: target.street,
          houseNumber: target.houseNumber,
          city: target.city,
        };
      }

      const res = await fetch(`${API_BASE}/api/proposal/create`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        credentials: "include",
        body: JSON.stringify(payload),
      });

      if (!res.ok) {
        const txt = await res.text();
        throw new Error(`Błąd serwera: ${res.status} - ${txt}`);
      }

      await res.json();
      setSuccessMsg("Propozycja zapisana. Dziękujemy — administracja ją przejrzy.");
      setFuelRows([{ fuelCode: "PB95", price: "" }]);
    } catch (e: any) {
      console.error(e);
      setError(e?.message ?? "Nie udało się wysłać propozycji.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="min-h-screen bg-base-200 text-base-content">
      <Header />

      <main className="mx-auto max-w-4xl px-4 py-8">
        <div className="flex items-center justify-between mb-6">
          <h1 className="text-2xl md:text-3xl font-bold">Zgłoś propozycję stacji lub zmian na niej</h1>
          <button className="btn btn-outline btn-sm" onClick={() => navigate(-1)}>← Powrót</button>
        </div>

        <section className="bg-base-300 rounded-xl p-6 shadow-md mb-6">
          <p className="text-sm text-base-content/70">Wybierz czy chcesz <strong>dodać nową stację</strong> czy <strong>zgłosić zmianę</strong> dla istniejącej.</p>

          <div className="mt-4 flex gap-3">
            <label className={`btn ${type === "add" ? "btn-primary" : "btn-outline"}`}>
              <input type="radio" name="ptype" checked={type === "add"} onChange={() => { setType("add"); setSelectedStationIndex(null); setFetchedStation(null); }} className="hidden" />
              Dodaj stację
            </label>
            <label className={`btn ${type === "edit" ? "btn-primary" : "btn-outline"}`}>
              <input type="radio" name="ptype" checked={type === "edit"} onChange={() => setType("edit")} className="hidden" />
              Zmień istniejącą
            </label>
          </div>
        </section>

        {type === "edit" && (
          <section className="bg-base-300 rounded-xl p-6 shadow-md mb-6">
            <h2 className="font-semibold mb-3">Wybierz stację z listy (możesz filtrować po marce)</h2>

            <div className="flex gap-2 mb-3">
              <input className="input input-bordered" placeholder="Filtruj po marce (np. Orlen)" onChange={(e) => { loadStationsForDropdown(e.target.value); }} />
              <button className="btn" onClick={() => loadStationsForDropdown()}>Odśwież listę</button>
            </div>

            <div className="mb-3">
              {fetchingStations ? (
                <div>Ładowanie listy stacji...</div>
              ) : (
                <select
                  className="select select-bordered w-full"
                  value={selectedStationIndex !== null ? String(selectedStationIndex) : ""}
                  onChange={(e) => onSelectExistingStation(e.target.value)}
                >
                  <option value="">-- Wybierz stację --</option>
                  {stationsDropdown.map((s, idx) => (
                    <option key={`${s.brandName}-${s.city}-${s.street}-${s.houseNumber}-${idx}`} value={String(idx)}>
                      {s.brandName} — {s.city}, {s.street} {s.houseNumber}
                    </option>
                  ))}
                </select>
              )}
            </div>

            {fetchedStation && (
              <div className="mt-4 bg-base-100 p-3 rounded-md">
                <h3 className="font-semibold">Dotychczasowe dane</h3>
                <p className="text-sm">{fetchedStation.brandName} — {fetchedStation.city}, {fetchedStation.street} {fetchedStation.houseNumber}</p>
                <p className="text-sm text-base-content/70">Kod pocztowy: {fetchedStation.postalCode ?? '-'}</p>

                <div className="mt-3">
                  <h4 className="font-medium">Ceny paliw (ostatnie)</h4>
                  {fetchedStation.fuelPrice && fetchedStation.fuelPrice.length > 0 ? (
                    <table className="table w-full mt-2">
                      <thead><tr><th>Kod</th><th>Cena</th><th>Ważne od</th></tr></thead>
                      <tbody>
                        {fetchedStation.fuelPrice.map((fp, i) => (
                          <tr key={i}><td>{fp.fuelCode}</td><td>{Number(fp.price).toFixed(2)}</td><td>{fp.validFrom ? new Date(fp.validFrom).toLocaleString() : '-'}</td></tr>
                        ))}
                      </tbody>
                    </table>
                  ) : (
                    <p className="text-sm text-base-content/70">Brak danych o cenach.</p>
                  )}
                </div>
              </div>
            )}
          </section>
        )}

        <form onSubmit={handleSubmit} className="bg-base-300 rounded-xl p-6 shadow-md mb-6">
          <h2 className="font-semibold mb-3">Twoja propozycja (proponowane wartości)</h2>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <input className="input input-bordered" placeholder="Marka" value={brandName} onChange={(e) => setBrandName(e.target.value)} />
            <input className="input input-bordered" placeholder="Miasto" value={city} onChange={(e) => setCity(e.target.value)} />
            <input className="input input-bordered" placeholder="Ulica" value={street} onChange={(e) => setStreet(e.target.value)} />
            <input className="input input-bordered" placeholder="Nr domu" value={String(houseNumber)} onChange={(e) => setHouseNumber(e.target.value)} />
            <input className="input input-bordered" placeholder="Kod pocztowy" value={postalCode} onChange={(e) => setPostalCode(e.target.value)} />

            <div className="md:col-span-2">
              <label className="block text-sm font-medium mb-2">Adres (opcjonalnie — wpisz aby pobrać współrzędne)</label>
              <div className="relative">
                <input type="text" placeholder="Wpisz adres (np. Warszawa, Marszałkowska 1)" value={address} onChange={(e) => handleAddressChange(e.target.value)} className="input input-bordered w-full" />
                {addressLoading && <span className="loading loading-spinner loading-sm absolute right-2 top-2"></span>}
                {addressSuggestions.length > 0 && (
                  <ul className="absolute z-50 bg-base-100 w-full mt-1 rounded-md shadow-lg max-h-52 overflow-auto">
                    {addressSuggestions.map((s, i) => (
                      <li key={i} className="p-2 hover:bg-base-200 cursor-pointer" onClick={() => selectAddress(s)}>{s.display_name}</li>
                    ))}
                  </ul>
                )}
              </div>
              <div className="mt-2 grid grid-cols-2 gap-3">
                <input className="input input-bordered" placeholder="Szer. geogr. (latitude)" value={String(latitude)} onChange={(e) => setLatitude(e.target.value)} />
                <input className="input input-bordered" placeholder="Dł. geogr. (longitude)" value={String(longitude)} onChange={(e) => setLongitude(e.target.value)} />
              </div>
            </div>
          </div>

          <div className="mt-4">
            <h3 className="font-medium mb-2">Proponowane ceny / paliwa</h3>
            <div className="space-y-2">
              {fuelRows.map((r, idx) => (
                <div key={idx} className="flex gap-2 items-center">
                  <select className="select select-bordered w-1/2" value={r.fuelCode} onChange={(e) => updateFuelRow(idx, { fuelCode: e.target.value })}>
                    <option value="">-- wybierz paliwo --</option>
                    {AVAILABLE_FUEL_TYPES.map((ft) => (
                      <option key={ft} value={ft}>{ft}</option>
                    ))}
                  </select>

                  <input className="input input-bordered w-1/2" placeholder="Cena (zł)" value={r.price} onChange={(e) => updateFuelRow(idx, { price: e.target.value })} />
                  <button type="button" className="btn btn-sm btn-outline" onClick={() => removeFuelRow(idx)}>usuń</button>
                </div>
              ))}
            </div>
            <div className="mt-2">
              <button type="button" className="btn btn-sm" onClick={addFuelRow}>+ Dodaj paliwo / cenę</button>
            </div>
          </div>

          {error && <div className="mt-4 alert alert-error">{error}</div>}
          {successMsg && <div className="mt-4 alert alert-success">{successMsg}</div>}

          <div className="mt-6 flex gap-2">
            <button className="btn btn-primary" type="submit" disabled={submitting}>{submitting ? 'Wysyłanie...' : 'Wyślij propozycję'}</button>
            <button type="button" className="btn btn-outline" onClick={() => {
              setBrandName(""); setStreet(""); setHouseNumber(""); setCity(""); setPostalCode(""); setLatitude(""); setLongitude(""); setFuelRows([{ fuelCode: 'PB95', price: '' }]);
            }}>Wyczyść</button>
          </div>
        </form>

      </main>

      <Footer />
    </div>
  );
}
