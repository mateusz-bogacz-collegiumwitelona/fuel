import * as React from "react";
import { useTranslation } from "react-i18next";
import { API_BASE } from "./api";

type StationData = {
  brandName: string;
  street: string;
  houseNumber: string;
  city: string;
  postalCode: string;
  latitude: number;
  longitude: number;
  fuelPrice?: { fuelCode: string; price: number }[];
};

type FuelRow = { fuelCode: string; price: string };

type ProposalModalProps = {
  isOpen: boolean;
  onClose: () => void;
  station: StationData | null;
};

function BaseModal({ isOpen, title, children, onClose }: { isOpen: boolean; title: string; children: React.ReactNode; onClose: () => void }) {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[9999] flex items-center justify-center bg-base-300/80 p-4 overflow-y-auto">
      <div className="bg-base-100 rounded-xl shadow-xl w-full max-w-2xl my-8 relative flex flex-col max-h-[90vh]">
        <div className="flex justify-between items-center p-6 border-b border-base-200">
          <h2 className="text-xl font-semibold">{title}</h2>
          <button className="btn btn-sm btn-ghost" onClick={onClose} type="button">✕</button>
        </div>
        <div className="p-6 overflow-y-auto flex-1">
          {children}
        </div>
      </div>
    </div>
  );
}

const AVAILABLE_FUEL_TYPES = ["PB95", "PB98", "ON", "LPG", "E85"];

