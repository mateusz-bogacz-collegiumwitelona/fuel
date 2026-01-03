import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";

import {
  AddStationModal,
  EditStationModal,
  DeleteStationModal,
} from "../components/gas-station-modals";

import type {
  AdminStation,
  StationFormValues,
} from "../components/gas-station-modals";

import { useAdminGuard } from "../components/useAdminGuard";
import { useTranslation } from "react-i18next";
import { API_BASE } from "../components/api";

type StationListResponseData = {
  items: AdminStation[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export default function GasStationAdminPage() {
  const { t } = useTranslation();
  const { state, email } = useAdminGuard();

  const [stations, setStations] = React.useState<AdminStation[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);

  const [pageNumber, setPageNumber] = React.useState(1);
  const [pageSize] = React.useState(10);
  const [totalPages, setTotalPages] = React.useState(1);

  const [search, setSearch] = React.useState("");

  const [activeModal, setActiveModal] =
    React.useState<"add" | "edit" | "delete" | null>(null);
  const [selectedStation, setSelectedStation] =
    React.useState<AdminStation | null>(null);

  React.useEffect(() => {
    if (state !== "allowed") return;
    (async () => {
      await loadStationsFromApi(pageNumber, pageSize, search);
    })();
  }, [state, pageNumber, pageSize, search, t]);

  async function loadStationsFromApi(
    page: number,
    size: number,
    searchValue: string,
  ) {
    setLoading(true);
    setError(null);

    try {
      const params = new URLSearchParams({
        PageNumber: String(page),
        PageSize: String(size),
        SortBy: "brandname",
        SortDirection: "asc",
      });

      if (searchValue.trim().length > 0) {
        params.set("Search", searchValue.trim());
      }

      const res = await fetch(
        `${API_BASE}/api/admin/station/list?${params.toString()}`,
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
        const msg = t("stationadmin.error_fetch", { status: res.status, text });
        throw new Error(msg);
      }

      const json = await res.json();
      const data: StationListResponseData | undefined = json.items
        ? (json as StationListResponseData)
        : json.data;

      if (!data || !Array.isArray(data.items)) {
        throw new Error(t("stationadmin.error_unexpected_response"));
      }

      setStations(data.items);
      setPageNumber(data.pageNumber);
      setTotalPages(data.totalPages);
    } catch (e: any) {
      console.error(e);
      setError(e?.message ?? t("stationadmin.error_fetch_fallback"));
      setStations([]);
    } finally {
      setLoading(false);
    }
  }

  const openAdd = () => { setSelectedStation(null); setActiveModal("add"); };
  const openEdit = (station: AdminStation) => { setSelectedStation(station); setActiveModal("edit"); };
  const openDelete = (station: AdminStation) => { setSelectedStation(station); setActiveModal("delete"); };
  const closeModal = () => { setActiveModal(null); setSelectedStation(null); };

  const handleAddConfirm = async (values: StationFormValues) => {
    try {
      const res = await fetch(`${API_BASE}/api/admin/station/add`, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        credentials: "include",
        body: JSON.stringify({
          brandName: values.brandName,
          street: values.street,
          houseNumber: values.houseNumber,
          city: values.city,
          postalCode: values.postalCode,
          latitude: values.latitude,
          longitude: values.longitude,
          fuelTypes: values.fuelTypes.map((f) => ({ name: f.code, code: f.code, price: f.price })),
        }),
      });

      if (!res.ok) {
        const text = await res.text();
        const msg = t("stationadmin.error_add", { status: res.status, text });
        throw new Error(msg);
      }

      await loadStationsFromApi(pageNumber, pageSize, search);
      closeModal();
    } catch (e: any) {
      console.error(e);
      alert(e instanceof Error ? e.message : t("stationadmin.error_add_fallback"));
    }
  };

  const handleEditConfirm = async (values: Partial<StationFormValues>) => {
    if (!selectedStation) return;
    try {
      const body: any = {
        findStation: {
          brandName: selectedStation.brandName,
          street: selectedStation.street,
          houseNumber: selectedStation.houseNumber,
          city: selectedStation.city,
        },
      };

      if (values.brandName) body.newBrandName = values.brandName;
      if (values.street) body.newStreet = values.street;
      if (values.houseNumber) body.newHouseNumber = values.houseNumber;
      if (values.city) body.newCity = values.city;
      if (values.postalCode) body.newPostalCode = values.postalCode;
      if (typeof values.latitude === "number") body.newLatitude = values.latitude;
      if (typeof values.longitude === "number") body.newLongitude = values.longitude;
      if (values.fuelTypes && values.fuelTypes.length > 0) {
        body.fuelType = values.fuelTypes.map((f) => ({ name: f.code, code: f.code, price: f.price }));
      }

      const res = await fetch(`${API_BASE}/api/admin/station/edit`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        credentials: "include",
        body: JSON.stringify(body),
      });

      if (!res.ok) {
        const text = await res.text();
        const msg = t("stationadmin.error_edit", { status: res.status, text });
        throw new Error(msg);
      }

      await loadStationsFromApi(pageNumber, pageSize, search);
      closeModal();
    } catch (e: any) {
      console.error(e);
      alert(e instanceof Error ? e.message : t("stationadmin.error_edit_fallback"));
    }
  };

  const handleDeleteConfirm = async () => {
    if (!selectedStation) return;
    try {
      const res = await fetch(`${API_BASE}/api/admin/station/delete`, {
        method: "DELETE",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        credentials: "include",
        body: JSON.stringify({
          brandName: selectedStation.brandName,
          street: selectedStation.street,
          houseNumber: selectedStation.houseNumber,
          city: selectedStation.city,
        }),
      });

      if (!res.ok) {
        const text = await res.text();
        const msg = t("stationadmin.error_delete", { status: res.status, text });
        throw new Error(msg);
      }

      await loadStationsFromApi(pageNumber, pageSize, search);
      closeModal();
    } catch (e: any) {
      console.error(e);
      alert(e instanceof Error ? e.message : t("stationadmin.error_delete_fallback"));
    }
  };

  const goToPage = (p: number) => {
    if (p < 1 || p > totalPages || p === pageNumber) return;
    setPageNumber(p);
  };

  function renderPageButtons() {
    const pages: number[] = [];
    const windowSize = 3; 
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
            <h1 className="text-2xl sm:text-3xl font-bold">{t("stationadmin.title")}</h1>
            <p className="text-sm text-base-content/70">
              {email ? t("stationadmin.logged_in_as", { email }) : t("stationadmin.checking_session")}
            </p>
          </div>
          <div className="flex gap-2 w-full sm:w-auto justify-end">
            <a href="/admin" className="btn btn-outline btn-sm">
              {t("stationadmin.back_to_admin")}
            </a>
            <button className="btn btn-primary btn-sm" onClick={openAdd} type="button">
              {t("stationadmin.add_station_button")}
            </button>
          </div>
        </div>

        <div className="bg-base-300 rounded-xl p-4 shadow-md mb-4">
          <div className="form-control w-full">
            <label className="label">
              <span className="label-text">{t("stationadmin.search_label")}</span>
            </label>
            <input
              type="text"
              className="input input-bordered input-sm w-full"
              placeholder={t("stationadmin.search_placeholder")}
              value={search}
              onChange={(e) => {
                setPageNumber(1);
                setSearch(e.target.value);
              }}
            />
          </div>
        </div>

        <div className="bg-base-300 rounded-xl p-4 shadow-md">
          {loading ? (
            <div className="text-sm">{t("stationadmin.loading")}</div>
          ) : error ? (
            <div className="text-sm text-error">{error}</div>
          ) : stations.length === 0 ? (
            <div className="text-sm">{t("stationadmin.no_stations")}</div>
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="table table-zebra table-sm w-full">
                  <thead>
                    <tr>
                      <th>{t("stationadmin.table_hash")}</th>
                      <th>{t("stationadmin.table_brand")}</th>
                      <th>{t("stationadmin.table_city")}</th>
                      <th>{t("stationadmin.table_street")}</th>
                      <th>{t("stationadmin.table_postal")}</th>
                      <th>{t("stationadmin.table_created")}</th>
                      <th>{t("stationadmin.table_updated")}</th>
                      <th className="text-right">{t("stationadmin.table_actions")}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {stations.map((s, idx) => (
                      <tr key={`${s.brandName}-${s.city}-${s.street}-${s.houseNumber}-${idx}`}>
                        <td>{idx + 1 + (pageNumber - 1) * pageSize}</td>
                        <td className="font-semibold">{s.brandName}</td>
                        <td>{s.city}</td>
                        <td className="whitespace-nowrap">{s.street} {s.houseNumber}</td>
                        <td>{s.postalCode}</td>
                        <td className="whitespace-nowrap">{s.createdAt ? new Date(s.createdAt).toLocaleDateString() : "-"}</td>
                        <td className="whitespace-nowrap">{s.updatedAt ? new Date(s.updatedAt).toLocaleDateString() : "-"}</td>
                        <td>
                          <div className="flex justify-end gap-2">
                            <button className="btn btn-xs" type="button" onClick={() => openEdit(s)}>
                              {t("stationadmin.edit_button")}
                            </button>
                            <button className="btn btn-xs btn-error" type="button" onClick={() => openDelete(s)}>
                              {t("stationadmin.delete_button")}
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
                  {t("stationadmin.page_info", { page: pageNumber, total: totalPages })}
                </div>
              </div>
            </>
          )}
        </div>
      </main>

      <Footer />

      <AddStationModal isOpen={activeModal === "add"} onClose={closeModal} onConfirm={handleAddConfirm} />
      <EditStationModal isOpen={activeModal === "edit"} onClose={closeModal} station={selectedStation} onConfirm={handleEditConfirm} />
      <DeleteStationModal isOpen={activeModal === "delete"} onClose={closeModal} station={selectedStation} onConfirm={handleDeleteConfirm} />
    </div>
  );
}