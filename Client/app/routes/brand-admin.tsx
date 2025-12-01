import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";

import {
  AddBrandModal,
  EditBrandModal,
  DeleteBrandModal,
} from "../components/brand-admin-modals";

import type {
  AdminBrand,
  AddBrandForm,
  EditBrandForm,
} from "../components/brand-admin-modals";

import { useAdminGuard } from "../components/useAdminGuard";
import { useTranslation } from "react-i18next";

const API_BASE = "http://localhost:5111";

type BrandListResponseData = {
  items: AdminBrand[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export default function BrandAdminPage() {
  const { t } = useTranslation();
  const { state, email } = useAdminGuard();

  const [brands, setBrands] = React.useState<AdminBrand[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);

  const [pageNumber, setPageNumber] = React.useState(1);
  const [pageSize] = React.useState(10);
  const [totalPages, setTotalPages] = React.useState(1);
  const [search, setSearch] = React.useState("");
  const [sortDirection, setSortDirection] = React.useState<"asc" | "desc">(
    "asc",
  );

  const [activeModal, setActiveModal] = React.useState<
    "add" | "edit" | "delete" | null
  >(null);
  const [selectedBrand, setSelectedBrand] =
    React.useState<AdminBrand | null>(null);

  React.useEffect(() => {
    if (state !== "allowed") return;
    (async () => {
      await loadBrandsFromApi(pageNumber, pageSize, search, sortDirection);
    })();
  }, [state, pageNumber, pageSize, search, sortDirection]);

  async function loadBrandsFromApi(
    page: number,
    size: number,
    searchValue: string,
    direction: "asc" | "desc",
  ) {
    setLoading(true);
    setError(null);

    try {
      const params = new URLSearchParams({
        PageNumber: String(page),
        PageSize: String(size),
        sortBy: "name",
        sortDirection: direction,
      });

      if (searchValue.trim().length > 0) {
        params.set("Search", searchValue.trim());
      }

      const res = await fetch(
        `${API_BASE}/api/admin/brand/list?${params.toString()}`,
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
        throw new Error(`${t("brandadmin.error_fetch_prefix")} (${res.status}): ${text}`);
      }

      const json = await res.json();

      const data: BrandListResponseData | undefined = json.data
        ? (json.data as BrandListResponseData)
        : (json as BrandListResponseData);

      if (!data || !Array.isArray(data.items)) {
        throw new Error(t("brandadmin.error_unexpected_response"));
      }

      setBrands(data.items);
      setPageNumber(data.pageNumber);
      setTotalPages(data.totalPages);
    } catch (e: any) {
      console.error(e);
      setError(e?.message ?? t("brandadmin.error_fetch_fallback"));
      setBrands([]);
    } finally {
      setLoading(false);
    }
  }

  const openAdd = () => {
    setSelectedBrand(null);
    setActiveModal("add");
  };

  const openEdit = (brand: AdminBrand) => {
    setSelectedBrand(brand);
    setActiveModal("edit");
  };

  const openDelete = (brand: AdminBrand) => {
    setSelectedBrand(brand);
    setActiveModal("delete");
  };

  const closeModal = () => {
    setActiveModal(null);
    setSelectedBrand(null);
  };

  const handleAddConfirm = async (form: AddBrandForm) => {
    try {
      const formData = new FormData();
      formData.append("name", form.name);

      const res = await fetch(`${API_BASE}/api/admin/brand/add`, {
        method: "POST",
        headers: {
          Accept: "application/json",
        },
        credentials: "include",
        body: formData,
      });

      if (!res.ok) {
        const text = await res.text();
        throw new Error(`${t("brandadmin.error_add_prefix")} (${res.status}): ${text}`);
      }

      await loadBrandsFromApi(pageNumber, pageSize, search, sortDirection);
      closeModal();
    } catch (e: any) {
      console.error(e);
      alert(e instanceof Error ? e.message : t("brandadmin.error_add_fallback"));
    }
  };

  const handleEditConfirm = async (form: EditBrandForm) => {
    if (!selectedBrand) return;

    try {
      const res = await fetch(
        `${API_BASE}/api/admin/brand/edit/${encodeURIComponent(
          selectedBrand.name,
        )}?newName=${encodeURIComponent(form.newName)}`,
        {
          method: "PATCH",
          headers: {
            Accept: "application/json",
          },
          credentials: "include",
        },
      );

      if (!res.ok) {
        const text = await res.text();
        throw new Error(`${t("brandadmin.error_edit_prefix")} (${res.status}): ${text}`);
      }

      await loadBrandsFromApi(pageNumber, pageSize, search, sortDirection);
      closeModal();
    } catch (e: any) {
      console.error(e);
      alert(e instanceof Error ? e.message : t("brandadmin.error_edit_fallback"));
    }
  };

  const handleDeleteConfirm = async () => {
    if (!selectedBrand) return;

    try {
      const res = await fetch(
        `${API_BASE}/api/admin/brand/${encodeURIComponent(selectedBrand.name)}`,
        {
          method: "DELETE",
          headers: {
            Accept: "application/json",
          },
          credentials: "include",
        },
      );

      if (!res.ok) {
        const text = await res.text();
        throw new Error(`${t("brandadmin.error_delete_prefix")} (${res.status}): ${text}`);
      }

      await loadBrandsFromApi(pageNumber, pageSize, search, sortDirection);
      closeModal();
    } catch (e: any) {
      console.error(e);
      alert(e instanceof Error ? e.message : t("brandadmin.error_delete_fallback"));
    }
  };

  if (state === "checking") {
    return (
      <div className="min-h-screen bg-base-200 flex items-center justify-center">
        <span className="loading loading-spinner loading-lg" />
      </div>
    );
  }

  if (state !== "allowed") {
    return null;
  }

  return (
    <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
      <Header />

      <main className="flex-1 mx-auto w-full max-w-6xl px-4 py-10">
        <div className="flex justify-between items-center mb-4">
          <div>
            <h1 className="text-3xl font-bold">{t("brandadmin.title")}</h1>
            <p className="text-sm text-base-content/70">
              {email ? t("brandadmin.logged_in_as", { email }) : t("brandadmin.checking_session")}
            </p>
          </div>
          <div className="flex gap-2">
            <a href="/admin-dashboard" className="btn btn-outline btn-sm">
              {t("brandadmin.back_to_admin")}
            </a>
            <button
              className="btn btn-primary btn-sm"
              type="button"
              onClick={openAdd}
            >
              {t("brandadmin.add_brand_button")}
            </button>
          </div>
        </div>

        <div className="bg-base-300 rounded-xl p-4 shadow-md mb-4 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div className="flex flex-col gap-2 md:flex-row md:items-end">
            <div className="form-control">
              <label className="label">
                <span className="label-text">{t("brandadmin.search_label")}</span>
              </label>
              <input
                className="input input-bordered input-sm w-full md:w-64"
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value);
                  setPageNumber(1);
                }}
                placeholder={t("brandadmin.search_placeholder")}
              />
            </div>

            <div className="form-control">
              <label className="label">
                <span className="label-text">{t("brandadmin.sort_label")}</span>
              </label>
              <select
                className="select select-bordered select-sm"
                value={sortDirection}
                onChange={(e) =>
                  setSortDirection(e.target.value as "asc" | "desc")
                }
              >
                <option value="asc">{t("brandadmin.sort_asc")}</option>
                <option value="desc">{t("brandadmin.sort_desc")}</option>
              </select>
            </div>
          </div>
        </div>

        <div className="bg-base-300 rounded-xl p-4 shadow-md">
          {loading ? (
            <div className="text-sm">{t("brandadmin.loading")}</div>
          ) : error ? (
            <div className="text-sm text-error">{error}</div>
          ) : brands.length === 0 ? (
            <div className="text-sm">{t("brandadmin.no_brands")}</div>
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="table table-zebra table-sm w-full">
                  <thead>
                    <tr>
                      <th>{t("brandadmin.table_hash")}</th>
                      <th>{t("brandadmin.table_name")}</th>
                      <th>{t("brandadmin.table_created")}</th>
                      <th>{t("brandadmin.table_updated")}</th>
                      <th className="text-right">{t("brandadmin.table_actions")}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {brands.map((b, idx) => (
                      <tr key={`${b.name}-${idx}`}>
                        <td>{idx + 1 + (pageNumber - 1) * pageSize}</td>
                        <td>{b.name}</td>
                        <td>
                          {b.createdAt
                            ? new Date(b.createdAt).toLocaleDateString()
                            : "-"}
                        </td>
                        <td>
                          {b.updatedAt
                            ? new Date(b.updatedAt).toLocaleDateString()
                            : "-"}
                        </td>
                        <td>
                          <div className="flex justify-end gap-2">
                            <button
                              className="btn btn-xs"
                              type="button"
                              onClick={() => openEdit(b)}
                            >
                              {t("brandadmin.edit_button")}
                            </button>
                            <button
                              className="btn btn-xs btn-error"
                              type="button"
                              onClick={() => openDelete(b)}
                            >
                              {t("brandadmin.delete_button")}
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="flex justify-end items-center gap-3 mt-3 text-sm">
                <span>
                  {t("brandadmin.page_info", { page: pageNumber, total: totalPages })}
                </span>
                <button
                  className="btn btn-xs"
                  disabled={pageNumber <= 1}
                  onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
                  type="button"
                >
                  ◀
                </button>
                <button
                  className="btn btn-xs"
                  disabled={pageNumber >= totalPages}
                  onClick={() =>
                    setPageNumber((p) => (p < totalPages ? p + 1 : p))
                  }
                  type="button"
                >
                  ▶
                </button>
              </div>
            </>
          )}
        </div>
      </main>

      <Footer />

      {/* MODALE */}
      <AddBrandModal
        isOpen={activeModal === "add"}
        onClose={closeModal}
        onConfirm={handleAddConfirm}
      />

      <EditBrandModal
        isOpen={activeModal === "edit"}
        onClose={closeModal}
        brand={selectedBrand}
        onConfirm={handleEditConfirm}
      />

      <DeleteBrandModal
        isOpen={activeModal === "delete"}
        onClose={closeModal}
        brand={selectedBrand}
        onConfirm={handleDeleteConfirm}
      />
    </div>
  );
}
