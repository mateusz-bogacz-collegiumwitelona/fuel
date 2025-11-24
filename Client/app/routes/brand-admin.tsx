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

const API_BASE = "http://localhost:5111";

function parseJwt(token: string | null) {
  if (!token) return null;
  try {
    const payload = token.split(".")[1];
    const decoded = atob(payload);
    return JSON.parse(decodeURIComponent(escape(decoded)));
  } catch {
    return null;
  }
}

type BrandListResponseData = {
  items: AdminBrand[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export default function BrandAdminPage() {
  const [email, setEmail] = React.useState<string | null>(null);
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
    (async () => {
      try {
        const token = localStorage.getItem("token");
        const expiration = localStorage.getItem("token_expiration");

        if (token && expiration && new Date(expiration) > new Date()) {
          const decoded = parseJwt(token);
          const userEmail = (decoded && (decoded.email || decoded.sub)) || null;
          setEmail(userEmail ?? "Zalogowany administrator");
          await loadBrandsFromApi(pageNumber, pageSize, search, sortDirection);
          return;
        }

        const refreshRes = await fetch(`${API_BASE}/api/refresh`, {
          method: "POST",
          headers: { Accept: "application/json" },
          credentials: "include",
        });

        if (refreshRes.ok) {
          setEmail("Zalogowany administrator");
          await loadBrandsFromApi(pageNumber, pageSize, search, sortDirection);
        } else {
          if (typeof window !== "undefined") window.location.href = "/login";
        }
      } catch (err) {
        console.error(err);
        if (typeof window !== "undefined") window.location.href = "/login";
      } finally {
        setLoading(false);
      }
    })();
  }, [pageNumber, pageSize, search, sortDirection]);

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
        `${API_BASE}/api/admin/brad/list?${params.toString()}`,
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
        throw new Error(`Błąd pobierania marek (${res.status}): ${text}`);
      }

      const json = await res.json();

      const data: BrandListResponseData | undefined = json.data
        ? (json.data as BrandListResponseData)
        : (json as BrandListResponseData);

      if (!data || !Array.isArray(data.items)) {
        throw new Error("Nieoczekiwany format odpowiedzi marek");
      }

      setBrands(data.items);
      setPageNumber(data.pageNumber);
      setTotalPages(data.totalPages);
    } catch (e: any) {
      console.error(e);
      setError(e?.message ?? "Nie udało się pobrać listy marek");
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

    const res = await fetch(`${API_BASE}/api/admin/brad/add`, {
      method: "POST",
      headers: {
        Accept: "application/json",
      },
      credentials: "include",
      body: formData,
    });

    if (!res.ok) {
      const text = await res.text();
      throw new Error(`Nie udało się dodać marki (${res.status}): ${text}`);
    }

    await loadBrandsFromApi(pageNumber, pageSize, search, sortDirection);
    closeModal();
  } catch (e) {
    console.error(e);
    alert(e instanceof Error ? e.message : "Nie udało się dodać marki");
  }
};


  const handleEditConfirm = async (form: EditBrandForm) => {
    if (!selectedBrand) return;

    try {
      const res = await fetch(
        `${API_BASE}/api/admin/brad/edit/${encodeURIComponent(
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
        throw new Error(`Nie udało się edytować marki (${res.status}): ${text}`);
      }

      await loadBrandsFromApi(pageNumber, pageSize, search, sortDirection);
      closeModal();
    } catch (e) {
      console.error(e);
      alert(e instanceof Error ? e.message : "Nie udało się edytować marki");
    }
  };

  const handleDeleteConfirm = async () => {
    if (!selectedBrand) return;

    try {
      const res = await fetch(
        `${API_BASE}/api/admin/brad/${encodeURIComponent(selectedBrand.name)}`,
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
        throw new Error(`Nie udało się usunąć marki (${res.status}): ${text}`);
      }

      await loadBrandsFromApi(pageNumber, pageSize, search, sortDirection);
      closeModal();
    } catch (e) {
      console.error(e);
      alert(e instanceof Error ? e.message : "Nie udało się usunąć marki");
    }
  };


  return (
    <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
      <Header />

      <main className="flex-1 mx-auto w-full max-w-6xl px-4 py-10">
        <div className="flex justify-between items-center mb-4">
          <div>
            <h1 className="text-3xl font-bold">
              Panel administracyjny – marki stacji
            </h1>
            <p className="text-sm text-base-content/70">
              {email ? `Zalogowano jako: ${email}` : "Sprawdzanie sesji..."}
            </p>
          </div>
          <div className="flex gap-2">
            <a href="/admin-dashboard" className="btn btn-outline btn-sm">
              ← Powrót do panelu administratora
            </a>
            <button
              className="btn btn-primary btn-sm"
              type="button"
              onClick={openAdd}
            >
              + Dodaj markę
            </button>
          </div>
        </div>

        <div className="bg-base-300 rounded-xl p-4 shadow-md mb-4 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div className="flex flex-col gap-2 md:flex-row md:items-end">
            <div className="form-control">
              <label className="label">
                <span className="label-text">Szukaj (nazwa marki)</span>
              </label>
              <input
                className="input input-bordered input-sm w-full md:w-64"
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value);
                  setPageNumber(1);
                }}
                placeholder="np. Orlen"
              />
            </div>

            <div className="form-control">
              <label className="label">
                <span className="label-text">Kierunek sortowania</span>
              </label>
              <select
                className="select select-bordered select-sm"
                value={sortDirection}
                onChange={(e) =>
                  setSortDirection(e.target.value as "asc" | "desc")
                }
              >
                <option value="asc">Rosnąco (A–Z)</option>
                <option value="desc">Malejąco (Z–A)</option>
              </select>
            </div>
          </div>
        </div>

        <div className="bg-base-300 rounded-xl p-4 shadow-md">
          {loading ? (
            <div className="text-sm">Ładowanie marek...</div>
          ) : error ? (
            <div className="text-sm text-error">{error}</div>
          ) : brands.length === 0 ? (
            <div className="text-sm">Brak marek w systemie.</div>
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="table table-zebra table-sm w-full">
                  <thead>
                    <tr>
                      <th>#</th>
                      <th>Nazwa marki</th>
                      <th>Utworzono</th>
                      <th>Zaktualizowano</th>
                      <th className="text-right">Akcje</th>
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
                              edytuj
                            </button>
                            <button
                              className="btn btn-xs btn-error"
                              type="button"
                              onClick={() => openDelete(b)}
                            >
                              usuń
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
                  Strona {pageNumber} / {totalPages}
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
