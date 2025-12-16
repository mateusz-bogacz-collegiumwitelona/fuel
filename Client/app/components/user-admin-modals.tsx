import * as React from "react";
import { useTranslation } from "react-i18next";

export type AdminUser = {
  userName: string;
  email: string;
  roles: string;
  createdAt: string;
  isBanned: boolean;
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
