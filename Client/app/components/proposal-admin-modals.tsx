import * as React from "react";


export type ProposalGroupItem = {
  token: string;
  fuelName: string;
  fuelCode: string;
  proposedPrice: number;
  status: string;
  photoUrl?: string | null;
};


export type ProposalGroup = {
  id: string;
  userName: string;
  brandName: string;
  street: string;
  houseNumber: string;
  city: string;
  createdAt: string;
  items: ProposalGroupItem[];
};

type ReviewProposalModalProps = {
  isOpen: boolean;
  group: ProposalGroup | null;
  loading: boolean;
  error: string | null;
  onClose: () => void;

  onAcceptAll: () => void;
  onRejectAll: () => void;

  onAcceptSingle: (token: string) => void;
  onRejectSingle: (token: string) => void;

  photosLoading?: boolean;
};

export function ReviewProposalModal({
  isOpen,
  group,
  loading,
  error,
  onClose,
  onAcceptAll,
  onRejectAll,
  onAcceptSingle,
  onRejectSingle,
  photosLoading = false,
}: ReviewProposalModalProps) {
  if (!isOpen) return null;

  const formatDate = (iso?: string) =>
    iso ? new Date(iso).toLocaleString() : "-";

  return (
    <div className={`modal modal-open`}>
      <div className="modal-box max-w-4xl">
        <div className="flex justify-between items-start mb-4">
          <div>
            <h2 className="text-xl font-semibold mb-1">
              Przegląd propozycji cen
            </h2>
            {group ? (
              <>
                <p className="text-sm text-base-content/70">
                  Użytkownik:{" "}
                    <span className="font-mono">{group.userName}</span>
                </p>
                <p className="text-sm text-base-content/70">
                  Stacja:{" "}
                  <span className="font-semibold">{group.brandName}</span>,{" "}
                  {group.street} {group.houseNumber}, {group.city}
                </p>
                <p className="text-xs text-base-content/60">
                  Zgłoszono: {formatDate(group.createdAt)}
                </p>
              </>
            ) : (
              <p className="text-sm text-base-content/70">
                Ładowanie szczegółów zgłoszenia...
              </p>
            )}
          </div>

          <button
            type="button"
            className="btn btn-sm btn-ghost"
            onClick={onClose}
            disabled={loading}
          >
            ✕
          </button>
        </div>

        {error && (
          <div className="alert alert-error mb-3 py-2 min-h-0">
            <span className="text-sm">{error}</span>
          </div>
        )}

        {!group ? (
          <div className="py-8 flex justify-center">
            <span className="loading loading-spinner loading-lg" />
          </div>
        ) : (
          <>
            <div className="overflow-x-auto mb-4">
              <table className="table table-sm w-full">
                <thead>
                  <tr>
                    <th>#</th>
                    <th>Paliwo</th>
                    <th>Kod</th>
                    <th>Proponowana cena [PLN]</th>
                    <th>Status</th>
                    <th className="text-right">Akcje</th>
                  </tr>
                </thead>
                <tbody>
                  {group.items.map((it, idx) => (
                    <tr key={it.token}>
                      <td>{idx + 1}</td>
                      <td>{it.fuelName}</td>
                      <td>{it.fuelCode}</td>
                      <td>{it.proposedPrice.toFixed(2)}</td>
                      <td>
                        {it.status === "Pending" ? (
                          <span className="badge badge-warning badge-sm">
                            Oczekuje
                          </span>
                        ) : it.status === "Accepted" ? (
                          <span className="badge badge-success badge-sm">
                            Zaakceptowana
                          </span>
                        ) : it.status === "Rejected" ? (
                          <span className="badge badge-error badge-sm">
                            Odrzucona
                          </span>
                        ) : (
                          <span className="badge badge-ghost badge-sm">
                            {it.status}
                          </span>
                        )}
                      </td>
                      <td>
                        <div className="flex justify-end gap-2">
                          <button
                            type="button"
                            className="btn btn-xs btn-success"
                            onClick={() => onAcceptSingle(it.token)}
                            disabled={loading || it.status !== "Pending"}
                          >
                            akceptuj
                          </button>
                          <button
                            type="button"
                            className="btn btn-xs btn-error"
                            onClick={() => onRejectSingle(it.token)}
                            disabled={loading || it.status !== "Pending"}
                          >
                            odrzuć
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

{(() => {
  const photoItem = group.items.find((it) => it.photoUrl);
  if (!photoItem) return null;

  return (
    <div className="mt-4">
      <h3 className="text-sm font-semibold mb-2">
        Zdjęcie potwierdzające
      </h3>

      {photosLoading && (
        <div className="flex items-center gap-2 text-xs text-base-content/70 mb-2">
          <span className="loading loading-spinner loading-xs" />
          <span>Ładowanie zdjęcia…</span>
        </div>
      )}

      <div className="w-full flex justify-center">
        <img
          src={photoItem.photoUrl!}
          alt="Zdjęcie potwierdzające"
          className="rounded-md max-h-64 object-contain"
        />
      </div>
    </div>
  );
})()}



            <div className="flex justify-between items-center mt-4">
              <div className="flex gap-2">
                <button
                  type="button"
                  className="btn btn-sm btn-success"
                  onClick={onAcceptAll}
                  disabled={loading || !group.items.length}
                >
                  Akceptuj wszystkie
                </button>
                <button
                  type="button"
                  className="btn btn-sm btn-error"
                  onClick={onRejectAll}
                  disabled={loading || !group.items.length}
                >
                  Odrzuć wszystkie
                </button>
              </div>

              {loading && (
                <div className="flex items-center gap-2 text-sm text-base-content/70">
                  <span className="loading loading-spinner loading-sm" />
                  <span>Przetwarzanie…</span>
                </div>
              )}
            </div>
          </>
        )}
      </div>

      <label className="modal-backdrop" onClick={loading ? undefined : onClose}>
        Zamknij
      </label>
    </div>
  );
}
