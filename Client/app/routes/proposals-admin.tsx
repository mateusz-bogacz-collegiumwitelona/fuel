import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";

import { useAdminGuard } from "../components/useAdminGuard";
import { API_BASE } from "../components/api";

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

export default function ProposalAdminPage() {
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


  const [activeGroup, setActiveGroup] = React.useState<ProposalGroup | null>(
    null,
  );
  const [modalLoading, setModalLoading] = React.useState(false);
  const [modalError, setModalError] = React.useState<string | null>(null);
  const [photosLoading, setPhotosLoading] = React.useState(false);

  React.useEffect(() => {
    if (state !== "allowed") return;
    (async () => {
      await loadProposalsFromApi(
        pageNumber,
        pageSize,
        search,
        sortBy,
        sortDirection,
      );
    })();
  }, [state, pageNumber, pageSize, search, sortBy, sortDirection]);

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
          headers: {
            Accept: "application/json",
          },
          credentials: "include",
        },
      );

      if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(
          `Błąd pobierania propozycji cen (${res.status}): ${text}`,
        );
      }

      const json = await res.json();
      const data: ProposalListResponseData | undefined = json.items
        ? (json as ProposalListResponseData)
        : json.data;

      if (!data || !Array.isArray(data.items)) {
        throw new Error("Nieoczekiwany format odpowiedzi propozycji cen");
      }

      const grouped = groupProposals(data.items);

      setGroups(grouped);
      setPageNumber(data.pageNumber);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
    } catch (e: any) {
      console.error(e);
      setError(e?.message ?? "Nie udało się pobrać listy propozycji cen");
      setGroups([]);
    } finally {
      setLoading(false);
    }
  }

  function groupProposals(items: ProposalListItem[]): ProposalGroup[] {
    const map = new Map<string, ProposalGroup>();

    for (const it of items) {
      const key = [
        it.userName,
        it.brandName,
        it.street,
        it.houseNumber,
        it.city,
      ].join("|");

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
        ? {
            ...prev,
            items: prev.items.map((it) => ({
              ...it,
              photoUrl,
            })),
          }
        : prev,
    );
  } catch (err) {
    console.warn("Nie udało się pobrać zdjęcia dla grupy", group.id, err);
  } finally {
    setPhotosLoading(false);
  }
}

  async function changeStatusForToken(
    token: string,
    isAccepted: boolean,
  ): Promise<void> {
    const res = await fetch(
      `${API_BASE}/api/admin/proposal/change-status?token=${encodeURIComponent(
        token,
      )}&isAccepted=${isAccepted}`,
      {
        method: "PATCH",
        headers: {
          Accept: "application/json",
        },
        credentials: "include",
      },
    );

    const text = await res.text().catch(() => "");
    let json: any = null;
    try {
      json = text ? JSON.parse(text) : null;
    } catch {
      json = null;
    }

    if (!res.ok || (json && json.success === false)) {
      const msg =
        (json && (json.message || json.error)) ||
        `Nie udało się zmienić statusu propozycji (${res.status})`;
      throw new Error(msg);
    }
  }

  const reloadList = async () => {
    await loadProposalsFromApi(
      pageNumber,
      pageSize,
      search,
      sortBy,
      sortDirection,
    );
  };

  const handleAcceptSingle = async (token: string) => {
    if (!activeGroup) return;
    setModalLoading(true);
    setModalError(null);
    try {
      await changeStatusForToken(token, true);

      setActiveGroup((prev) =>
        prev
          ? {
              ...prev,
              items: prev.items.map((it) =>
                it.token === token ? { ...it, status: "Accepted" } : it,
              ),
            }
          : prev,
      );

      await reloadList();
    } catch (e: any) {
      console.error(e);
      setModalError(
        e?.message ?? "Nie udało się zaktualizować statusu propozycji.",
      );
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

      setActiveGroup((prev) =>
        prev
          ? {
              ...prev,
              items: prev.items.map((it) =>
                it.token === token ? { ...it, status: "Rejected" } : it,
              ),
            }
          : prev,
      );

      await reloadList();
    } catch (e: any) {
      console.error(e);
      setModalError(
        e?.message ?? "Nie udało się zaktualizować statusu propozycji.",
      );
    } finally {
      setModalLoading(false);
    }
  };

  const handleAcceptAll = async () => {
    if (!activeGroup) return;
    setModalLoading(true);
    setModalError(null);
    try {
      for (const it of activeGroup.items) {
        await changeStatusForToken(it.token, true);
      }
      await reloadList();
      closeReview();
    } catch (e: any) {
      console.error(e);
      setModalError(
        e?.message ?? "Nie udało się zaakceptować wszystkich propozycji.",
      );
    } finally {
      setModalLoading(false);
    }
  };

  const handleRejectAll = async () => {
    if (!activeGroup) return;
    setModalLoading(true);
    setModalError(null);
    try {
      for (const it of activeGroup.items) {
        await changeStatusForToken(it.token, false);
      }
      await reloadList();
      closeReview();
    } catch (e: any) {
      console.error(e);
      setModalError(
        e?.message ?? "Nie udało się odrzucić wszystkich propozycji.",
      );
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
    const windowSize = 5;
    let start = Math.max(1, pageNumber - Math.floor(windowSize / 2));
    let end = start + windowSize - 1;

    if (end > totalPages) {
      end = totalPages;
      start = Math.max(1, end - windowSize + 1);
    }

    for (let i = start; i <= end; i++) pages.push(i);

    return (
      <div className="flex items-center gap-2">
        <button
          className="btn btn-sm"
          onClick={() => goToPage(1)}
          disabled={pageNumber === 1}
        >
          «1
        </button>

        <button
          className="btn btn-sm"
          onClick={() => goToPage(pageNumber - 1)}
          disabled={pageNumber === 1}
        >
          ←
        </button>

        {pages.map((p) => (
          <button
            key={p}
            className={`btn btn-sm ${p === pageNumber ? "btn-active" : ""}`}
            onClick={() => goToPage(p)}
          >
            {p}
          </button>
        ))}

        <button
          className="btn btn-sm"
          onClick={() => goToPage(pageNumber + 1)}
          disabled={pageNumber === totalPages}
        >
          →
        </button>
        <button
          className="btn btn-sm"
          onClick={() => goToPage(totalPages)}
          disabled={pageNumber === totalPages}
        >
          {totalPages} »
        </button>
      </div>
    );
  }

  const formatDate = (iso?: string) =>
    iso ? new Date(iso).toLocaleString() : "-";

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
            <h1 className="text-3xl font-bold">
              Panel administracyjny – propozycje cen
            </h1>
            <p className="text-sm text-base-content/70">
              {email
                ? `Zalogowano jako: ${email}`
                : "Sprawdzanie sesji administratora..."}
            </p>
          </div>
          <a href="/admin-dashboard" className="btn btn-outline btn-sm">
            ← Powrót do panelu administratora
          </a>
        </div>


        <div className="bg-base-300 rounded-xl p-4 shadow-md mb-4 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div className="flex flex-col gap-2 md:flex-row md:items-end">
            <div className="form-control">
              <label className="label">
                <span className="label-text">
                  Szukaj (użytkownik, marka, adres, paliwo, cena)
                </span>
              </label>
              <input
                className="input input-bordered input-sm w-full md:w-72"
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value);
                  setPageNumber(1);
                }}
                placeholder="np. Orlen, ON, 6.35"
              />
            </div>

            <div className="form-control">
              <label className="label">
                <span className="label-text">Sortowanie</span>
              </label>
              <select
                className="select select-bordered select-sm"
                value={sortBy}
                onChange={(e) => {
                  setSortBy(e.target.value);
                  setPageNumber(1);
                }}
              >
                <option value="createdat">Data zgłoszenia</option>
                <option value="username">Nazwa użytkownika</option>
                <option value="brandname">Marka stacji</option>
                <option value="city">Miasto</option>
                <option value="fuelname">Rodzaj paliwa</option>
                <option value="fuelcode">Kod paliwa</option>
                <option value="proposedprice">Zaproponowana cena</option>
              </select>
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
                <option value="desc">Najnowsze najpierw</option>
                <option value="asc">Najstarsze najpierw</option>
              </select>
            </div>
          </div>

          <div className="text-sm text-base-content/70">
            Łącznie pozycji (paliw):{" "}
            <span className="font-semibold">{totalCount}</span>
          </div>
        </div>


        <div className="bg-base-300 rounded-xl p-4 shadow-md">
          {loading ? (
            <div className="text-sm">Ładowanie propozycji...</div>
          ) : error ? (
            <div className="text-sm text-error">{error}</div>
          ) : groups.length === 0 ? (
            <div className="text-sm">
              Brak oczekujących propozycji cen na tej stronie.
            </div>
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="table table-zebra table-sm w-full">
                  <thead>
                    <tr>
                      <th>#</th>
                      <th>Użytkownik</th>
                      <th>Stacja</th>
                      <th>Adres</th>
                      <th>Liczba paliw</th>
                      <th>Paliwa</th>
                      <th>Data</th>
                      <th className="text-right">Akcje</th>
                    </tr>
                  </thead>
                  <tbody>
                    {groups.map((g, idx) => (
                      <tr key={g.id}>
                        <td>{idx + 1 + (pageNumber - 1) * pageSize}</td>
                        <td>{g.userName}</td>
                        <td>{g.brandName}</td>
                        <td>
                          {g.street} {g.houseNumber}, {g.city}
                        </td>
                        <td>{g.items.length}</td>
                        <td>
                          {g.items
                            .map((it) => `${it.fuelName} (${it.fuelCode})`)
                            .join(", ")}
                        </td>
                        <td>{formatDate(g.createdAt)}</td>
                        <td>
                          <div className="flex justify-end">
                            <button
                              className="btn btn-xs btn-primary"
                              type="button"
                              onClick={() => openReview(g)}
                            >
                              przejrzyj
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="mt-4 flex justify-between items-center text-sm">
                {renderPageButtons()}
                <div className="text-base-content/70">
                  Strona {pageNumber} / {totalPages}
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
