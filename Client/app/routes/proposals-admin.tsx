import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";

import { useAdminGuard } from "../components/useAdminGuard";
import { API_BASE } from "../components/api";
import { useTranslation } from "react-i18next";

import {
  ReviewProposalModal,
  type ProposalGroup,
  type ProposalGroupItem,
} from "../components/proposal-admin-modals";

type ProposalListItem = {
  userName: string;
  brandName: string;
  street: string;
  houseNumber: string;
  city: string;
  fuelName: string;
  fuelCode: string;
  proposedPrice: number;
  status: string;
  token: string;
  createdAt: string;
};

type ProposalListResponseData = {
  items: ProposalListItem[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage?: boolean;
  hasNextPage?: boolean;
};

type StatsData = {
  acceptedRate: number;
  rejectedRate: number;
  pendingRate: number;
};

export default function ProposalAdminPage() {
  const { t } = useTranslation();
  const { state, email } = useAdminGuard();

  const [groups, setGroups] = React.useState<ProposalGroup[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);

  const [pageNumber, setPageNumber] = React.useState(1);
  const [pageSize] = React.useState(10);
  const [totalPages, setTotalPages] = React.useState(1);
  const [totalCount, setTotalCount] = React.useState(0);

  const [search, setSearch] = React.useState("");
  const [sortBy, setSortBy] = React.useState("createdat");
  const [sortDirection, setSortDirection] =
    React.useState<"asc" | "desc">("desc");

  const [activeGroup, setActiveGroup] = React.useState<ProposalGroup | null>(null);
  const [modalLoading, setModalLoading] = React.useState(false);
  const [modalError, setModalError] = React.useState<string | null>(null);
  const [photosLoading, setPhotosLoading] = React.useState(false);

  const [stats, setStats] = React.useState<StatsData | null>(null);
  const [statsLoading, setStatsLoading] = React.useState(false);

  React.useEffect(() => {
    if (state !== "allowed") return;
    (async () => {
      await loadProposalsFromApi(pageNumber, pageSize, search, sortBy, sortDirection);
      loadStats(); 
    })();
  }, [state, pageNumber, pageSize, search, sortBy, sortDirection]);

  async function loadStats() {
    setStatsLoading(true);
    try {
      const res = await fetch(`${API_BASE}/api/admin/proposal/stats`, {
        method: "GET",
        headers: { Accept: "application/json" },
        credentials: "include",
      });
      if (res.ok) {
        const data = await res.json();
        setStats(data);
      }
    } catch (e) {
      console.warn("Błąd pobierania statystyk", e);
    } finally {
      setStatsLoading(false);
    }
  }

  async function loadProposalsFromApi(
    page: number,
    size: number,
    searchValue: string,
    sortField: string,
    direction: "asc" | "desc",
  ) {
    setLoading(true);
    setError(null);

    try {
      const params = new URLSearchParams({
        PageNumber: String(page),
        PageSize: String(size),
        SortBy: sortField || "createdat",
        SortDirection: direction,
      });

      if (searchValue.trim().length > 0) {
        params.set("Search", searchValue.trim());
      }

      const res = await fetch(
        `${API_BASE}/api/admin/proposal/list?${params.toString()}`,
        {
          method: "GET",
          headers: { Accept: "application/json" },
          credentials: "include",
        },
      );

      if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(t("proposaladmin.error_fetch", { status: res.status, text }));
      }

      const json = await res.json();
      const data: ProposalListResponseData | undefined = json.items
        ? (json as ProposalListResponseData)
        : json.data;

      if (!data || !Array.isArray(data.items)) {
        throw new Error(t("proposaladmin.error_unexpected_response"));
      }

      const grouped = groupProposals(data.items);

      setGroups(grouped);
      setPageNumber(data.pageNumber);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
    } catch (e: any) {
      console.error(e);
      setError(e?.message ?? t("proposaladmin.error_fetch_fallback"));
      setGroups([]);
    } finally {
      setLoading(false);
    }
  }

  function groupProposals(items: ProposalListItem[]): ProposalGroup[] {
    const map = new Map<string, ProposalGroup>();

    for (const it of items) {
      const key = [it.userName, it.brandName, it.street, it.houseNumber, it.city].join("|");
      let group = map.get(key);
      if (!group) {
        group = {
          id: key,
          userName: it.userName,
          brandName: it.brandName,
          street: it.street,
          houseNumber: it.houseNumber,
          city: it.city,
          createdAt: it.createdAt,
          items: [],
        };
        map.set(key, group);
      }
      const item: ProposalGroupItem = {
        token: it.token,
        fuelName: it.fuelName,
        fuelCode: it.fuelCode,
        proposedPrice: it.proposedPrice,
        status: it.status,
      };
      group.items.push(item);
    }
    return Array.from(map.values());
  }

  const openReview = (group: ProposalGroup) => {
    setActiveGroup(group);
    setModalError(null);
    fetchPhotosForGroup(group);
  };

  const closeReview = () => {
    setActiveGroup(null);
    setModalError(null);
    setModalLoading(false);
    loadStats();
  };

  async function fetchPhotosForGroup(group: ProposalGroup) {
    setPhotosLoading(true);
    try {
      const first = group.items[0];
      if (!first) return;

      const res = await fetch(
        `${API_BASE}/api/admin/proposal?token=${encodeURIComponent(first.token)}`,
        {
          method: "GET",
          headers: { Accept: "application/json" },
          credentials: "include",
        },
      );

      if (!res.ok) throw new Error(`photo-fetch-failed ${res.status}`);
      const data = await res.json();
      const photoUrl = data.photoUrl ?? null;

      setActiveGroup((prev) =>
        prev && prev.id === group.id
          ? { ...prev, items: prev.items.map((it) => ({ ...it, photoUrl })) }
          : prev,
      );
    } catch (err) {
      console.warn("Nie udało się pobrać zdjęcia dla grupy", group.id, err);
    } finally {
      setPhotosLoading(false);
    }
  }

  async function changeStatusForToken(token: string, isAccepted: boolean): Promise<void> {
    const res = await fetch(
      `${API_BASE}/api/admin/proposal/change-status?token=${encodeURIComponent(token)}&isAccepted=${isAccepted}`,
      {
        method: "PATCH",
        headers: { Accept: "application/json" },
        credentials: "include",
      },
    );

    const text = await res.text().catch(() => "");
    let json: any = null;
    try { json = text ? JSON.parse(text) : null; } catch { json = null; }

    if (!res.ok || (json && json.success === false)) {
      const msg = (json && (json.message || json.error)) || t("proposaladmin.error_change_status", { status: res.status });
      throw new Error(msg);
    }
  }

  const reloadList = async () => {
    await loadProposalsFromApi(pageNumber, pageSize, search, sortBy, sortDirection);
  };

  const handleAcceptSingle = async (token: string) => {
    if (!activeGroup) return;
    setModalLoading(true);
    setModalError(null);
    try {
      await changeStatusForToken(token, true);
      setActiveGroup((prev) => prev ? { ...prev, items: prev.items.map((it) => it.token === token ? { ...it, status: "Accepted" } : it) } : prev);
      await reloadList();
    } catch (e: any) {
      console.error(e);
      setModalError(e?.message ?? t("proposaladmin.modal_error_update"));
    } finally {
      setModalLoading(false);
    }
  };

  const handleRejectSingle = async (token: string) => {
    if (!activeGroup) return;
    setModalLoading(true);
    setModalError(null);
    try {
      await changeStatusForToken(token, false);
      setActiveGroup((prev) => prev ? { ...prev, items: prev.items.map((it) => it.token === token ? { ...it, status: "Rejected" } : it) } : prev);
      await reloadList();
    } catch (e: any) {
      console.error(e);
      setModalError(e?.message ?? t("proposaladmin.modal_error_update"));
    } finally {
      setModalLoading(false);
    }
  };

  const handleAcceptAll = async () => {
    if (!activeGroup) return;
    setModalLoading(true);
    setModalError(null);
    try {
      const pendingItems = activeGroup.items.filter((it) => it.status === "Pending" || it.status === "pending");
      for (const it of pendingItems) { await changeStatusForToken(it.token, true); }
      await reloadList();
      closeReview();
    } catch (e: any) {
      console.error(e);
      setModalError(e?.message ?? "Nie udało się zaakceptować wszystkich propozycji.");
    } finally {
      setModalLoading(false);
    }
  };

  const handleRejectAll = async () => {
    if (!activeGroup) return;
    setModalLoading(true);
    setModalError(null);
    try {
      const pendingItems = activeGroup.items.filter((it) => it.status === "Pending" || it.status === "pending");
      for (const it of pendingItems) { await changeStatusForToken(it.token, false); }
      await reloadList();
      closeReview();
    } catch (e: any) {
      console.error(e);
      setModalError(e?.message ?? "Nie udało się odrzucić wszystkich propozycji.");
    } finally {
      setModalLoading(false);
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
        <button className="btn btn-sm" onClick={() => goToPage(1)} disabled={pageNumber === 1}>«</button>
        <button className="btn btn-sm" onClick={() => goToPage(pageNumber - 1)} disabled={pageNumber === 1}>←</button>
        {pages.map((p) => (
          <button key={p} className={`btn btn-sm ${p === pageNumber ? "btn-active" : ""}`} onClick={() => goToPage(p)}>{p}</button>
        ))}
        <button className="btn btn-sm" onClick={() => goToPage(pageNumber + 1)} disabled={pageNumber === totalPages}>→</button>
        <button className="btn btn-sm" onClick={() => goToPage(totalPages)} disabled={pageNumber === totalPages}>»</button>
      </div>
    );
  }

  const formatDate = (iso?: string) => iso ? new Date(iso).toLocaleString() : "-";

  if (state === "checking") return <div className="min-h-screen bg-base-200 flex items-center justify-center"><span className="loading loading-spinner loading-lg" /></div>;
  if (state !== "allowed") return null;

  return (
    <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
      <Header />

      <main className="flex-1 mx-auto w-full max-w-6xl px-4 py-6 sm:py-10">
        
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-4">
          <div>
            <h1 className="text-2xl sm:text-3xl font-bold">{t("proposaladmin.title")}</h1>
            <p className="text-sm text-base-content/70">
              {email ? t("proposaladmin.logged_in_as", { email }) : t("proposaladmin.checking_session")}
            </p>
          </div>
          <a href="/admin" className="btn btn-outline btn-sm">
            {t("proposaladmin.back_to_admin")}
          </a>
        </div>

        {/* --- STATYSTYKI --- */}
        <div className="mt-6 mb-6 grid grid-cols-1 md:grid-cols-3 gap-4">
           <div className="bg-base-300 rounded-xl p-4 shadow-md flex flex-col items-center">
              <h3 className="text-lg font-semibold mb-1">Zgłoszenia Zaakceptowane</h3>
              {statsLoading ? <span className="loading loading-dots"/> : (
                 <span className="text-3xl font-bold text-success">{stats?.acceptedRate ?? "-"}</span>
              )}
           </div>
           <div className="bg-base-300 rounded-xl p-4 shadow-md flex flex-col items-center">
              <h3 className="text-lg font-semibold mb-1">Zgłoszenia Oczekujące</h3>
              {statsLoading ? <span className="loading loading-dots"/> : (
                 <span className="text-3xl font-bold text-warning">{stats?.pendingRate ?? "-"}</span>
              )}
           </div>
           <div className="bg-base-300 rounded-xl p-4 shadow-md flex flex-col items-center">
              <h3 className="text-lg font-semibold mb-1">Zgłoszenia Odrzucone</h3>
              {statsLoading ? <span className="loading loading-dots"/> : (
                 <span className="text-3xl font-bold text-error">{stats?.rejectedRate ?? "-"}</span>
              )}
           </div>
        </div>

        <div className="bg-base-300 rounded-xl p-4 shadow-md mb-4 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div className="flex flex-col gap-2 w-full md:w-auto md:flex-row md:items-end">
            <div className="form-control w-full md:w-auto">
              <label className="label"><span className="label-text">{t("proposaladmin.search_label")}</span></label>
              <input
                className="input input-bordered input-sm w-full md:w-72"
                value={search}
                onChange={(e) => { setSearch(e.target.value); setPageNumber(1); }}
                placeholder={t("proposaladmin.search_placeholder")}
              />
            </div>

            <div className="form-control w-full md:w-auto">
              <label className="label"><span className="label-text">{t("proposaladmin.sort_label")}</span></label>
              <select
                className="select select-bordered select-sm w-full md:w-auto"
                value={sortBy}
                onChange={(e) => { setSortBy(e.target.value); setPageNumber(1); }}
              >
                <option value="createdat">{t("proposaladmin.sort_option_createdat")}</option>
                <option value="username">{t("proposaladmin.sort_option_username")}</option>
                <option value="brandname">{t("proposaladmin.sort_option_brandname")}</option>
                <option value="city">{t("proposaladmin.sort_option_city")}</option>
                <option value="fuelname">{t("proposaladmin.sort_option_fuelname")}</option>
                <option value="fuelcode">{t("proposaladmin.sort_option_fuelcode")}</option>
                <option value="proposedprice">{t("proposaladmin.sort_option_proposedprice")}</option>
              </select>
            </div>

            <div className="form-control w-full md:w-auto">
              <label className="label"><span className="label-text">{t("proposaladmin.sort_dir_label")}</span></label>
              <select
                className="select select-bordered select-sm w-full md:w-auto"
                value={sortDirection}
                onChange={(e) => setSortDirection(e.target.value as "asc" | "desc")}
              >
                <option value="desc">{t("proposaladmin.sort_dir_desc")}</option>
                <option value="asc">{t("proposaladmin.sort_dir_asc")}</option>
              </select>
            </div>
          </div>
        </div>

        <div className="bg-base-300 rounded-xl p-4 shadow-md">
          {loading ? (
            <div className="text-sm">{t("proposaladmin.loading")}</div>
          ) : error ? (
            <div className="text-sm text-error">{error}</div>
          ) : groups.length === 0 ? (
            <div className="text-sm">{t("proposaladmin.no_items")}</div>
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="table table-zebra table-sm w-full">
                  <thead>
                    <tr>
                      <th>{t("proposaladmin.th_hash")}</th>
                      <th>{t("proposaladmin.th_user")}</th>
                      <th>{t("proposaladmin.th_station")}</th>
                      <th>{t("proposaladmin.th_address")}</th>
                      <th>{t("proposaladmin.th_count")}</th>
                      <th>{t("proposaladmin.th_fuels")}</th>
                      <th>{t("proposaladmin.th_date")}</th>
                      <th className="text-right">{t("proposaladmin.th_actions")}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {groups.map((g, idx) => (
                      <tr key={g.id}>
                        <td>{idx + 1 + (pageNumber - 1) * pageSize}</td>
                        <td className="font-semibold">{g.userName}</td>
                        <td>{g.brandName}</td>
                        <td className="whitespace-normal min-w-[150px]">{g.street} {g.houseNumber}, {g.city}</td>
                        <td>{g.items.length}</td>
                        <td className="whitespace-normal min-w-[150px]">
                          {g.items.map((it) => `${it.fuelName} (${it.fuelCode})`).join(", ")}
                        </td>
                        <td className="whitespace-nowrap">{formatDate(g.createdAt)}</td>
                        <td>
                          <div className="flex justify-end">
                            <button
                              className="btn btn-xs btn-primary"
                              type="button"
                              onClick={() => openReview(g)}
                            >
                              {t("proposaladmin.btn_review")}
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
                  {t("proposaladmin.page_info", { page: pageNumber, total: totalPages })}
                </div>
              </div>
            </>
          )}
        </div>
      </main>

      <Footer />

      <ReviewProposalModal
        isOpen={!!activeGroup}
        group={activeGroup}
        loading={modalLoading}
        error={modalError}
        onClose={closeReview}
        onAcceptAll={handleAcceptAll}
        onRejectAll={handleRejectAll}
        onAcceptSingle={handleAcceptSingle}
        onRejectSingle={handleRejectSingle}
        photosLoading={photosLoading}
      />
    </div>
  );
}