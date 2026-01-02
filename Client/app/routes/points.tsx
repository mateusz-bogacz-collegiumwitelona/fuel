import React from "react";
import { API_BASE } from "../components/api";
import Header from "../components/header";
import Footer from "../components/footer";
import { useTranslation } from "react-i18next";

export type UserPointsItem = {
  id: string;
  name: string;
  points: number;
  totalProposals?: number;
  approvedProposals?: number;
  rejectedProposals?: number;
  acceptedRate?: number | null;
};

export default function UsersPoints(): JSX.Element {
  const { t } = useTranslation();
  const [users, setUsers] = React.useState<UserPointsItem[] | null>(null);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  // Pagination (server-side)
  const [pageNumber, setPageNumber] = React.useState<number>(1);
  const [pageSize, setPageSize] = React.useState<number>(10);
  const [totalPages, setTotalPages] = React.useState<number>(1);
  const [totalCount, setTotalCount] = React.useState<number | null>(null);

  React.useEffect(() => {
    let abort = false;
    (async () => {
      setLoading(true);
      setError(null);

      const possible = [
        "api/proposal-statistic/top-users",
      ];

      for (const ep of possible) {
        try {
          const url = `${API_BASE}${ep}?PageNumber=${pageNumber}&PageSize=${pageSize}`;
          console.log("[UsersPoints] trying", url);
          const res = await fetch(url, {
            headers: { Accept: "application/json" },
            credentials: "include",
          });

          console.log(`[UsersPoints] ${url} -> ${res.status}`);
          if (res.status === 404) {
            // try next endpoint
            continue;
          }
          if (!res.ok) {
            const text = await res.text().catch(() => "");
            throw new Error(`Błąd ${res.status}: ${text}`);
          }

          const data = await res.json();
          const items = Array.isArray(data.items) ? data.items : Array.isArray(data) ? data : [];

          const normalized = items.map((x: any, idx: number) => ({
            id: String(x.userName ?? x.userId ?? x.id ?? `u_${idx}`),
            name: String(x.userName ?? x.name ?? x.username ?? "Unknown"),
            points: Number(x.points ?? x.score ?? 0),
            totalProposals: x.totalProposals ?? x.total ?? undefined,
            approvedProposals: x.approvedProposals ?? x.approved ?? undefined,
            rejectedProposals: x.rejectedProposals ?? x.rejected ?? undefined,
            acceptedRate: x.acceptedRate ?? null,
          }));

          if (!abort) {
            setUsers(normalized);
            setTotalCount(typeof data.totalCount === 'number' ? data.totalCount : items.length);
            setTotalPages(typeof data.totalPages === 'number' ? data.totalPages : 1);
            setPageNumber(typeof data.pageNumber === 'number' ? data.pageNumber : pageNumber);
          }
          setLoading(false);
          return;
        } catch (err: any) {
          console.warn("[UsersPoints] fetch error for", ep, err);
        }
      }

      // nothing works
      if (!abort) {
        setError("Nie znaleziono endpointu top-users (404). Sprawdź ścieżkę API na backendzie.");
        setUsers([]);
        setLoading(false);
      }
    })();

    return () => { abort = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageNumber, pageSize]);

  const usersList = users ?? [];

  const maxPointsVal = React.useMemo(() => {
    if (!usersList || usersList.length === 0) return 0;
    return Math.max(...usersList.map((u) => u.points));
  }, [usersList]);

  function goToPage(p: number) {
    if (p < 1) p = 1;
    if (p > totalPages) p = totalPages;
    setPageNumber(p);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

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

  return (
    <div className="min-h-screen bg-base-200 text-base-content">
      <Header />
      <main className="mx-auto max-w-4xl px-4 py-8">
        <h1 className="text-2xl font-bold mb-4">{t ? t("points.pointsTitle") : "Użytkownicy — punkty"}</h1>

        <section className="bg-base-300 p-4 rounded-xl shadow-md">
          <div className="flex justify-between items-center mb-4">
            <h2 className="text-lg font-semibold">{t ? t("points.leaderboard") : "Tabela"} <span className="text-sm text-gray-400">({totalCount ?? 0})</span></h2>

            <div className="flex items-center gap-2">
              <label className="text-sm text-gray-500">{t ? t("points.perPage") : "Na stronę"}</label>
              <select
                value={pageSize}
                onChange={(e) => { setPageSize(Number(e.target.value)); setPageNumber(1); }}
                className="select select-sm select-bordered"
              >
                {[5, 10, 20, 50].map(n => (
                  <option key={n} value={n}>{n}</option>
                ))}
              </select>
            </div>
          </div>

          {loading ? (
            <div className="text-center py-8">
              <span className="loading loading-spinner loading-lg"></span>
            </div>
          ) : error ? (
            <div className="alert alert-error">{error}</div>
          ) : usersList && usersList.length > 0 ? (
            <div className="space-y-3">
              {usersList.map((u) => {
                const pct = maxPointsVal > 0 ? Math.max(3, Math.round((u.points / maxPointsVal) * 100)) : 3;
                return (
                  <div key={u.id} className="p-3 bg-base-100 rounded flex items-center gap-4">
                    <div className="w-36 min-w-[9rem]">
                      <div className="font-medium">{u.name}</div>
                      <div className="text-sm text-gray-500">{u.points} pkt</div>
                    </div>

                    <div className="flex-1">
                      <div className="h-6 bg-base-200 rounded overflow-hidden relative">
                        <div
                          className="h-full rounded-l shadow-inner"
                          style={{ width: `${pct}%`, minWidth: "3%" }}
                          aria-hidden
                        />
                        <div className="absolute inset-0 flex items-center justify-center text-sm font-medium pointer-events-none">
                          {u.points}
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}

              <div className="mt-6 flex justify-between items-center">
                {renderPageButtons()}
                <div className="text-sm text-gray-400">
                  {t ? t("list.page") : "Strona"} {pageNumber} / {totalPages}
                </div>
              </div>
            </div>
          ) : (
            <div className="text-center py-8 text-gray-400">{t ? t("points.nousers") : "Brak użytkowników"}</div>
          )}
        </section>
      </main>
      <Footer />
    </div>
  );
}
