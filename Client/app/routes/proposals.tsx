import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";
import { useNavigate } from "react-router";
import { API_BASE } from "../components/api";
import { useTranslation } from "react-i18next";

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

type FuelRow = { fuelCode: string; price: string };
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

const AVAILABLE_FUEL_TYPES = ["PB95", "PB98", "ON", "LPG", "E85"];

export default function ProposalPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const [userEmail, setUserEmail] = React.useState<string | null>(null);

  const [brandName, setBrandName] = React.useState("");
  const [street, setStreet] = React.useState("");
  const [houseNumber, setHouseNumber] = React.useState<string | number>("");
  const [city, setCity] = React.useState("");
  const [postalCode, setPostalCode] = React.useState("");
  const [latitude, setLatitude] = React.useState<string | number>("");
  const [longitude, setLongitude] = React.useState<string | number>("");

  const [fuelRows, setFuelRows] = React.useState<FuelRow[]>([{ fuelCode: "PB95", price: "" }]);

  // single photo for all prices
  const [globalFile, setGlobalFile] = React.useState<File | null>(null);
  const [globalPreview, setGlobalPreview] = React.useState<string | null>(null);

  // edit dropdown stuff
  const [stationsDropdown, setStationsDropdown] = React.useState<StationProfile[]>([]);
  const [selectedStationIndex, setSelectedStationIndex] = React.useState<number | null>(null);
  const [fetchedStation, setFetchedStation] = React.useState<StationProfile | null>(null);

  // address autocomplete
  const [address, setAddress] = React.useState<string>("");
  const [addressSuggestions, setAddressSuggestions] = React.useState<
    Array<{ display_name: string; lat: string; lon: string }>
  >([]);
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
    loadStationsForDropdown();
    // eslint-disable-next-line react-hooks/exhaustive-deps
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
        throw new Error(
          `${t("proposal.error_loading_stations")}: ${res.status} - ${txt || t("proposal.no_content")}`
        );
      }

      const data = await res.json();
      const items = Array.isArray(data.items) ? data.items : Array.isArray(data.data) ? data.data : [];
      setStationsDropdown(items);
    } catch (e: any) {
      console.error(e);
      setError(e?.message ?? t("proposal.error_loading_stations_fallback"));
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

  // handle single global file
  function handleGlobalFileChange(f: File | null) {
    setGlobalFile(f);
    if (globalPreview) {
      URL.revokeObjectURL(globalPreview);
      setGlobalPreview(null);
    }
    if (f) {
      const url = URL.createObjectURL(f);
      setGlobalPreview(url);
    }
  }

  React.useEffect(() => {
    return () => {
      if (globalPreview) {
        URL.revokeObjectURL(globalPreview);
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Geocoding
  async function geocodeAddress(query: string) {
    if (!query || query.trim().length === 0) {
      setAddressSuggestions([]);
      return;
    }
    setAddressLoading(true);
    try {
      const res = await fetch(
        `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(
          query
        )}&addressdetails=0&limit=5`
      );
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

    setBrandName(st.brandName ?? "");
    setStreet(st.street ?? "");
    setHouseNumber(st.houseNumber ?? "");
    setCity(st.city ?? "");
    setPostalCode(st.postalCode ?? "");
    setLatitude(st.latitude ?? "");
    setLongitude(st.longitude ?? "");
    if (Array.isArray(st.fuelPrice)) {
      setFuelRows(st.fuelPrice.map((f) => ({ fuelCode: f.fuelCode, price: String(f.price) })));
    }
  }

  function validateForm() {
    // page only supports proposing changes for existing stations
    if (selectedStationIndex === null) {
      setError(t("proposal.error_select_station"));
      return false;
    }

    if (!brandName || !street || !city || !houseNumber) {
      setError(t("proposal.error_fill_required"));
      return false;
    }

    const activeRows = fuelRows.filter((r) => r.fuelCode);
    if (activeRows.length === 0) {
      setError(t("proposal.error_no_price"));
      return false;
    }

    for (const [i, r] of fuelRows.entries()) {
      if (!r.fuelCode) continue;
      if (!r.price || isNaN(Number(r.price)) || Number(r.price) <= 0) {
        setError(t("proposal.error_invalid_price_row", { row: i + 1, value: r.price }));
        return false;
      }
    }

    // Require at least one photo (globalFile). Per request we'll attach the same photo for all prices.
    if (!globalFile) {
      setError(t("proposal.error_no_photo"));
      return false;
    }

    // client-side file checks
    if (globalFile.size > 5 * 1024 * 1024) {
      setError(t("proposal.error_file_too_large"));
      return false;
    }
    if (!["image/jpeg", "image/jpg", "image/png", "image/webp"].includes(globalFile.type)) {
      setError(t("proposal.error_bad_file_type"));
      return false;
    }

    return true;
  }

  // Now we create one request per fuel row, but attach the same globalFile to each request.
  async function handleSubmit(e?: React.FormEvent) {
    if (e) e.preventDefault();
    setError(null);
    setSuccessMsg(null);

    if (!validateForm()) return;

    setSubmitting(true);

    try {
      const token = localStorage.getItem("token");

      // build array of active rows with index so we can map results clearly
      const activeRows = fuelRows.map((r, i) => ({ ...r, index: i })).filter((r) => r.fuelCode);

      // prepare target identification (station selected)
      let targetStation: any = null;
      if (selectedStationIndex !== null) {
        targetStation = stationsDropdown[selectedStationIndex];
      }

      const requests = activeRows.map((row) => {
        const fd = new FormData();
        fd.append("BrandName", brandName);
        fd.append("Street", street);
        fd.append("HouseNumber", String(houseNumber));
        fd.append("City", city);

        fd.append("FuelTypeCode", row.fuelCode);

        // Normalize price
        const normalized = String(row.price).replace(",", ".");
        const priceNum = Number(normalized);
        fd.append("ProposedPrice", String(priceNum));

        // attach same photo for all rows
        if (globalFile) fd.append("Photo", globalFile, globalFile.name);

        // target identification for edit
        if (targetStation) {
          if (targetStation.brandName) fd.append("TargetBrandName", String(targetStation.brandName));
          if (targetStation.street) fd.append("TargetStreet", String(targetStation.street));
          if (targetStation.houseNumber !== undefined)
            fd.append("TargetHouseNumber", String(targetStation.houseNumber));
          if (targetStation.city) fd.append("TargetCity", String(targetStation.city));
          const possibleIdKeys = ["id", "stationId", "station_id", "StationId", "Id"];
          for (const k of possibleIdKeys) {
            if (k in targetStation && (targetStation as any)[k] != null) {
              fd.append("TargetStationId", String((targetStation as any)[k]));
              break;
            }
          }
        }

        for (const pair of fd.entries()) {
          if (pair[1] instanceof File) {
            console.log("FORMDATA:", pair[0], (pair[1] as File).name);
          } else {
            console.log("FORMDATA:", pair[0], pair[1]);
          }
        }

        return fetch(`${API_BASE}/api/station/price-proposal/add`, {
          method: "POST",
          headers: {
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
            Accept: "application/json",
          },
          credentials: "include",
          body: fd,
        }).then(async (res) => {
          const text = await res.text();
          let parsed: any = null;
          try {
            parsed = JSON.parse(text);
          } catch {}
          if (!res.ok) {
            if (parsed && parsed.errors && Array.isArray(parsed.errors) && parsed.errors.length > 0) {
              const joined = parsed.errors.join("; ");
              const msg = `${parsed.message ?? `HTTP ${res.status}`}: ${joined}`;
              throw new Error(msg);
            }
            const fallback = parsed?.message || text || `HTTP ${res.status}`;
            throw new Error(fallback);
          }
          return parsed ?? text;
        });
      });

      const settled = await Promise.allSettled(requests);

      const successes: string[] = [];
      const failures: string[] = [];

      settled.forEach((r, i) => {
        const row = activeRows[i];
        const code = row.fuelCode;
        if (r.status === "fulfilled") {
          const val = r.value;
          if (val && typeof val === "object" && val.message) {
            successes.push(`${code}: ${val.message}`);
          } else {
            successes.push(`${code}: OK`);
          }
        } else {
          const reason = r.reason;
          const message = reason?.message ?? String(reason) ?? t("proposal.unknown_error");
          failures.push(`${code}: ${message}`);
        }
      });

      if (failures.length === 0) {
        setSuccessMsg(t("proposal.success_all_sent"));
        setFuelRows([{ fuelCode: "PB95", price: "" }]);
        if (globalPreview) {
          URL.revokeObjectURL(globalPreview);
          setGlobalPreview(null);
        }
        setGlobalFile(null);
      } else {
        const finalMsg =
          `${successes.length > 0 ? t("proposal.successes") + ":\n- " + successes.join("\n- ") + "\n\n" : ""}` +
          `${t("proposal.errors")}:\n- ${failures.join("\n- ")}`;
        setError(finalMsg);
      }
    } catch (e: any) {
      console.error(e);
      setError(e?.message ?? t("proposal.error_sending_fallback"));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="min-h-screen bg-base-200 text-base-content">
      <Header />

      <main className="mx-auto max-w-4xl px-4 py-8">
        <div className="flex items-center justify-between mb-6">
          <h1 className="text-2xl md:text-3xl font-bold">{t("proposal.proposaladd")}</h1>
          <button className="btn btn-outline btn-sm" onClick={() => navigate(-1)}>
            {t("proposal.back")}
          </button>
        </div>

        {/* station selector — page only supports proposing changes */}
        <section className="bg-base-300 rounded-xl p-6 shadow-md mb-6">
          <h2 className="font-semibold mb-3">{t("proposal.select_station_title")}</h2>

          <div className="flex gap-2 mb-3">
            <input
              className="input input-bordered"
              placeholder={t("proposal.filter_brand_placeholder")}
              onChange={(e) => {
                loadStationsForDropdown(e.target.value);
              }}
            />
            <button className="btn" onClick={() => loadStationsForDropdown()}>
              {t("proposal.refresh_list")}
            </button>
          </div>

          <div className="mb-3">
            {fetchingStations ? (
              <div>{t("proposal.loading_stations")}</div>
            ) : (
              <select
                className="select select-bordered w-full"
                value={selectedStationIndex !== null ? String(selectedStationIndex) : ""}
                onChange={(e) => onSelectExistingStation(e.target.value)}
              >
                <option value="">-- {t("proposal.choose_station_option")} --</option>
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
              <h3 className="font-semibold">{t("proposal.station_data_title")}</h3>
              <p className="text-sm">
                {fetchedStation.brandName} — {fetchedStation.city}, {fetchedStation.street} {fetchedStation.houseNumber}
              </p>
              <p className="text-sm text-base-content/70">
                {t("proposal.postalcode_label")} {fetchedStation.postalCode ?? "-"}
              </p>

              <div className="mt-3">
                <h4 className="font-medium">{t("proposal.prices_last")}</h4>
                {fetchedStation.fuelPrice && fetchedStation.fuelPrice.length > 0 ? (
                  <table className="table w-full mt-2">
                    <thead>
                      <tr>
                        <th>{t("proposal.code")}</th>
                        <th>{t("proposal.price")}</th>
                        <th>{t("proposal.valid_from")}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {fetchedStation.fuelPrice.map((fp, i) => (
                        <tr key={i}>
                          <td>{fp.fuelCode}</td>
                          <td>{Number(fp.price).toFixed(2)}</td>
                          <td>{fp.validFrom ? new Date(fp.validFrom).toLocaleString() : "-"}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                ) : (
                  <p className="text-sm text-base-content/70">{t("proposal.no_price_data")}</p>
                )}
              </div>
            </div>
          )}
        </section>

        <form onSubmit={handleSubmit} className="bg-base-300 rounded-xl p-6 shadow-md mb-6">
          <h2 className="font-semibold mb-3">{t("proposal.form_title")}</h2>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <input
              className="input input-bordered"
              placeholder={t("proposal.brand_placeholder")}
              value={brandName}
              onChange={(e) => setBrandName(e.target.value)}
            />
            <input
              className="input input-bordered"
              placeholder={t("proposal.city_placeholder")}
              value={city}
              onChange={(e) => setCity(e.target.value)}
            />
            <input
              className="input input-bordered"
              placeholder={t("proposal.street_placeholder")}
              value={street}
              onChange={(e) => setStreet(e.target.value)}
            />
            <input
              className="input input-bordered"
              placeholder={t("proposal.house_placeholder")}
              value={String(houseNumber)}
              onChange={(e) => setHouseNumber(e.target.value)}
            />
            <input
              className="input input-bordered"
              placeholder={t("proposal.postalcode_placeholder")}
              value={postalCode}
              onChange={(e) => setPostalCode(e.target.value)}
            />

            <div className="md:col-span-2">
              <label className="block text-sm font-medium mb-2">{t("proposal.address_label")}</label>
              <div className="relative">
                <input
                  type="text"
                  placeholder={t("proposal.address_placeholder")}
                  value={address}
                  onChange={(e) => handleAddressChange(e.target.value)}
                  className="input input-bordered w-full"
                />
                {addressLoading && <span className="loading loading-spinner loading-sm absolute right-2 top-2"></span>}
                {addressSuggestions.length > 0 && (
                  <ul className="absolute z-50 bg-base-100 w-full mt-1 rounded-md shadow-lg max-h-52 overflow-auto">
                    {addressSuggestions.map((s, i) => (
                      <li key={i} className="p-2 hover:bg-base-200 cursor-pointer" onClick={() => selectAddress(s)}>
                        {s.display_name}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
              <div className="mt-2 grid grid-cols-2 gap-3">
                <input
                  className="input input-bordered"
                  placeholder={t("proposal.latitude_placeholder")}
                  value={String(latitude)}
                  onChange={(e) => setLatitude(e.target.value)}
                />
                <input
                  className="input input-bordered"
                  placeholder={t("proposal.longitude_placeholder")}
                  value={String(longitude)}
                  onChange={(e) => setLongitude(e.target.value)}
                />
              </div>
            </div>
          </div>

          {/* single photo for all prices */}
          <div className="mt-4">
            <label className="block text-sm mb-1">{t("proposal.photo_label")}</label>
            <div className="text-sm text-base-content/70 mb-1">{t("proposal.photo_help")}</div>
            <input
              type="file"
              accept="image/jpeg,image/jpg,image/png,image/webp"
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
                const f = e.target.files && e.target.files[0] ? e.target.files[0] : null;
                handleGlobalFileChange(f);
              }}
            />
            {globalPreview && (
              <div className="mt-2">
                <img src={globalPreview} alt="preview" className="max-w-xs max-h-40 rounded-md shadow-sm" />
              </div>
            )}
          </div>

          <div className="mt-4">
            <h3 className="font-medium mb-2">{t("proposal.proposed_prices_title")}</h3>
            <div className="space-y-2">
              {fuelRows.map((r, idx) => (
                <div key={idx} className="flex gap-2 items-center">
                  <select
                    className="select select-bordered w-1/2"
                    value={r.fuelCode}
                    onChange={(e) => updateFuelRow(idx, { fuelCode: e.target.value })}
                  >
                    <option value="">{t("proposal.select_fuel_placeholder")}</option>
                    {AVAILABLE_FUEL_TYPES.map((ft) => (
                      <option key={ft} value={ft}>
                        {ft}
                      </option>
                    ))}
                  </select>

                  <input
                    className="input input-bordered w-1/2"
                    placeholder={t("proposal.price_placeholder")}
                    value={r.price}
                    onChange={(e) => updateFuelRow(idx, { price: e.target.value })}
                  />
                  <button type="button" className="btn btn-sm btn-outline" onClick={() => removeFuelRow(idx)}>
                    {t("proposal.remove_button")}
                  </button>
                </div>
              ))}
            </div>
            <div className="mt-2">
              <button type="button" className="btn btn-sm" onClick={addFuelRow}>
                {t("proposal.add_fuel_button")}
              </button>
            </div>
          </div>

          {error && <div className="mt-4 alert alert-error whitespace-pre-wrap">{error}</div>}
          {successMsg && <div className="mt-4 alert alert-success">{successMsg}</div>}

          <div className="mt-6 flex gap-2">
            <button className="btn btn-primary" type="submit" disabled={submitting}>
              {submitting ? t("proposal.sending_text") : t("proposal.submit_button")}
            </button>
            <button
              type="button"
              className="btn btn-outline"
              onClick={() => {
                setBrandName("");
                setStreet("");
                setHouseNumber("");
                setCity("");
                setPostalCode("");
                setLatitude("");
                setLongitude("");
                setFuelRows([{ fuelCode: "PB95", price: "" }]);
                if (globalPreview) {
                  URL.revokeObjectURL(globalPreview);
                  setGlobalPreview(null);
                }
                setGlobalFile(null);
              }}
            >
              {t("proposal.clear_button")}
            </button>
          </div>
        </form>
      </main>
      <Footer />
    </div>
  );
}
