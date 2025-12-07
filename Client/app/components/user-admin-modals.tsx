import * as React from "react";

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
          >
            ✕
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}

/* ---------------------- MODAL ZMIANY ROLI ---------------------- */

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
    <BaseModal isOpen={isOpen} onClose={onClose} title="Zmień rolę użytkownika">
      <form onSubmit={handleSubmit} className="space-y-4">
        <p className="text-sm">
          Użytkownik:{" "}
          <span className="font-semibold">
            {user.userName} ({user.email})
          </span>
        </p>

        <div className="form-control">
          <label className="label">
            <span className="label-text">Nowa rola</span>
          </label>
          <select
            className="select select-bordered select-sm"
            value={form.newRole}
            onChange={(e) =>
              setForm({ newRole: e.target.value as "User" | "Admin" })
            }
          >
            <option value="User">User</option>
            <option value="Admin">Admin</option>
          </select>
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

/* ---------------------- MODAL BANA (LOCK-OUT) ---------------------- */

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
    <BaseModal isOpen={isOpen} onClose={onClose} title="Zbanuj użytkownika">
      <form onSubmit={handleSubmit} className="space-y-4">
        <p className="text-sm">
          Zablokujesz dostęp dla:{" "}
          <span className="font-semibold">
            {user.userName} ({user.email})
          </span>
        </p>

        <div className="form-control">
          <label className="label">
            <span className="label-text">Powód bana</span>
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
            <span className="label-text">Ban permanentny</span>
          </label>
        </div>

        {!permanent && (
          <div className="form-control">
            <label className="label">
              <span className="label-text">Liczba dni bana</span>
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
          Użytkownik nie będzie mógł się logować przez czas trwania bana.
        </p>

        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={onClose}
          >
            Anuluj
          </button>
          <button type="submit" className="btn btn-error btn-sm">
            Zbanuj
          </button>
        </div>
      </form>
    </BaseModal>
  );
}

/* ---------------------- MODAL SZCZEGÓŁÓW BANA ---------------------- */

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
  if (!user) return null;

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title="Szczegóły bana">
      <div className="space-y-3 text-sm">
        <p>
          Użytkownik:{" "}
          <span className="font-semibold">
            {user.userName} ({user.email})
          </span>
        </p>

        {loading ? (
          <p>Ładowanie informacji o banie...</p>
        ) : error ? (
          <p className="text-error">{error}</p>
        ) : !banInfo ? (
          <p>Brak aktywnego bana dla tego użytkownika.</p>
        ) : (
          <>
            <p>
              <span className="font-semibold">Powód:</span> {banInfo.reason}
            </p>
            <p>
              <span className="font-semibold">Zbanowany:</span>{" "}
              {new Date(banInfo.bannedAt).toLocaleString()}
            </p>
            <p>
              <span className="font-semibold">Ban ważny do:</span>{" "}
              {banInfo.bannedUntil === "9999-12-31T23:59:59.9999999Z"
                ? "permanentny"
                : new Date(banInfo.bannedUntil).toLocaleString()}
            </p>
            <p>
              <span className="font-semibold">Nałożył:</span>{" "}
              {banInfo.bannedBy}
            </p>
          </>
        )}

        <div className="flex justify-end pt-2">
          <button
            className="btn btn-primary btn-sm"
            type="button"
            onClick={onClose}
          >
            Zamknij
          </button>
        </div>
      </div>
    </BaseModal>
  );
}

/* ---------------------- MODAL ODBLOKOWANIA ---------------------- */

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
  if (!user) return null;

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title="Odblokuj użytkownika">
      <p className="mb-4">
        Czy na pewno chcesz zdjąć bana z użytkownika{" "}
        <span className="font-semibold">
          {user.userName} ({user.email})
        </span>
        ?
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
          className="btn btn-primary btn-sm"
          type="button"
          onClick={onConfirm}
        >
          Odblokuj
        </button>
      </div>
    </BaseModal>
  );
}
