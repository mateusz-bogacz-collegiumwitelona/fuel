import * as React from "react";

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

/* ---------------------- MODAL DODAWANIA ---------------------- */

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
    fuelTypes: [
      {
        code: "PB95",
        price: 6.5,
      },
      {
        code: "PB98",
        price: 6.5,
      },
      {
        code: "E85",
        price: 6.5,
      },
      {
        code: "ON",
        price: 6.5,
      },
      {
        code: "LPG",
        price: 6.5,
      },
    ],
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;

    setForm((prev) => ({
      ...prev,
      [name]: name === "latitude" || name === "longitude" ? Number(value) : value,
    }));
  };

  // Zmiana pojedynczego pola paliwa
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

  // Dodanie nowego wiersza paliwa
  const handleAddFuelRow = () => {
    setForm((prev) => ({
      ...prev,
      fuelTypes: [...prev.fuelTypes, { code: "", price: 0 }],
    }));
  };

  // Usunięcie wiersza paliwa
  const handleRemoveFuelRow = (index: number) => {
    setForm((prev) => {
      if (prev.fuelTypes.length === 1) return prev; // zawsze minimum jedno paliwo
      const fuelTypes = prev.fuelTypes.filter((_, i) => i !== index);
      return { ...prev, fuelTypes };
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    // odfiltruj puste wiersze paliw na wszelki wypadek
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
        </div>

        {/* Paliwa – teraz wiele wierszy */}
        <div className="mt-2 border-t border-base-300 pt-3">
          <h3 className="text-sm font-semibold mb-2">Ceny paliw</h3>

          {form.fuelTypes.map((fuel, index) => (
            <div key={index} className="grid grid-cols-12 gap-2 mb-2">
              <div className="form-control col-span-5">
                <label className="label">
                  <span className="label-text">Rodzaj paliwa</span>
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

/* ---------------------- MODAL EDYCJI ---------------------- */

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

  React.useEffect(() => {
    if (station) {
      setForm({
        brandName: station.brandName,
        city: station.city,
        street: station.street,
        houseNumber: station.houseNumber,
        postalCode: station.postalCode,
      });
    }
  }, [station]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setForm((prev) => ({
      ...prev,
      [name]: name === "latitude" || name === "longitude" ? Number(value) : value,
    }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onConfirm(form);
  };

  if (!station) return null;

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title="Edytuj stację paliw">
      <form onSubmit={handleSubmit} className="space-y-3">
        <p className="text-xs mb-1">
          Edytujesz stację:{" "}
          <span className="font-semibold">
            {station.brandName} – {station.city}, {station.street}{" "}
            {station.houseNumber}
          </span>
        </p>

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
    </BaseModal>
  );
}

/* ---------------------- MODAL USUWANIA ---------------------- */

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
