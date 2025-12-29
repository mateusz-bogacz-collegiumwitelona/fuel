import * as React from "react";
import { useTranslation } from "react-i18next";
import { API_BASE } from "./api";

export type AdminUser = {
  userName: string;
  email: string;
  roles: string;
  createdAt: string;
  isBanned: boolean;
  hasReport: boolean;
};

export type ChangeRoleForm = {
  newRole: "User" | "Admin";
};

export type BanForm = {
  reason: string;
  days: number | null; // null = ban permanentny
};

export type BanInfo = {
  userName: string;
  reason: string;
  bannedAt: string;
  bannedUntil: string;
  bannedBy: string;
};

export type UserReportItem = {
  userName: string;
  userEmail: string;
  reportingUserName: string;
  reportingUserEmail: string;
  reason: string;
  status: string;
  createdAt: string;
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
      <div className="bg-base-100 rounded-xl shadow-xl w-full max-w-2xl p-6 relative">
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-xl font-semibold">{title}</h2>
          <button
            className="btn btn-sm btn-ghost"
            onClick={onClose}
            type="button"
            aria-label={title}
          >
            ✕
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}

type ChangeRoleModalProps = {
  isOpen: boolean;
  onClose: () => void;
  user: AdminUser | null;
  onConfirm: (form: ChangeRoleForm) => void;
};

export function ChangeRoleModal({
  isOpen,
  onClose,
  user,
  onConfirm,
}: ChangeRoleModalProps) {
  const { t } = useTranslation();
  const [form, setForm] = React.useState<ChangeRoleForm>({
    newRole: "User",
  });

  React.useEffect(() => {
    if (user) {
      const currentRole =
        user.roles.includes("Admin") && !user.roles.includes("User")
          ? "Admin"
          : user.roles.includes("Admin")
          ? "Admin"
          : "User";
      setForm({ newRole: currentRole as "User" | "Admin" });
    }
  }, [user]);

  if (!user) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onConfirm(form);
  };

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title={t("user-admin.change_role_title")}>
      <form onSubmit={handleSubmit} className="space-y-4">
        <p className="text-sm">
          {t("user-admin.user_label")}:{" "}
          <span className="font-semibold">
            {user.userName} ({user.email})
          </span>
        </p>

        <div className="form-control">
          <label className="label">
            <span className="label-text">{t("user-admin.change_role_new_label")}</span>
          </label>
          <select
            className="select select-bordered select-sm"
            value={form.newRole}
            onChange={(e) =>
              setForm({ newRole: e.target.value as "User" | "Admin" })
            }
          >
            <option value="User">{t("user-admin.role_user")}</option>
            <option value="Admin">{t("user-admin.role_admin")}</option>
          </select>
        </div>

        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={onClose}
          >
            {t("common.cancel")}
          </button>
          <button type="submit" className="btn btn-primary btn-sm">
            {t("common.save")}
          </button>
        </div>
      </form>
    </BaseModal>
  );
}

type BanUserModalProps = {
  isOpen: boolean;
  onClose: () => void;
  user: AdminUser | null;
  onConfirm: (form: BanForm) => void;
};

