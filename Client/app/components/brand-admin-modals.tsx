import * as React from "react";
import { useTranslation } from "react-i18next";

export type AdminBrand = {
  name: string;
  createdAt?: string;
  updatedAt?: string;
};

export type AddBrandForm = {
  name: string;
};

export type EditBrandForm = {
  newName: string;
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
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-base-300/60 p-4">
      <div className="bg-base-100 rounded-xl shadow-xl w-full max-w-md flex flex-col max-h-[90vh]">
        <div className="flex justify-between items-center p-6 pb-2 flex-shrink-0">
          <h2 className="text-xl font-semibold">{title}</h2>
          <button
            className="btn btn-sm btn-ghost"
            onClick={onClose}
            type="button"
            aria-label="Close"
          >
            ✕
          </button>
        </div>
        <div className="p-6 pt-2 overflow-y-auto">
          {children}
        </div>
      </div>
    </div>
  );
}

type AddBrandModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (form: AddBrandForm) => void;
};

export function AddBrandModal({
  isOpen,
  onClose,
  onConfirm,
}: AddBrandModalProps) {
  const { t } = useTranslation();
  const [name, setName] = React.useState("");

  React.useEffect(() => {
    if (isOpen) setName("");
  }, [isOpen]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onConfirm({ name });
  };

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title={t("brandadmin.add_title")}>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="form-control">
          <label className="label">
            <span className="label-text">{t("brandadmin.add_label_name")}</span>
          </label>
          <input
            className="input input-bordered input-sm w-full"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
          />
        </div>

        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={onClose}
          >
            {t("brandadmin.cancel")}
          </button>
          <button type="submit" className="btn btn-primary btn-sm">
            {t("brandadmin.save")}
          </button>
        </div>
      </form>
    </BaseModal>
  );
}

type EditBrandModalProps = {
  isOpen: boolean;
  onClose: () => void;
  brand: AdminBrand | null;
  onConfirm: (form: EditBrandForm) => void;
};

export function EditBrandModal({
  isOpen,
  onClose,
  brand,
  onConfirm,
}: EditBrandModalProps) {
  const { t } = useTranslation();
  const [newName, setNewName] = React.useState("");

  React.useEffect(() => {
    if (brand && isOpen) {
      setNewName(brand.name);
    }
  }, [brand, isOpen]);

  if (!brand) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onConfirm({ newName });
  };

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title={t("brandadmin.edit_title")}>
      <form onSubmit={handleSubmit} className="space-y-4">
        <p className="text-sm">
          <span className="text-sm">{t("brandadmin.current_name_label")}</span>{" "}
          <span className="font-semibold">{brand.name}</span>
        </p>

        <div className="form-control">
          <label className="label">
            <span className="label-text">{t("brandadmin.edit_new_name_label")}</span>
          </label>
          <input
            className="input input-bordered input-sm w-full"
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            required
          />
        </div>

        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={onClose}
          >
            {t("brandadmin.cancel")}
          </button>
          <button type="submit" className="btn btn-primary btn-sm">
            {t("brandadmin.save_changes")}
          </button>
        </div>
      </form>
    </BaseModal>
  );
}

type DeleteBrandModalProps = {
  isOpen: boolean;
  onClose: () => void;
  brand: AdminBrand | null;
  onConfirm: () => void;
};

export function DeleteBrandModal({
  isOpen,
  onClose,
  brand,
  onConfirm,
}: DeleteBrandModalProps) {
  const { t } = useTranslation();
  if (!brand) return null;

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title={t("brandadmin.delete_title")}>
      <p className="mb-4">
        {t("brandadmin.delete_confirm", { name: brand.name })}
      </p>

      <p className="text-xs text-error mb-4">
        {t("brandadmin.delete_warning")}
      </p>

      <div className="flex justify-end gap-2 pt-2">
        <button
          className="btn btn-ghost btn-sm"
          type="button"
          onClick={onClose}
        >
          {t("brandadmin.cancel")}
        </button>
        <button
          className="btn btn-error btn-sm"
          type="button"
          onClick={onConfirm}
        >
          {t("brandadmin.delete_confirm_button")}
        </button>
      </div>
    </BaseModal>
  );
}