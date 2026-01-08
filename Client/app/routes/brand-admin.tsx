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
import { API_BASE } from "../components/api";

type BrandListResponseData = {
  items: AdminBrand[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export default function BrandAdminPage() {
  const { t } = useTranslation();
  React.useEffect(() => {
    document.title = t("brandadmin.title") + " - FuelStats";
  }, [t]);
  const { state, email } = useAdminGuard();

  const [brands, setBrands] = React.useState<AdminBrand[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);

  const [pageNumber, setPageNumber] = React.useState(1);
  const [pageSize] = React.useState(10);
  const [totalPages, setTotalPages] = React.useState(1);
  const [search, setSearch] = React.useState("");
  const [sortDirection, setSortDirection] = React.useState<"asc" | "desc">("asc");

  const [activeModal, setActiveModal] = React.useState<"add" | "edit" | "delete" | null>(null);
  const [selectedBrand, setSelectedBrand] = React.useState<AdminBrand | null>(null);

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
          headers: { Accept: "application/json" },
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

  const openAdd = () => { setSelectedBrand(null); setActiveModal("add"); };
  const openEdit = (brand: AdminBrand) => { setSelectedBrand(brand); setActiveModal("edit"); };
  const openDelete = (brand: AdminBrand) => { setSelectedBrand(brand); setActiveModal("delete"); };
  const closeModal = () => { setActiveModal(null); setSelectedBrand(null); };

  const handleAddConfirm = async (form: AddBrandForm) => {
    try {
      const formData = new FormData();
      formData.append("name", form.name);

      const res = await fetch(`${API_BASE}/api/admin/brand/add`, {
        method: "POST",
        headers: { Accept: "application/json" },
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
        `${API_BASE}/api/admin/brand/edit/${encodeURIComponent(selectedBrand.name)}?newName=${encodeURIComponent(form.newName)}`,
        {
          method: "PATCH",
          headers: { Accept: "application/json" },
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
          headers: { Accept: "application/json" },
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

  const goToPage = (p: number) => {
    if (p < 1 || p > totalPages || p === pageNumber) return;
    setPageNumber(p);
  };

  function renderPageButtons() {
    const pages: number[] = [];
    const windowSize = 5; 
    let start = Math.max(1, pageNumber - Math.floor(windowSize / 2));
    let end = start + windowSize - 1;

    if (end > totalPages) {
      end = totalPages;
      start = Math.max(1, end - windowSize + 1);
    }
    for (let i = start; i <= end; i++) pages.push(i);

    return (
      <div className="flex items-center gap-1 sm:gap-2">
        <button className="btn btn-sm" onClick={() => goToPage(1)} disabled={pageNumber === 1} type="button">«</button>
        <button className="btn btn-sm" onClick={() => goToPage(pageNumber - 1)} disabled={pageNumber === 1} type="button">←</button>
        {pages.map((p) => (
          <button key={p} className={`btn btn-sm ${p === pageNumber ? "btn-active" : ""}`} onClick={() => goToPage(p)} type="button">{p}</button>
        ))}
        <button className="btn btn-sm" onClick={() => goToPage(pageNumber + 1)} disabled={pageNumber === totalPages} type="button">→</button>
        <button className="btn btn-sm" onClick={() => goToPage(totalPages)} disabled={pageNumber === totalPages} type="button">»</button>
      </div>
    );
  }

  if (state === "checking") return <div className="min-h-screen bg-base-200 flex items-center justify-center"><span className="loading loading-spinner loading-lg" /></div>;
  if (state !== "allowed") return null;

  return (
    <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
      <Header />

      <main className="flex-1 mx-auto w-full max-w-6xl px-4 py-6 sm:py-10">
      
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-4">
          <div>
            <h1 className="text-2xl sm:text-3xl font-bold">{t("brandadmin.title")}</h1>
            <p className="text-sm text-base-content/70">
              {email ? t("brandadmin.logged_in_as", { email }) : t("brandadmin.checking_session")}
            </p>
          </div>
          <div className="flex gap-2 w-full sm:w-auto justify-end">
            <a href="/admin" className="btn btn-outline btn-sm">
              {t("brandadmin.back_to_admin")}
            </a>
            <button className="btn btn-primary btn-sm" type="button" onClick={openAdd}>
              {t("brandadmin.add_brand_button")}
            </button>
          </div>
        </div>

        <div className="bg-base-300 rounded-xl p-4 shadow-md mb-4 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div className="flex flex-col gap-2 w-full md:w-auto md:flex-row md:items-end">
            <div className="form-control w-full md:w-auto">
              <label className="label"><span className="label-text">{t("brandadmin.search_label")}</span></label>
              <input
                className="input input-bordered input-sm w-full md:w-64"
                value={search}
                onChange={(e) => { setSearch(e.target.value); setPageNumber(1); }}
                placeholder={t("brandadmin.search_placeholder")}
              />
            </div>
            <div className="form-control w-full md:w-auto">
              <label className="label"><span className="label-text">{t("brandadmin.sort_label")}</span></label>
              <select
                className="select select-bordered select-sm w-full md:w-auto"
                value={sortDirection}
                onChange={(e) => setSortDirection(e.target.value as "asc" | "desc")}
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
                        <td className="font-semibold">{b.name}</td>
                        <td className="whitespace-nowrap">{b.createdAt ? new Date(b.createdAt).toLocaleDateString() : "-"}</td>
                        <td className="whitespace-nowrap">{b.updatedAt ? new Date(b.updatedAt).toLocaleDateString() : "-"}</td>
                        <td>
                          <div className="flex justify-end gap-2">
                            <button className="btn btn-xs" type="button" onClick={() => openEdit(b)}>
                              {t("brandadmin.edit_button")}
                            </button>
                            <button className="btn btn-xs btn-error" type="button" onClick={() => openDelete(b)}>
                              {t("brandadmin.delete_button")}
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <div className="mt-4 flex flex-col sm:flex-row justify-between items-center gap-4 text-sm">
                {renderPageButtons()}
                <div className="text-base-content/70">
                  {t("brandadmin.page_info", { page: pageNumber, total: totalPages })}
                </div>
              </div>
            </>
          )}
        </div>
      </main>

      <Footer />

      <AddBrandModal isOpen={activeModal === "add"} onClose={closeModal} onConfirm={handleAddConfirm} />
      <EditBrandModal isOpen={activeModal === "edit"} onClose={closeModal} brand={selectedBrand} onConfirm={handleEditConfirm} />
      <DeleteBrandModal isOpen={activeModal === "delete"} onClose={closeModal} brand={selectedBrand} onConfirm={handleDeleteConfirm} />
    </div>
  );
}