export function BanUserModal({
  isOpen,
  onClose,
  user,
  onConfirm,
}: BanUserModalProps) {
  const { t } = useTranslation();

  const [form, setForm] = React.useState<BanForm>({
    reason: "",
    days: 7,
  });
  const [permanent, setPermanent] = React.useState(false);

  React.useEffect(() => {
    if (isOpen) {
      setForm({ reason: "", days: 7 });
      setPermanent(false);
    }
  }, [isOpen]);

  if (!user) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onConfirm({
      reason: form.reason,
      days: permanent ? null : form.days ?? 7,
    });
  };

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title={t("user-admin.ban_title")}>
      <form onSubmit={handleSubmit} className="space-y-4">
        <p className="text-sm">
          {t("user-admin.ban_confirm_user", { user: `${user.userName} (${user.email})` })}
        </p>

        <div className="form-control">
          <label className="label">
            <span className="label-text">{t("user-admin.ban_reason_label")}</span>
          </label>
          <textarea
            className="textarea textarea-bordered textarea-sm"
            value={form.reason}
            onChange={(e) =>
              setForm((prev) => ({ ...prev, reason: e.target.value }))
            }
            required
          />
        </div>

        <div className="form-control">
          <label className="cursor-pointer flex items-center gap-2">
            <input
              type="checkbox"
              className="checkbox checkbox-sm"
              checked={permanent}
              onChange={(e) => setPermanent(e.target.checked)}
            />
            <span className="label-text">{t("user-admin.ban_permanent_label")}</span>
          </label>
        </div>

        {!permanent && (
          <div className="form-control">
            <label className="label">
              <span className="label-text">{t("user-admin.ban_days_label")}</span>
            </label>
            <input
              type="number"
              min={1}
              className="input input-bordered input-sm"
              value={form.days ?? 7}
              onChange={(e) =>
                setForm((prev) => ({
                  ...prev,
                  days: Number(e.target.value) || 1,
                }))
              }
              required
            />
          </div>
        )}

        <p className="text-xs text-warning">
          {t("user-admin.ban_note")}
        </p>

        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={onClose}
          >
            {t("common.cancel")}
          </button>
          <button type="submit" className="btn btn-error btn-sm">
            {t("user-admin.ban_submit")}
          </button>
        </div>
      </form>
    </BaseModal>
  );
}

type ReviewBanModalProps = {
  isOpen: boolean;
  onClose: () => void;
  user: AdminUser | null;
  banInfo: BanInfo | null;
  loading: boolean;
  error: string | null;
};

export function ReviewBanModal({
  isOpen,
  onClose,
  user,
  banInfo,
  loading,
  error,
}: ReviewBanModalProps) {
  const { t } = useTranslation();

  if (!user) return null;

  const formatDate = (iso?: string) => (iso ? new Date(iso).toLocaleString() : "-");
  const isPermanent = (until?: string) =>
    until === "9999-12-31T23:59:59.9999999Z" || until === "9999-12-31T23:59:59Z";

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title={t("user-admin.review_ban_title")}>
      <div className="space-y-3 text-sm">
        <p>
          {t("user-admin.user_label")}:{" "}
          <span className="font-semibold">
            {user.userName} ({user.email})
          </span>
        </p>

        {loading ? (
          <p>{t("user-admin.review_ban_loading")}</p>
        ) : error ? (
          <p className="text-error">{error}</p>
        ) : !banInfo ? (
          <p>{t("user-admin.review_ban_no_ban")}</p>
        ) : (
          <>
            <p>
              <span className="font-semibold">{t("user-admin.review_ban_reason_label")}: </span> {banInfo.reason}
            </p>
            <p>
              <span className="font-semibold">{t("user-admin.review_ban_banned_at_label")}: </span> {formatDate(banInfo.bannedAt)}
            </p>
            <p>
              <span className="font-semibold">{t("user-admin.review_ban_banned_until_label")}: </span>{" "}
              {isPermanent(banInfo.bannedUntil) ? t("user-admin.review_ban_permanent") : formatDate(banInfo.bannedUntil)}
            </p>
            <p>
              <span className="font-semibold">{t("user-admin.review_ban_by_label")}: </span> {banInfo.bannedBy}
            </p>
          </>
        )}

        <div className="flex justify-end pt-2">
          <button
            className="btn btn-primary btn-sm"
            type="button"
            onClick={onClose}
          >
            {t("common.close")}
          </button>
        </div>
      </div>
    </BaseModal>
  );
}

type UnlockUserModalProps = {
  isOpen: boolean;
  onClose: () => void;
  user: AdminUser | null;
  onConfirm: () => void;
};

export function UnlockUserModal({
  isOpen,
  onClose,
  user,
  onConfirm,
}: UnlockUserModalProps) {
  const { t } = useTranslation();

  if (!user) return null;

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title={t("user-admin.unlock_title")}>
      <p className="mb-4">
        {t("user-admin.unlock_confirm", { user: `${user.userName} (${user.email})` })}
      </p>

      <div className="flex justify-end gap-2">
        <button
          className="btn btn-ghost btn-sm"
          type="button"
          onClick={onClose}
        >
          {t("common.cancel")}
        </button>
        <button
          className="btn btn-primary btn-sm"
          type="button"
          onClick={onConfirm}
        >
          {t("user-admin.unlock_confirm_button")}
        </button>
      </div>
    </BaseModal>
  );
}


