import * as React from "react";
import { useTranslation } from "react-i18next";

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
  const { t } = useTranslation();

  if (!isOpen) return null;

  const formatDate = (iso?: string) =>
    iso ? new Date(iso).toLocaleString() : "-";

  const hasPendingItems = group?.items.some(
    (it) => it.status === "Pending" || it.status === "pending"
  );

  return (
    <div className={`modal modal-open`}>
      <div className="modal-box w-11/12 max-w-4xl max-h-[90vh] overflow-y-auto">
        <div className="flex justify-between items-start mb-4">
          <div>
            <h2 className="text-xl font-semibold mb-1">
              {t("proposaladmin.review_title")}
            </h2>
            {group ? (
              <>
                <p className="text-sm text-base-content/70">
                  {t("proposaladmin.user_label")}:{" "}
                  <span className="font-mono">{group.userName}</span>
                </p>
                <p className="text-sm text-base-content/70">
                  {t("proposaladmin.station_label")}:{" "}
                  <span className="font-semibold">{group.brandName}</span>,{" "}
                  {group.street} {group.houseNumber}, {group.city}
                </p>
                <p className="text-xs text-base-content/60">
                  {t("proposaladmin.submitted_label")}: {formatDate(group.createdAt)}
                </p>
              </>
            ) : (
              <p className="text-sm text-base-content/70">
                {t("proposaladmin.loading_details")}
              </p>
            )}
          </div>

          <button
            type="button"
            className="btn btn-sm btn-ghost"
            onClick={onClose}
            disabled={loading}
            title={t("proposaladmin.close")}
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
                    <th>{t("proposaladmin.table.th_hash")}</th>
                    <th>{t("proposaladmin.table.th_fuel")}</th>
                    <th>{t("proposaladmin.table.th_code")}</th>
                    <th>{t("proposaladmin.table.th_price")}</th>
                    <th>{t("proposaladmin.table.th_status")}</th>
                    <th className="text-right">{t("proposaladmin.table.th_actions")}</th>
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
                        {it.status === "Pending" || it.status === "pending" ? (
                          <span className="badge badge-warning badge-sm">
                            {t("proposaladmin.status.pending")}
                          </span>
                        ) : it.status === "Accepted" || it.status === "accepted" ? (
                          <span className="badge badge-success badge-sm">
                            {t("proposaladmin.status.accepted")}
                          </span>
                        ) : it.status === "Rejected" || it.status === "rejected" ? (
                          <span className="badge badge-error badge-sm">
                            {t("proposaladmin.status.rejected")}
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
                            disabled={loading || (it.status !== "Pending" && it.status !== "pending")}
                          >
                            {t("proposaladmin.btn.accept")}
                          </button>
                          <button
                            type="button"
                            className="btn btn-xs btn-error"
                            onClick={() => onRejectSingle(it.token)}
                            disabled={loading || (it.status !== "Pending" && it.status !== "pending")}
                          >
                            {t("proposaladmin.btn.reject")}
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
                    {t("proposaladmin.photo_title")}
                  </h3>

                  {photosLoading && (
                    <div className="flex items-center gap-2 text-xs text-base-content/70 mb-2">
                      <span className="loading loading-spinner loading-xs" />
                      <span>{t("proposaladmin.photo_loading")}</span>
                    </div>
                  )}

                  <div className="w-full flex justify-center">
                    <img
                      src={photoItem.photoUrl!}
                      alt={t("proposaladmin.photo_alt")}
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
                  disabled={loading || !hasPendingItems}
                >
                  {t("proposaladmin.btn.accept_all")}
                </button>
                <button
                  type="button"
                  className="btn btn-sm btn-error"
                  onClick={onRejectAll}
                  disabled={loading || !hasPendingItems}
                >
                  {t("proposaladmin.btn.reject_all")}
                </button>
              </div>

              {loading && (
                <div className="flex items-center gap-2 text-sm text-base-content/70">
                  <span className="loading loading-spinner loading-sm" />
                  <span>{t("proposaladmin.processing")}</span>
                </div>
              )}
            </div>
          </>
        )}
      </div>

      <label className="modal-backdrop" onClick={loading ? undefined : onClose}>
        {t("proposaladmin.close")}
      </label>
    </div>
  );
}