export function ProposalModal({ isOpen, onClose, station }: ProposalModalProps) {
  const { t } = useTranslation();


  const [brandName, setBrandName] = React.useState("");
  const [street, setStreet] = React.useState("");
  const [houseNumber, setHouseNumber] = React.useState("");
  const [city, setCity] = React.useState("");
  

  const [fuelRows, setFuelRows] = React.useState<FuelRow[]>([{ fuelCode: "PB95", price: "" }]);
  const [globalFile, setGlobalFile] = React.useState<File | null>(null);
  const [globalPreview, setGlobalPreview] = React.useState<string | null>(null);


  const [submitting, setSubmitting] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [successMsg, setSuccessMsg] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (isOpen && station) {
      setBrandName(station.brandName || "");
      setStreet(station.street || "");
      setHouseNumber(station.houseNumber || "");
      setCity(station.city || "");
      
      if (station.fuelPrice && station.fuelPrice.length > 0) {
        setFuelRows(station.fuelPrice.map(f => ({
          fuelCode: f.fuelCode,
          price: String(f.price)
        })));
      } else {
        setFuelRows([{ fuelCode: "PB95", price: "" }]);
      }
      
      setError(null);
      setSuccessMsg(null);
      setGlobalFile(null);
      setGlobalPreview(null);
    }
  }, [isOpen, station]);


  React.useEffect(() => {
    return () => {
      if (globalPreview) URL.revokeObjectURL(globalPreview);
    };
  }, [globalPreview]);

  function addFuelRow() {
    setFuelRows((p) => [...p, { fuelCode: "", price: "" }]);
  }
  function removeFuelRow(idx: number) {
    setFuelRows((p) => p.filter((_, i) => i !== idx));
  }
  function updateFuelRow(idx: number, row: Partial<FuelRow>) {
    setFuelRows((p) => p.map((r, i) => (i === idx ? { ...r, ...row } : r)));
  }

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const f = e.target.files?.[0] || null;
    setGlobalFile(f);
    if (globalPreview) URL.revokeObjectURL(globalPreview);
    if (f) setGlobalPreview(URL.createObjectURL(f));
    else setGlobalPreview(null);
  }


  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSuccessMsg(null);

    if (!globalFile) {
      setError(t("proposal.error_no_photo") || "Musisz dodać zdjęcie weryfikacyjne.");
      return;
    }
    const activeRows = fuelRows.filter(r => r.fuelCode && r.price);
    if (activeRows.length === 0) {
      setError(t("proposal.error_no_price") || "Musisz dodać co najmniej jedną cenę.");
      return;
    }

    setSubmitting(true);

    try {
      const token = localStorage.getItem("token");
      
      const requests = activeRows.map(row => {
        const fd = new FormData();
        
        fd.append("BrandName", brandName);
        fd.append("Street", street);
        fd.append("HouseNumber", String(houseNumber));
        fd.append("City", city);
        
        fd.append("FuelTypeCode", row.fuelCode);
        const priceNum = Number(String(row.price).replace(",", "."));
        fd.append("ProposedPrice", String(priceNum));
        
        if (globalFile) fd.append("Photo", globalFile, globalFile.name);

        if (station) {
            fd.append("TargetBrandName", station.brandName);
            fd.append("TargetStreet", station.street);
            fd.append("TargetHouseNumber", station.houseNumber);
            fd.append("TargetCity", station.city);
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
            if (!res.ok) throw new Error(text || res.statusText);
            return text;
        });
      });

      const settled = await Promise.allSettled(requests);
      const failures = settled.filter(r => r.status === "rejected");

      if (failures.length === 0) {
        setSuccessMsg(t("proposal.success_all_sent") || "Zgłoszenie wysłane pomyślnie!");
        setTimeout(() => {
            onClose(); 
        }, 1500);
      } else {
        setError(`${t("proposal.errors")}: ${failures.length} błędów wysyłania.`);
      }

    } catch (err: any) {
        console.error(err);
        setError(err.message || "Błąd wysyłania.");
    } finally {
        setSubmitting(false);
    }
  }

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title={t("proposal.form_title") || "Zgłoś zmianę cen"}>
      <form onSubmit={handleSubmit} className="space-y-6">
        
        <div className="bg-base-200 p-4 rounded-lg border border-base-300">
            <h3 className="text-lg font-bold text-base">{brandName}</h3>
            <p className="text-base-content/80 text-sm">
                {city}, ul. {street} {houseNumber}
            </p>
            <p className="text-xs text-base-content/50 mt-1">
                Zgłaszasz aktualizację cen dla tej stacji.
            </p>
        </div>

        <div>
            <div className="flex justify-between items-center mb-2">
                <span className="font-semibold text-sm">{t("proposal.proposed_prices_title") || "Proponowane ceny"}</span>
            </div>
            <div className="space-y-3">
                {fuelRows.map((row, idx) => (
                    <div key={idx} className="flex gap-2">
                        <select 
                            className="select select-bordered select-sm w-1/3"
                            value={row.fuelCode}
                            onChange={e => updateFuelRow(idx, { fuelCode: e.target.value })}
                        >
                            {AVAILABLE_FUEL_TYPES.map(ft => <option key={ft} value={ft}>{ft}</option>)}
                        </select>
                        <div className="relative w-1/3">
                            <input 
                                className="input input-bordered input-sm w-full pr-8"
                                type="number" step="0.01"
                                placeholder="0.00"
                                value={row.price}
                                onChange={e => updateFuelRow(idx, { price: e.target.value })}
                            />
                            <span className="absolute right-3 top-1 text-xs text-gray-500 leading-8">zł</span>
                        </div>
                        <button type="button" className="btn btn-sm btn-ghost text-error" onClick={() => removeFuelRow(idx)}>
                           ✕
                        </button>
                    </div>
                ))}
                <button type="button" className="btn btn-xs btn-outline w-40 border-dashed" onClick={addFuelRow}>+ Dodaj kolejne paliwo</button>
            </div>
        </div>

        <div>
            <label className="label pt-0"><span className="label-text font-semibold">{t("proposal.photo_label") || "Zdjęcie dowodowe (paragon/pylon)"}</span></label>
            <input type="file" className="file-input file-input-bordered w-full file-input-sm" accept="image/*" onChange={handleFileChange} />
            {globalPreview && (
                <div className="mt-3 flex justify-center bg-base-200 p-2 rounded-lg">
                    <img src={globalPreview} alt="Preview" className="h-40 object-contain rounded" />
                </div>
            )}
        </div>

        {error && <div className="alert alert-error text-sm py-2">{error}</div>}
        {successMsg && <div className="alert alert-success text-sm py-2">{successMsg}</div>}

        <div className="flex justify-end gap-2 pt-2 border-t border-base-200">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={submitting}>{t("common.cancel") || "Anuluj"}</button>
            <button type="submit" className="btn btn-primary px-6" disabled={submitting}>
                {submitting ? <span className="loading loading-spinner loading-xs"></span> : (t("proposal.submit_button") || "Wyślij zgłoszenie")}
            </button>
        </div>

      </form>
    </BaseModal>
  );
}