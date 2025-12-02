import * as React from "react";
import { API_BASE } from "./api";


export type AdminStation = {
  brandName: string;
  street: string;
  houseNumber: string;
  city: string;
  postalCode: string;
  createdAt?: string;
  updatedAt?: string;
};

export type FuelForm = {
  code: string;
  price: number;
};

export type StationFormValues = {
  brandName: string;
  street: string;
  houseNumber: string;
  city: string;
  postalCode: string;
  latitude: number;
  longitude: number;
  fuelTypes: FuelForm[];
};

const DEFAULT_FUEL_TYPES: FuelForm[] = [
  { code: "PB95", price: 6.52 },
  { code: "PB98", price: 5.98 },
  { code: "E85", price: 6.23 },
  { code: "ON", price: 6.21 },
  { code: "LPG", price: 3.56 },
];


type BaseModalProps = {
  isOpen: boolean;
  title: string;
  children: React.ReactNode;
  onClose: () => void;
};

function BaseModal({ isOpen, title, children, onClose }: BaseModalProps) {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-base-300/60">
      <div className="bg-base-100 rounded-xl shadow-xl w-full max-w-lg p-6">
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-xl font-semibold">{title}</h2>
          <button
            className="btn btn-sm btn-ghost"
            onClick={onClose}
            type="button"
          >
            ✕
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}


type AddStationModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (values: StationFormValues) => void;
};