type UserReportsModalProps = {
  isOpen: boolean;
  onClose: () => void;
  user: AdminUser | null;
};

// W pliku src/components/user-admin-modals.tsx podmień funkcję UserReportsModal:

export function UserReportsModal({ isOpen, onClose, user }: UserReportsModalProps) {
  const { t } = useTranslation();
  const [reports, setReports] = React.useState<UserReportItem[]>([]);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  const [actionState, setActionState] = React.useState<{
    report: UserReportItem | null;
    type: "accept" | null;
    days: number;
    reason: string;
    isProcessing: boolean;
  }>({
    report: null,
    type: null,
    days: 7,
    reason: "",
    isProcessing: false,
  });

  React.useEffect(() => {
    if (isOpen && user) {
      fetchReports(user.email);
    } else {
      setReports([]);
      setError(null);
      resetActionState();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, user]);

  const resetActionState = () => {
    setActionState({
      report: null,
      type: null,
      days: 7,
      reason: "",
      isProcessing: false,
    });
  };

  const fetchReports = async (email: string) => {
    setLoading(true);
    setError(null);
    try {
      const res = await fetch(
        `${API_BASE}/api/admin/user/report/list?email=${encodeURIComponent(
          email
        )}&PageSize=100`,
        {
          headers: { Accept: "application/json" },
          credentials: "include",
        }
      );

      if (!res.ok) {
        throw new Error(
          t("useradmin.reports_error_fetch", {
            status: res.status,
            text: res.statusText,
          })
        );
      }

      const json = await res.json();
      const items = json.data?.items || json.items || [];
      setReports(items);
    } catch (e: any) {
      console.error(e);
      setError(
        e.message ||
          t("useradmin.reports_error_fetch", { status: "Unknown", text: "" })
      );
    } finally {
      setLoading(false);
    }
  };

  const handleRejectClick = async (report: UserReportItem) => {
    if (!confirm(t("Are you sure you want to reject this report?"))) return;
    await sendChangeStatus(report, false);
  };

  const handleAcceptClick = (report: UserReportItem) => {
    setActionState({
      report,
      type: "accept",
      days: 7,
      reason: report.reason || "",
      isProcessing: false,
    });
  };

  const sendChangeStatus = async (
    report: UserReportItem,
    isAccepted: boolean,
    banDays?: number,
    banReason?: string
  ) => {
    if (isAccepted) {
        setActionState(prev => ({ ...prev, isProcessing: true }));
    } else {
        setLoading(true);
    }

    try {
      const payload: any = {
        isAccepted: isAccepted,
        reportedUserEmail: report.reportedUserEmail,
        reportingUserEmail: report.reportingUserEmail,
        reportCreatedAt: report.createdAt,
      };

      if (isAccepted) {
        payload.reason = banReason;
        payload.days = banDays;
      }

      const res = await fetch(`${API_BASE}/api/admin/user/report/change-status`, {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        credentials: "include",
        body: JSON.stringify(payload),
      });

      if (!res.ok) {
        const text = await res.text();
        throw new Error(text || res.statusText);
      }

      if (user) await fetchReports(user.email);
      resetActionState();

    } catch (e: any) {
      console.error(e);
      alert(t("user-admin.report_action_error", { text: e.message }));
      setActionState(prev => ({ ...prev, isProcessing: false }));
    } finally {
      setLoading(false);
    }
  };

  if (!user) return null;

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={onClose}
      title={t("user-admin.reports_modal_title")}
    >
      <div className="space-y-4 relative">
        <p className="text-sm text-base-content/70">
          {t("user-admin.user_label")}:{" "}
          <span className="font-semibold text-base-content">{user.userName}</span>
        </p>

        {loading ? (
          <div className="flex justify-center py-4">
            <span className="loading loading-spinner"></span>
            <span className="ml-2 text-sm">
              {t("useradmin.reports_modal_loading")}
            </span>
          </div>
        ) : error ? (
          <div className="alert alert-error text-sm">{error}</div>
        ) : reports.length === 0 ? (
          <div className="alert alert-success text-sm">
            {t("useradmin.reports_modal_no_reports")}
          </div>
        ) : (
          <div className="overflow-x-auto max-h-80">
            <table className="table table-xs table-pin-rows">
              <thead>
                <tr>
                  <th>{t("useradmin.reports_table_date")}</th>
                  <th>{t("useradmin.reports_table_reporter")}</th>
                  <th>{t("useradmin.reports_table_reason")}</th>
                  <th>{t("useradmin.reports_table_status")}</th>
                  <th className="text-right">{t("useradmin.table_actions")}</th>
                </tr>
              </thead>
              <tbody>
                {reports.map((r, idx) => (
                  <tr key={idx}>
                    <td>
                      {new Date(r.createdAt).toLocaleDateString()} <br />
                      <span className="text-xs opacity-50">
                        {new Date(r.createdAt).toLocaleTimeString([], {
                          hour: "2-digit",
                          minute: "2-digit",
                        })}
                      </span>
                    </td>
                    <td className="font-medium">
                      {r.reportingUserName}
                      {r.reportingUserEmail && (
                        <div className="text-[10px] opacity-60 font-normal">
                          {r.reportingUserEmail}
                        </div>
                      )}
                    </td>
                    <td className="whitespace-normal min-w-[150px] max-w-xs">
                      {r.reason}
                    </td>
                    <td>
                      <span className="badge badge-warning badge-xs">
                        {r.status === "Pending"
                          ? t("useradmin.reports_status_pending")
                          : r.status}
                      </span>
                    </td>
                    <td className="text-right">
                      {r.status === "Pending" && (
                        <div className="flex flex-col gap-1 items-end">
                          <button
                            className="btn btn-xs btn-success text-white"
                            onClick={() => handleAcceptClick(r)}
                          >
                            {t("user-admin.report_action_accept")}
                          </button>
                          <button
                            className="btn btn-xs btn-error btn-outline"
                            onClick={() => handleRejectClick(r)}
                          >
                            {t("user-admin.report_action_reject")}
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {actionState.type === "accept" && actionState.report && (
          <div className="absolute inset-0 bg-base-100/95 z-20 flex items-center justify-center rounded-xl">
            <div className="w-full max-w-sm p-4 bg-base-200 shadow-xl rounded-xl border border-base-300">
              <h3 className="font-bold text-lg mb-4">
                {t("user-admin.report_accept_title")}
              </h3>
              
              <div className="form-control mb-2">
                <label className="label">
                  <span className="label-text">{t("user-admin.report_accept_days")}</span>
                </label>
                <input
                  type="number"
                  className="input input-sm input-bordered"
                  value={actionState.days}
                  onChange={(e) =>
                    setActionState({ ...actionState, days: Math.max(1, parseInt(e.target.value) || 1) })
                  }
                  min={1}
                />
              </div>

              <div className="form-control mb-4">
                <label className="label">
                  <span className="label-text">{t("user-admin.report_accept_reason")}</span>
                </label>
                <textarea
                  className="textarea textarea-bordered h-24"
                  value={actionState.reason}
                  onChange={(e) =>
                    setActionState({ ...actionState, reason: e.target.value })
                  }
                  placeholder="Wpisz powód bana..."
                ></textarea>
              </div>

              <div className="flex justify-end gap-2">
                <button
                  className="btn btn-sm btn-ghost"
                  onClick={resetActionState}
                  disabled={actionState.isProcessing}
                >
                  {t("common.cancel")}
                </button>
                <button
                  className="btn btn-sm btn-error"
                  disabled={actionState.isProcessing || !actionState.reason || actionState.days < 1}
                  onClick={() =>
                    sendChangeStatus(
                      actionState.report!,
                      true,
                      actionState.days,
                      actionState.reason
                    )
                  }
                >
                  {actionState.isProcessing ? (
                    <span className="loading loading-spinner loading-xs"></span>
                  ) : (
                    t("user-admin.report_accept_confirm")
                  )}
                </button>
              </div>
            </div>
          </div>
        )}

        <div className="flex justify-end pt-2">
          <button className="btn btn-primary btn-sm" onClick={onClose}>
            {t("common.close")}
          </button>
        </div>
      </div>
    </BaseModal>
  );
}