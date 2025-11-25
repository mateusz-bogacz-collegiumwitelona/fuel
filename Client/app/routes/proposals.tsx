import React from "react";
import Header from "../components/header";
import Footer from "../components/footer";

const API_BASE = "http://localhost:5111";

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
  fuelPrices?: Record<string, number | string> | null;
};

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
  } catch {
    return null;
  }
}

export default function ProposalPage() {
  const [mode, setMode] = React.useState<"add" | "change">("add");
  const [stations, setStations] = React.useState<Station[]>([]);
  const [loadingStations, setLoadingStations] = React.useState(false);
  const [selectedStationId, setSelectedStationId] = React.useState<string | null>(null);

  // Add-station form state
  const [newStation, setNewStation] = React.useState<Partial<Station>>({});

  // Propose-change form state
  const [proposalFuelCode, setProposalFuelCode] = React.useState<string>("PB95");
  const [proposalPrice, setProposalPrice] = React.useState<string>("");
  const [photoFile, setPhotoFile] = React.useState<File | null>(null);

  const [submitting, setSubmitting] = React.useState(false);
  const [message, setMessage] = React.useState<string | null>(null);
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    void fetchStations();
  }, []);

  async function fetchStations() {
    setLoadingStations(true);
    try {
      // reuse the /api/station/list endpoint like in list.tsx
      const body = { pagging: { pageNumber: 1, pageSize: 200 } };
      const res = await fetch(`${API_BASE}/api/station/list`, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        credentials: "include",
        body: JSON.stringify(body),
      });
      if (!res.ok) throw new Error(`Błąd pobierania stacji: ${res.status}`);
      const json = await res.json();
      const items = Array.isArray(json.items) ? json.items : Array.isArray(json) ? json : json.stations ?? [];
      // simple normalize
      const normalized = (items as any[]).map((s) => ({
        id: s.id ?? s.stationId,
        brandName: s.brandName ?? s.name ?? s.stationName,
        street: s.street ?? (s.address ? String(s.address).split(",")[0] : undefined),
        houseNumber: s.houseNumber ?? s.no,
        city: s.city ?? s.town,
        latitude: s.latitude ?? s.lat,
        longitude: s.longitude ?? s.lon ?? s.lng,
        imageUrl: s.imageUrl ?? s.image,
        fuelPrices: s.fuelPrices ?? s.prices ?? null,
      })) as Station[];

      setStations(normalized);
    } catch (e: any) {
      console.error(e);
      setError("Nie udało się pobrać listy stacji. Sprawdź backend/CORS.");
    } finally {
      setLoadingStations(false);
    }
  }

  function selectStation(id: string | null) {
    setSelectedStationId(id);
    setMessage(null);
    setError(null);
  }

  const selectedStation = React.useMemo(() => stations.find((s) => s.id === selectedStationId) ?? null, [stations, selectedStationId]);

  // --- handlers for Add Station ---
  function handleNewStationChange<K extends keyof Station>(k: K, v: Station[K]) {
    setNewStation((prev) => ({ ...prev, [k]: v }));
  }

  async function submitNewStation(e?: React.FormEvent) {
    e?.preventDefault();
    setSubmitting(true);
    setMessage(null);
    setError(null);

    try {
      // Endpoint for adding station might differ on your backend.
      // Try POST /api/admin/station or /api/station - adjust if needed.
      const token = localStorage.getItem("token");
      const headers: Record<string, string> = { "Content-Type": "application/json", Accept: "application/json" };
      if (token) headers["Authorization"] = `Bearer ${token}`;

      const payload = {
        brandName: newStation.brandName ?? null,
        name: newStation.name ?? null,
        street: newStation.street ?? null,
        houseNumber: newStation.houseNumber ?? null,
        postalCode: newStation.postalCode ?? null,
        city: newStation.city ?? null,
        latitude: newStation.latitude ?? null,
        longitude: newStation.longitude ?? null,
        imageUrl: newStation.imageUrl ?? null,
      };

      // try a couple of plausible endpoints
      const tryUrls = [`${API_BASE}/api/station/create`, `${API_BASE}/api/admin/station`, `${API_BASE}/api/station`];
      let ok = false;
      for (const url of tryUrls) {
        try {
          const res = await fetch(url, { method: "POST", headers, credentials: "include", body: JSON.stringify(payload) });
          if (res.ok) {
            ok = true;
            setMessage("Dodano propozycję stacji / wysłano żądanie dodania (zależnie od backendu).");
            await fetchStations();
            setNewStation({});
            break;
          } else {
            const txt = await res.text().catch(() => "");
            console.warn("add station failed for", url, res.status, txt);
          }
        } catch (err) {
          console.warn("fetch error for", url, err);
        }
      }

      if (!ok) setError("Nie udało się wysłać żądania dodania stacji — sprawdź konsolę i endpointy backendu.");
    } catch (err: any) {
      console.error(err);
      setError(err?.message ?? "Błąd serwera");
    } finally {
      setSubmitting(false);
    }
  }

  // --- handlers for Propose Change ---
  function handlePhotoChange(f?: File) {
    setPhotoFile(f ?? null);
  }

  async function submitProposal(e?: React.FormEvent) {
    e?.preventDefault();
    setSubmitting(true);
    setMessage(null);
    setError(null);

    if (!selectedStation) {
      setError("Wybierz stację, której dotyczy propozycja.");
      setSubmitting(false);
      return;
    }

    try {
      const token = localStorage.getItem("token");
      const headers: Record<string, string> = { Accept: "application/json" };
      if (token) headers["Authorization"] = `Bearer ${token}`;

      // The API may expect multipart/form-data for photo upload. We'll send FormData.
      const fd = new FormData();
      fd.append("stationId", String(selectedStation.id ?? ""));
      fd.append("brandName", String(selectedStation.brandName ?? ""));
      fd.append("street", String(selectedStation.street ?? ""));
      fd.append("houseNumber", String(selectedStation.houseNumber ?? ""));
      fd.append("city", String(selectedStation.city ?? ""));
      fd.append("fuelCode", proposalFuelCode ?? "PB95");
      fd.append("proposedPrice", proposalPrice ?? "");
      if (photoFile) fd.append("photo", photoFile);

      // Try likely endpoints
      const tryUrls = [`${API_BASE}/api/proposal`, `${API_BASE}/api/priceproposal`, `${API_BASE}/api/proposal/create`];
      let ok = false;
      for (const url of tryUrls) {
        try {
          const res = await fetch(url, { method: "POST", headers, credentials: "include", body: fd });
          if (res.ok) {
            ok = true;
            setMessage("Propozycja zmiany została wysłana.");
            setProposalPrice("");
            setPhotoFile(null);
            break;
          } else {
            const txt = await res.text().catch(() => "");
            console.warn("proposal POST failed for", url, res.status, txt);
          }
        } catch (err) {
          console.warn("fetch error for", url, err);
        }
      }

      if (!ok) setError("Nie udało się wysłać propozycji — sprawdź endpointy backendu.");
    } catch (err: any) {
      console.error(err);
      setError(err?.message ?? "Błąd serwera");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="min-h-screen bg-base-200 text-base-content">
      <Header />
      <main className="mx-auto max-w-4xl px-4 py-8">
        <h1 className="text-2xl font-bold mb-4">Dodaj stację / Zaproponuj zmianę</h1>

        <div className="flex gap-2 mb-6">
          <button className={`btn ${mode === "add" ? "btn-primary" : "btn-outline"}`} onClick={() => setMode("add")}>Dodaj stację</button>
          <button className={`btn ${mode === "change" ? "btn-primary" : "btn-outline"}`} onClick={() => setMode("change")}>Zaproponuj zmianę</button>
        </div>

        <div className="bg-base-300 rounded-xl p-4 shadow-md">
          {mode === "add" ? (
            <form onSubmit={submitNewStation} className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div>
                <label className="label"><span className="label-text">Nazwa (brand)</span></label>
                <input value={newStation.brandName ?? ""} onChange={(e) => handleNewStationChange("brandName", e.target.value)} className="input input-bordered w-full" />
              </div>

              <div>
                <label className="label"><span className="label-text">Nazwa stacji (opcjonalnie)</span></label>
                <input value={newStation.name ?? ""} onChange={(e) => handleNewStationChange("name", e.target.value)} className="input input-bordered w-full" />
              </div>

              <div>
                <label className="label"><span className="label-text">Ulica</span></label>
                <input value={newStation.street ?? ""} onChange={(e) => handleNewStationChange("street", e.target.value)} className="input input-bordered w-full" />
              </div>

              <div>
                <label className="label"><span className="label-text">Numer domu</span></label>
                <input value={String(newStation.houseNumber ?? "")} onChange={(e) => handleNewStationChange("houseNumber", e.target.value)} className="input input-bordered w-full" />
              </div>

              <div>
                <label className="label"><span className="label-text">Miasto</span></label>
                <input value={newStation.city ?? ""} onChange={(e) => handleNewStationChange("city", e.target.value)} className="input input-bordered w-full" />
              </div>

              <div>
                <label className="label"><span className="label-text">Kod pocztowy</span></label>
                <input value={newStation.postalCode ?? ""} onChange={(e) => handleNewStationChange("postalCode", e.target.value)} className="input input-bordered w-full" />
              </div>

              <div>
                <label className="label"><span className="label-text">Szer. geogr.</span></label>
                <input value={String(newStation.latitude ?? "")} onChange={(e) => handleNewStationChange("latitude", Number(e.target.value) || undefined)} className="input input-bordered w-full" />
              </div>

              <div>
                <label className="label"><span className="label-text">Dł. geogr.</span></label>
                <input value={String(newStation.longitude ?? "")} onChange={(e) => handleNewStationChange("longitude", Number(e.target.value) || undefined)} className="input input-bordered w-full" />
              </div>

              <div className="md:col-span-2">
                <label className="label"><span className="label-text">URL zdjęcia (opcjonalnie)</span></label>
                <input value={newStation.imageUrl ?? ""} onChange={(e) => handleNewStationChange("imageUrl", e.target.value)} className="input input-bordered w-full" />
              </div>

              <div className="md:col-span-2 flex gap-2 justify-end">
                <button type="submit" className="btn btn-primary" disabled={submitting}>{submitting ? "Wysyłanie..." : "Wyślij propozycję"}</button>
                <button type="button" className="btn btn-ghost" onClick={() => setNewStation({})}>Wyczyść</button>
              </div>

              <div className="md:col-span-2">
                {message && <div className="alert alert-success">{message}</div>}
                {error && <div className="alert alert-error">{error}</div>}
              </div>
            </form>
          ) : (
            // --- propose change mode ---
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="md:col-span-2">
                <label className="label"><span className="label-text">Wybierz stację</span></label>
                {loadingStations ? (
                  <div>Ładowanie...</div>
                ) : (
                  <select className="select select-bordered w-full" value={selectedStationId ?? ""} onChange={(e) => selectStation(e.target.value || null)}>
                    <option value="">-- wybierz stację --</option>
                    {stations.map((s) => (
                      <option key={s.id} value={s.id}>{`${s.brandName ?? s.name ?? "-"} — ${s.street ?? ""} ${s.houseNumber ?? ""} ${s.city ?? ""}`}</option>
                    ))}
                  </select>
                )}

                <div className="mt-3">
                  <label className="label"><span className="label-text">Typ paliwa</span></label>
                  <select className="select select-bordered w-full" value={proposalFuelCode} onChange={(e) => setProposalFuelCode(e.target.value)}>
                    <option>PB95</option>
                    <option>PB98</option>
                    <option>ON</option>
                    <option>LPG</option>
                    <option>E85</option>
                  </select>
                </div>

                <div className="mt-3">
                  <label className="label"><span className="label-text">Proponowana cena (PLN)</span></label>
                  <input className="input input-bordered w-full" value={proposalPrice} onChange={(e) => setProposalPrice(e.target.value)} placeholder="np. 6.99" />
                </div>

                <div className="mt-3">
                  <label className="label"><span className="label-text">Zdjęcie weryfikacyjne</span></label>
                  <input type="file" accept="image/*" onChange={(e) => handlePhotoChange(e.target.files?.[0])} />
                </div>

                <div className="mt-4 flex gap-2">
                  <button className="btn btn-primary" onClick={submitProposal} disabled={submitting}>{submitting ? "Wysyłanie..." : "Wyślij propozycję zmiany"}</button>
                  <button className="btn btn-ghost" onClick={() => { setProposalPrice(""); setPhotoFile(null); }}>Wyczyść</button>
                </div>

                <div className="mt-3">
                  {message && <div className="alert alert-success">{message}</div>}
                  {error && <div className="alert alert-error">{error}</div>}
                </div>
              </div>

              <aside className="p-3 bg-base-200 rounded-md">
                <h3 className="font-semibold mb-2">Aktualne wartości stacji</h3>
                {selectedStation ? (
                  <div className="text-sm space-y-1">
                    <div><strong>Marka:</strong> {selectedStation.brandName ?? "-"}</div>
                    <div><strong>Ulica:</strong> {selectedStation.street ?? "-"} {selectedStation.houseNumber ?? ""}</div>
                    <div><strong>Miasto:</strong> {selectedStation.city ?? "-"}</div>
                    <div><strong>Koordynaty:</strong> {selectedStation.latitude ?? "-"}, {selectedStation.longitude ?? "-"}</div>

                    <div className="mt-2">
                      <strong>Ceny paliw (jeśli dostępne):</strong>
                      <ul className="list-disc ml-5 text-sm">
                        {selectedStation.fuelPrices && Object.keys(selectedStation.fuelPrices).length > 0 ? (
                          Object.entries(selectedStation.fuelPrices).map(([k, v]) => (
                            <li key={k}>{k}: {typeof v === 'number' ? v.toFixed(2) : String(v)}</li>
                          ))
                        ) : (
                          <li>- brak danych -</li>
                        )}
                      </ul>
                    </div>

                    {selectedStation.imageUrl && (
                      <div className="mt-2">
                        <img src={selectedStation.imageUrl} alt="stacja" className="w-full rounded-md shadow-sm" />
                      </div>
                    )}
                  </div>
                ) : (
                  <div className="text-sm text-gray-400">Wybierz stację, żeby zobaczyć jej bieżące wartości.</div>
                )}
              </aside>
            </div>
          )}
        </div>
      </main>
      <Footer />
    </div>
  );
}