export function AddStationModal({
  isOpen,
  onClose,
  onConfirm,
}: AddStationModalProps) {
  const [form, setForm] = React.useState<StationFormValues>({
    brandName: "",
    street: "",
    houseNumber: "",
    city: "",
    postalCode: "",
    latitude: 0,
    longitude: 0,
    fuelTypes: DEFAULT_FUEL_TYPES.map((f) => ({ ...f })),
  });

  const [geoLoading, setGeoLoading] = React.useState(false);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;

    setForm((prev) => ({
      ...prev,
      [name]:
        name === "latitude" || name === "longitude" ? Number(value) : value,
    }));
  };

  const handleFuelChange = (
    index: number,
    field: "code" | "price",
    value: string,
  ) => {
    setForm((prev) => {
      const fuelTypes = [...prev.fuelTypes];
      fuelTypes[index] = {
        ...fuelTypes[index],
        [field]: field === "price" ? Number(value) : value,
      };
      return { ...prev, fuelTypes };
    });
  };

  const handleAddFuelRow = () => {
    setForm((prev) => ({
      ...prev,
      fuelTypes: [...prev.fuelTypes, { code: "", price: 0 }],
    }));
  };

  const handleRemoveFuelRow = (index: number) => {
    setForm((prev) => {
      if (prev.fuelTypes.length === 1) return prev;
      const fuelTypes = prev.fuelTypes.filter((_, i) => i !== index);
      return { ...prev, fuelTypes };
    });
  };

  const handleGeocode = async () => {
    const { city, street, houseNumber, postalCode } = form;

    if (!city.trim() || !street.trim() || !houseNumber.trim()) {
      alert("Podaj co najmniej miasto, ulicę i numer budynku.");
      return;
    }

    const query = `${street} ${houseNumber}, ${postalCode ?? ""} ${city}, Polska`;

    try {
      setGeoLoading(true);

      const params = new URLSearchParams({
        format: "json",
        q: query,
        limit: "1",
      });

      const res = await fetch(
        `https://nominatim.openstreetmap.org/search?${params.toString()}`,
        {
          headers: {
            Accept: "application/json",
          },
        },
      );

      if (!res.ok) {
        const text = await res.text();
        throw new Error(
          `Błąd geokodowania (${res.status}): ${text || "brak treści"}`,
        );
      }

      const data: Array<{ lat: string; lon: string }> = await res.json();

      if (!data.length) {
        alert("Nie znaleziono współrzędnych dla podanego adresu.");
        return;
      }

      const first = data[0];

      setForm((prev) => ({
        ...prev,
        latitude: Number(first.lat),
        longitude: Number(first.lon),
      }));
    } catch (err) {
      console.error(err);
      alert("Nie udało się pobrać współrzędnych dla tego adresu.");
    } finally {
      setGeoLoading(false);
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    const cleaned: StationFormValues = {
      ...form,
      fuelTypes: form.fuelTypes.filter(
        (f) => f.code.trim().length > 0 && !Number.isNaN(f.price),
      ),
    };

    onConfirm(cleaned);
  };

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title="Dodaj stację paliw">
      <form onSubmit={handleSubmit} className="space-y-3">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div className="form-control">
            <label className="label">
              <span className="label-text">Marka / nazwa stacji</span>
            </label>
            <input
              name="brandName"
              className="input input-bordered input-sm"
              value={form.brandName}
              onChange={handleChange}
              required
            />
          </div>

          <div className="form-control">
            <label className="label">
              <span className="label-text">Miasto</span>
            </label>
            <input
              name="city"
              className="input input-bordered input-sm"
              value={form.city}
              onChange={handleChange}
              required
            />
          </div>

          <div className="form-control">
            <label className="label">
              <span className="label-text">Ulica</span>
            </label>
            <input
              name="street"
              className="input input-bordered input-sm"
              value={form.street}
              onChange={handleChange}
              required
            />
          </div>

          <div className="form-control">
            <label className="label">
              <span className="label-text">Numer budynku</span>
            </label>
            <input
              name="houseNumber"
              className="input input-bordered input-sm"
              value={form.houseNumber}
              onChange={handleChange}
              required
            />
          </div>

          <div className="form-control">
            <label className="label">
              <span className="label-text">Kod pocztowy</span>
            </label>
            <input
              name="postalCode"
              className="input input-bordered input-sm"
              value={form.postalCode}
              onChange={handleChange}
            />
          </div>

          <div className="col-span-1 md:col-span-2">
            <div className="grid grid-cols-2 gap-3">
              <div className="form-control">
                <label className="label">
                  <span className="label-text">lat</span>
                </label>
                <input
                  name="latitude"
                  type="number"
                  step="0.000001"
                  className="input input-bordered input-sm"
                  value={form.latitude}
                  onChange={handleChange}
                  required
                />
              </div>

              <div className="form-control">
                <label className="label">
                  <span className="label-text">lng</span>
                </label>
                <input
                  name="longitude"
                  type="number"
                  step="0.000001"
                  className="input input-bordered input-sm"
                  value={form.longitude}
                  onChange={handleChange}
                  required
                />
              </div>
            </div>

            <div className="mt-2">
              <button
                type="button"
                className="btn btn-outline btn-xs"
                onClick={handleGeocode}
                disabled={geoLoading}
              >
                {geoLoading
                  ? "Pobieranie współrzędnych..."
                  : "Pobierz współrzędne z adresu"}
              </button>
            </div>
          </div>
        </div>


        <div className="mt-2 border-t border-base-300 pt-3">
          <h3 className="text-sm font-semibold mb-2">Ceny paliw</h3>

          {form.fuelTypes.map((fuel, index) => (
            <div key={index} className="grid grid-cols-12 gap-2 mb-2">
              <div className="form-control col-span-5">
                <label className="label">
                  <span className="label-text">Kod (np. PB95)</span>
                </label>
                <input
                  className="input input-bordered input-sm"
                  value={fuel.code}
                  onChange={(e) =>
                    handleFuelChange(index, "code", e.target.value)
                  }
                  required
                />
              </div>

              <div className="form-control col-span-5">
                <label className="label">
                  <span className="label-text">Cena</span>
                </label>
                <input
                  type="number"
                  step="0.01"
                  className="input input-bordered input-sm"
                  value={fuel.price}
                  onChange={(e) =>
                    handleFuelChange(index, "price", e.target.value)
                  }
                  required
                />
              </div>

              <div className="flex items-end justify-end col-span-2">
                {form.fuelTypes.length > 1 && (
                  <button
                    type="button"
                    className="btn btn-xs btn-ghost text-error"
                    onClick={() => handleRemoveFuelRow(index)}
                  >
                    usuń
                  </button>
                )}
              </div>
            </div>
          ))}

          <button
            type="button"
            className="btn btn-xs mt-1"
            onClick={handleAddFuelRow}
          >
            + Dodaj paliwo
          </button>
        </div>

        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={onClose}
          >
            Anuluj
          </button>
          <button type="submit" className="btn btn-primary btn-sm">
            Zapisz
          </button>
        </div>
      </form>
    </BaseModal>
  );
}


