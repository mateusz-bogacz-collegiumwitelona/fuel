import * as React from "react";

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
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-base-300/60">
      <div className="bg-base-100 rounded-xl shadow-xl w-full max-w-md p-6">
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
  const [name, setName] = React.useState("");

  React.useEffect(() => {
    if (isOpen) setName("");
  }, [isOpen]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onConfirm({ name });
  };

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title="Dodaj nową markę">
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="form-control">
          <label className="label">
            <span className="label-text">Nazwa marki (np. Orlen)</span>
          </label>
          <input
            className="input input-bordered input-sm"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
          />
        </div>

        <div className="flex justify-end gap-2">
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
    <BaseModal isOpen={isOpen} onClose={onClose} title="Edytuj markę">
      <form onSubmit={handleSubmit} className="space-y-4">
        <p className="text-sm">
          Aktualna nazwa: <span className="font-semibold">{brand.name}</span>
        </p>

        <div className="form-control">
          <label className="label">
            <span className="label-text">Nowa nazwa marki</span>
          </label>
          <input
            className="input input-bordered input-sm"
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            required
          />
        </div>

        <div className="flex justify-end gap-2">
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
  if (!brand) return null;

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title="Usuń markę">
      <p className="mb-4">
        Czy na pewno chcesz usunąć markę{" "}
        <span className="font-semibold">{brand.name}</span>?
      </p>

      <p className="text-xs text-error mb-4">
        Operacja jest nieodwracalna. Zostaną usunięte wszystkie stacje i dane
        powiązane z tą marką (zgodnie z regułami cascade delete).
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