type EditStationModalProps = {
  isOpen: boolean;
  onClose: () => void;
  station: AdminStation | null;
  onConfirm: (values: Partial<StationFormValues>) => void;
};

export function EditStationModal({
  isOpen,
  onClose,
  station,
  onConfirm,
}: EditStationModalProps) {
  const [form, setForm] = React.useState<Partial<StationFormValues>>({});
  const [detailsLoading, setDetailsLoading] = React.useState(false);
  const [detailsError, setDetailsError] = React.useState<string | null>(null);
  const [geoLoading, setGeoLoading] = React.useState(false);

  React.useEffect(() => {
    if (!isOpen || !station) return;

    let cancelled = false;

    setDetailsError(null);
    setForm({
      brandName: station.brandName,
      city: station.city,
      street: station.street,
      houseNumber: station.houseNumber,
      postalCode: station.postalCode,
      fuelTypes: DEFAULT_FUEL_TYPES.map((f) => ({ ...f })),
    });

    (async () => {
      setDetailsLoading(true);
      try {
        const params = new URLSearchParams({
          BrandName: station.brandName,
          Street: station.street,
          HouseNumber: station.houseNumber,
          City: station.city,
        });

        const res = await fetch(
          `${API_BASE}/api/admin/station/edit/info?${params.toString()}`,
          {
            method: "GET",
            headers: {
              Accept: "application/json",
            },
            credentials: "include",
          },
        );

        if (!res.ok) {
          const text = await res.text();
          throw new Error(
            `Nie udało się pobrać danych stacji (${res.status}): ${text}`,
          );
        }

        const json = await res.json();
        const data = json.data ?? json;

        const rawLatitude =
          (data as any).Latitude ?? (data as any).latitude ?? (data as any).lat;
        const rawLongitude =
          (data as any).Longitude ??
          (data as any).longitude ??
          (data as any).lng;

        const latitude =
          rawLatitude !== undefined && rawLatitude !== null
            ? Number(rawLatitude)
            : undefined;
        const longitude =
          rawLongitude !== undefined && rawLongitude !== null
            ? Number(rawLongitude)
            : undefined;

        const rawFuelType =
          (data as any).FuelType ??
          (data as any).fuelType ??
          (data as any).FuelTypes ??
          (data as any).fuelTypes;

        let fuelTypes: FuelForm[];

        if (Array.isArray(rawFuelType)) {
          fuelTypes = rawFuelType.map((f: any) => ({
            code: f.code ?? f.Code ?? "",
            price: Number(f.price ?? f.Price ?? 0),
          }));
        } else {
          fuelTypes = DEFAULT_FUEL_TYPES.map((f) => ({ ...f }));
        }

        if (cancelled) return;

        setForm((prev) => ({
          ...prev,
          brandName:
            data.BrandName ?? data.brandName ?? station.brandName,
          city: data.City ?? data.city ?? station.city,
          street: data.Street ?? data.street ?? station.street,
          houseNumber:
            data.HouseNumber ?? data.houseNumber ?? station.houseNumber,
          postalCode: station.postalCode,
          latitude,
          longitude,
          fuelTypes,
        }));
      } catch (err: any) {
        console.error(err);
        if (cancelled) return;
        setDetailsError(
          err?.message ?? "Nie udało się pobrać danych stacji do edycji.",
        );
      } finally {
        if (!cancelled) setDetailsLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [isOpen, station]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setForm((prev) => ({
      ...prev,
      [name]:
        name === "latitude" || name === "longitude" ? Number(value) : value,
    }));
  };

  const handleFuelChange = (
    index: number,
    field: "code" | "price",
    value: string,
  ) => {
    setForm((prev) => {
      const current = prev.fuelTypes ?? [];
      const fuelTypes = [...current];
      fuelTypes[index] = {
        ...fuelTypes[index],
        [field]: field === "price" ? Number(value) : value,
      };
      return { ...prev, fuelTypes };
    });
  };

  const handleAddFuelRow = () => {
    setForm((prev) => {
      const current = prev.fuelTypes ?? [];
      return {
        ...prev,
        fuelTypes: [...current, { code: "", price: 0 }],
      };
    });
  };

  const handleRemoveFuelRow = (index: number) => {
    setForm((prev) => {
      const current = prev.fuelTypes ?? [];
      if (current.length <= 1) return prev;
      const fuelTypes = current.filter((_, i) => i !== index);
      return { ...prev, fuelTypes };
    });
  };

  const handleGeocode = async () => {
    const city = form.city ?? station?.city ?? "";
    const street = form.street ?? station?.street ?? "";
    const houseNumber = form.houseNumber ?? station?.houseNumber ?? "";
    const postalCode = form.postalCode ?? station?.postalCode ?? "";

    if (!city.trim() || !street.trim() || !houseNumber.trim()) {
      alert("Podaj co najmniej miasto, ulicę i numer budynku.");
      return;
    }

    const query = `${street} ${houseNumber}, ${postalCode ?? ""} ${city}, Polska`;

    try {
      setGeoLoading(true);

      const params = new URLSearchParams({
        format: "json",
        q: query,
        limit: "1",
      });

      const res = await fetch(
        `https://nominatim.openstreetmap.org/search?${params.toString()}`,
        {
          headers: {
            Accept: "application/json",
          },
        },
      );

      if (!res.ok) {
        const text = await res.text();
        throw new Error(
          `Błąd geokodowania (${res.status}): ${text || "brak treści"}`,
        );
      }

      const data: Array<{ lat: string; lon: string }> = await res.json();

      if (!data.length) {
        alert("Nie znaleziono współrzędnych dla podanego adresu.");
        return;
      }

      const first = data[0];

      setForm((prev) => ({
        ...prev,
        latitude: Number(first.lat),
        longitude: Number(first.lon),
      }));
    } catch (err) {
      console.error(err);
      alert("Nie udało się pobrać współrzędnych dla tego adresu.");
    } finally {
      setGeoLoading(false);
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    const cleaned: Partial<StationFormValues> = {
      ...form,
      fuelTypes: (form.fuelTypes ?? []).filter(
        (f) => f.code?.trim().length > 0 && !Number.isNaN(f.price),
      ),
    };

    onConfirm(cleaned);
  };

  if (!station) return null;

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title="Edytuj stację paliw">
      <p className="text-xs mb-2">
        Edytujesz stację:{" "}
        <span className="font-semibold">
          {station.brandName} – {station.city}, {station.street}{" "}
          {station.houseNumber}
        </span>
      </p>

      {detailsError && (
        <div className="alert alert-error mb-3 py-2 min-h-0">
          <span className="text-xs">{detailsError}</span>
        </div>
      )}

      {detailsLoading ? (
        <div className="py-8 flex justify-center">
          <span className="loading loading-spinner loading-lg" />
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="space-y-3">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div className="form-control">
              <label className="label">
                <span className="label-text">Marka / nazwa stacji</span>
              </label>
              <input
                name="brandName"
                className="input input-bordered input-sm"
                value={form.brandName ?? ""}
                onChange={handleChange}
              />
            </div>

            <div className="form-control">
              <label className="label">
                <span className="label-text">Miasto</span>
              </label>
              <input
                name="city"
                className="input input-bordered input-sm"
                value={form.city ?? ""}
                onChange={handleChange}
              />
            </div>

            <div className="form-control">
              <label className="label">
                <span className="label-text">Ulica</span>
              </label>
              <input
                name="street"
                className="input input-bordered input-sm"
                value={form.street ?? ""}
                onChange={handleChange}
              />
            </div>

            <div className="form-control">
              <label className="label">
                <span className="label-text">Numer budynku</span>
              </label>
              <input
                name="houseNumber"
                className="input input-bordered input-sm"
                value={form.houseNumber ?? ""}
                onChange={handleChange}
              />
            </div>

            <div className="form-control">
              <label className="label">
                <span className="label-text">Kod pocztowy</span>
              </label>
              <input
                name="postalCode"
                className="input input-bordered input-sm"
                value={form.postalCode ?? ""}
                onChange={handleChange}
              />
            </div>

            <div className="col-span-1 md:col-span-2">
              <div className="grid grid-cols-2 gap-3">
                <div className="form-control">
                  <label className="label">
                    <span className="label-text">lat</span>
                  </label>
                  <input
                    name="latitude"
                    type="number"
                    step="0.000001"
                    className="input input-bordered input-sm"
                    value={
                      typeof form.latitude === "number"
                        ? form.latitude
                        : ""
                    }
                    onChange={handleChange}
                  />
                </div>

                <div className="form-control">
                  <label className="label">
                    <span className="label-text">lng</span>
                  </label>
                  <input
                    name="longitude"
                    type="number"
                    step="0.000001"
                    className="input input-bordered input-sm"
                    value={
                      typeof form.longitude === "number"
                        ? form.longitude
                        : ""
                    }
                    onChange={handleChange}
                  />
                </div>
              </div>

              <div className="mt-2">
                <button
                  type="button"
                  className="btn btn-outline btn-xs"
                  onClick={handleGeocode}
                  disabled={geoLoading}
                >
                  {geoLoading
                    ? "Pobieranie współrzędnych..."
                    : "Pobierz współrzędne z adresu"}
                </button>
              </div>
            </div>
          </div>


          <div className="mt-2 border-t border-base-300 pt-3">
            <h3 className="text-sm font-semibold mb-2">Ceny paliw</h3>

            {(form.fuelTypes ?? []).map((fuel, index) => (
              <div key={index} className="grid grid-cols-12 gap-2 mb-2">
                <div className="form-control col-span-5">
                  <label className="label">
                    <span className="label-text">Kod (np. PB95)</span>
                  </label>
                  <input
                    className="input input-bordered input-sm"
                    value={fuel.code}
                    onChange={(e) =>
                      handleFuelChange(index, "code", e.target.value)
                    }
                  />
                </div>

                <div className="form-control col-span-5">
                  <label className="label">
                    <span className="label-text">Cena</span>
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    className="input input-bordered input-sm"
                    value={fuel.price}
                    onChange={(e) =>
                      handleFuelChange(index, "price", e.target.value)
                    }
                  />
                </div>

                <div className="flex items-end justify-end col-span-2">
                  {(form.fuelTypes ?? []).length > 1 && (
                    <button
                      type="button"
                      className="btn btn-xs btn-ghost text-error"
                      onClick={() => handleRemoveFuelRow(index)}
                    >
                      usuń
                    </button>
                  )}
                </div>
              </div>
            ))}

            <button
              type="button"
              className="btn btn-xs mt-1"
              onClick={handleAddFuelRow}
            >
              + Dodaj paliwo
            </button>
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              onClick={onClose}
            >
              Anuluj
            </button>
            <button type="submit" className="btn btn-primary btn-sm">
              Zapisz zmiany
            </button>
          </div>
        </form>
      )}
    </BaseModal>
  );
}

type DeleteStationModalProps = {
  isOpen: boolean;
  onClose: () => void;
  station: AdminStation | null;
  onConfirm: () => void;
};

export function DeleteStationModal({
  isOpen,
  onClose,
  station,
  onConfirm,
}: DeleteStationModalProps) {
  if (!station) return null;

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title="Usuń stację paliw">
      <p className="mb-4">
        Czy na pewno chcesz usunąć stację{" "}
        <span className="font-semibold">
          {station.brandName} – {station.city}, {station.street}{" "}
          {station.houseNumber}
        </span>
        ?
      </p>

      <p className="text-xs text-error mb-4">
        Tej operacji nie można cofnąć.
      </p>

      <div className="flex justify-end gap-2">
        <button
          className="btn btn-ghost btn-sm"
          type="button"
          onClick={onClose}
        >
          Anuluj
        </button>
        <button
          className="btn btn-error btn-sm"
          type="button"
          onClick={onConfirm}
        >
          Usuń
        </button>
      </div>
    </BaseModal>
  );
}
