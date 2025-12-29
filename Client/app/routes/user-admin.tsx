import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";

import {
  ChangeRoleModal,
  BanUserModal,
  ReviewBanModal,
  UnlockUserModal,
  UserReportsModal,
} from "../components/user-admin-modals";

import type {
  AdminUser,
  ChangeRoleForm,
  BanForm,
  BanInfo,
} from "../components/user-admin-modals";

import { useAdminGuard } from "../components/useAdminGuard";
import { useTranslation } from "react-i18next";
import { API_BASE } from "../components/api";

type UserListResponseData = {
  items: AdminUser[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage?: boolean;
  hasNextPage?: boolean;
};

export default function UserAdminPage() {
  const { t } = useTranslation();
  const { state, email } = useAdminGuard();

  const [users, setUsers] = React.useState<AdminUser[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);

  const [pageNumber, setPageNumber] = React.useState(1);
  const [pageSize] = React.useState(10);
  const [totalPages, setTotalPages] = React.useState(1);
  const [search, setSearch] = React.useState("");
  const [sortBy, setSortBy] = React.useState("username");
  const [sortDirection, setSortDirection] = React.useState<"asc" | "desc">("asc");

  const [onlyReported, setOnlyReported] = React.useState(false);

  const [activeModal, setActiveModal] = React.useState<
    "role" | "ban" | "review" | "unlock" | "reports" | null
  >(null);
  const [selectedUser, setSelectedUser] = React.useState<AdminUser | null>(null);

  const [banInfo, setBanInfo] = React.useState<BanInfo | null>(null);
  const [banInfoLoading, setBanInfoLoading] = React.useState(false);
  const [banInfoError, setBanInfoError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (state !== "allowed") return;
    (async () => {
      await loadUsersFromApi(pageNumber, pageSize, search, sortBy, sortDirection, onlyReported);
    })();
  }, [state, pageNumber, pageSize, search, sortBy, sortDirection, onlyReported]);

  async function loadUsersFromApi(
    page: number,
    size: number,
    searchValue: string,
    sort: string,
    direction: "asc" | "desc",
    showReported: boolean
  ) {
    setLoading(true);
    setError(null);

    try {
      const params = new URLSearchParams({
        PageNumber: String(page),
        PageSize: String(size),
        SortBy: sort || "username",
        SortDirection: direction,
      });

      if (searchValue.trim().length > 0) {
        params.set("Search", searchValue.trim());
      }

      if (showReported) {
        params.set("OnlyReported", "true");
      }

      const res = await fetch(
        `${API_BASE}/api/admin/user/list?${params.toString()}`,
        {
          method: "GET",
          headers: { Accept: "application/json" },
          credentials: "include",
        },
      );

      if (!res.ok) {
        const text = await res.text();
        const msg = t("useradmin.error_fetch", { status: res.status, text });
        throw new Error(msg);
      }

      const json = await res.json();
      const data: UserListResponseData | undefined = json.data
        ? (json.data as UserListResponseData)
        : (json as UserListResponseData);

      if (!data || !Array.isArray(data.items)) {
        throw new Error(t("useradmin.error_unexpected_response"));
      }

      setUsers(data.items);
      setPageNumber(data.pageNumber);
      setTotalPages(data.totalPages);
    } catch (e: any) {
      console.error(e);
      setError(e?.message ?? t("useradmin.error_fetch_fallback"));
      setUsers([]);
    } finally {
      setLoading(false);
    }
  }

  const displayedUsers = React.useMemo(() => {
    if (onlyReported) {
      return users.filter((u) => u.hasReport);
    }
    return users;
  }, [users, onlyReported]);

  const openRoleModal = (user: AdminUser) => {
    setSelectedUser(user);
    setActiveModal("role");
  };

  const openBanModal = (user: AdminUser) => {
    setSelectedUser(user);
    setActiveModal("ban");
  };

  const openUnlockModal = (user: AdminUser) => {
    setSelectedUser(user);
    setActiveModal("unlock");
  };

  const openReportsModal = (user: AdminUser) => {
    setSelectedUser(user);
    setActiveModal("reports");
  };

  const openReviewModal = async (user: AdminUser) => {
    setSelectedUser(user);
    setActiveModal("review");
    setBanInfo(null);
    setBanInfoError(null);
    setBanInfoLoading(true);

    try {
      const res = await fetch(
        `${API_BASE}/api/admin/user/lock-out/review?email=${encodeURIComponent(user.email)}`,
        {
          method: "GET",
          headers: { Accept: "application/json" },
          credentials: "include",
        },
      );

      if (!res.ok) {
        const text = await res.text();
        setBanInfoError(t("useradmin.error_fetch_baninfo", { status: res.status, text }));
        setBanInfo(null);
      } else {
        const json = await res.json();
        if (json.data) {
          setBanInfo(json.data as BanInfo);
        } else {
          setBanInfo(null);
        }
      }
    } catch (e: any) {
      console.error(e);
      setBanInfoError(e?.message ?? t("useradmin.error_fetch_baninfo_fallback"));
      setBanInfo(null);
    } finally {
      setBanInfoLoading(false);
    }
  };

  const closeModal = () => {
    setActiveModal(null);
    setSelectedUser(null);
    setBanInfo(null);
    setBanInfoError(null);
    setBanInfoLoading(false);
  };

  const handleChangeRoleConfirm = async (form: ChangeRoleForm) => {
    if (!selectedUser) return;
    try {
      const res = await fetch(
        `${API_BASE}/api/admin/user/change-role?email=${encodeURIComponent(selectedUser.email)}&newRole=${encodeURIComponent(form.newRole)}`,
        {
          method: "PATCH",
          headers: { Accept: "application/json" },
          credentials: "include",
        },
      );
      if (!res.ok) {
        const text = await res.text();
        throw new Error(t("useradmin.error_change_role", { status: res.status, text }));
      }
      await loadUsersFromApi(pageNumber, pageSize, search, sortBy, sortDirection, onlyReported);
      closeModal();
    } catch (e: any) {
      console.error(e);
      alert(e instanceof Error ? e.message : t("useradmin.error_change_role_fallback"));
    }
  };

  const handleBanConfirm = async (form: BanForm) => {
    if (!selectedUser) return;
    try {
      const res = await fetch(`${API_BASE}/api/admin/user/lock-out`, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        credentials: "include",
        body: JSON.stringify({ email: selectedUser.email, reason: form.reason, days: form.days }),
      });
      if (!res.ok) {
        const text = await res.text();
        throw new Error(t("useradmin.error_ban", { status: res.status, text }));
      }
      await loadUsersFromApi(pageNumber, pageSize, search, sortBy, sortDirection, onlyReported);
      closeModal();
    } catch (e: any) {
      console.error(e);
      alert(e instanceof Error ? e.message : t("useradmin.error_ban_fallback"));
    }
  };

  const handleUnlockConfirm = async () => {
    if (!selectedUser) return;
    try {
      const res = await fetch(
        `${API_BASE}/api/admin/user/unlock?userEmail=${encodeURIComponent(selectedUser.email)}`,
        {
          method: "POST",
          headers: { Accept: "application/json" },
          credentials: "include",
        },
      );
      if (!res.ok) {
        const text = await res.text();
        throw new Error(t("useradmin.error_unlock", { status: res.status, text }));
      }
      await loadUsersFromApi(pageNumber, pageSize, search, sortBy, sortDirection, onlyReported);
      closeModal();
    } catch (e: any) {
      console.error(e);
      alert(e instanceof Error ? e.message : t("useradmin.error_unlock_fallback"));
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
        <button className="btn btn-sm" onClick={() => goToPage(1)} disabled={pageNumber === 1} type="button">«1</button>
        <button className="btn btn-sm" onClick={() => goToPage(pageNumber - 1)} disabled={pageNumber === 1} type="button">←</button>
        {pages.map((p) => (
          <button key={p} className={`btn btn-sm ${p === pageNumber ? "btn-active" : ""}`} onClick={() => goToPage(p)} type="button">{p}</button>
        ))}
        <button className="btn btn-sm" onClick={() => goToPage(pageNumber + 1)} disabled={pageNumber === totalPages} type="button">→</button>
        <button className="btn btn-sm" onClick={() => goToPage(totalPages)} disabled={pageNumber === totalPages} type="button">{totalPages} »</button>
      </div>
    );
  }

  if (state === "checking") return <div className="min-h-screen bg-base-200 flex items-center justify-center"><span className="loading loading-spinner loading-lg" /></div>;
  if (state !== "allowed") return null;

  return (
    <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
      <Header />
      <main className="flex-1 mx-auto w-full max-w-6xl px-4 py-10">
        <div className="flex justify-between items-center mb-4">
          <div>
            <h1 className="text-3xl font-bold">{t("useradmin.title")}</h1>
            <p className="text-sm text-base-content/70">{email ? t("useradmin.logged_in_as", { email }) : t("useradmin.checking_session")}</p>
          </div>
          <a href="/admin-dashboard" className="btn btn-outline btn-sm">{t("useradmin.back_to_admin")}</a>
        </div>

        <div className="bg-base-300 rounded-xl p-4 shadow-md mb-4 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div className="flex flex-col gap-2 md:flex-row md:items-end">
            <div className="form-control">
              <label className="label"><span className="label-text">{t("useradmin.search_label")}</span></label>
              <input className="input input-bordered input-sm w-full md:w-64" value={search} onChange={(e) => { setSearch(e.target.value); setPageNumber(1); }} placeholder={t("useradmin.search_placeholder")} />
            </div>
            <div className="form-control">
              <label className="label"><span className="label-text">{t("useradmin.sort_label")}</span></label>
              <select className="select select-bordered select-sm" value={sortBy} onChange={(e) => { setSortBy(e.target.value); setPageNumber(1); }}>
                <option value="username">{t("useradmin.sort_username")}</option>
                <option value="email">{t("useradmin.sort_email")}</option>
                <option value="roles">{t("useradmin.sort_roles")}</option>
                <option value="isBanned">{t("useradmin.sort_isBanned")}</option>
                <option value="createdAt">{t("useradmin.sort_createdAt")}</option>
              </select>
            </div>
            <div className="form-control">
              <label className="label"><span className="label-text">{t("useradmin.sort_dir_label")}</span></label>
              <select className="select select-bordered select-sm" value={sortDirection} onChange={(e) => setSortDirection(e.target.value as "asc" | "desc")}>
                <option value="asc">{t("useradmin.sort_dir_asc")}</option>
                <option value="desc">{t("useradmin.sort_dir_desc")}</option>
              </select>
            </div>

            <div className="form-control ml-2">
               <label className="cursor-pointer label flex flex-col items-start gap-1">
                 <span className="label-text text-xs">{t("useradmin.filter_only_reported")}</span>
                 <input 
                   type="checkbox" 
                   className="checkbox checkbox-sm checkbox-warning" 
                   checked={onlyReported} 
                   onChange={e => {
                     setOnlyReported(e.target.checked); 
                     setPageNumber(1);
                   }} 
                  />
               </label>
            </div>
          </div>
        </div>

        <div className="bg-base-300 rounded-xl p-4 shadow-md">
          {loading ? (
            <div className="text-sm">{t("useradmin.loading")}</div>
          ) : error ? (
            <div className="text-sm text-error">{error}</div>
          ) : users.length === 0 ? (
            <div className="text-sm">{t("useradmin.no_users")}</div>
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="table table-zebra table-sm w-full">
                  <thead>
                    <tr>
                      <th>{t("useradmin.table_hash")}</th>
                      <th>{t("useradmin.table_username")}</th>
                      <th>{t("useradmin.table_email")}</th>
                      <th>{t("useradmin.table_roles")}</th>
                      <th>{t("useradmin.table_created")}</th>
                      <th>{t("useradmin.table_status")}</th>
                      <th className="text-right">{t("useradmin.table_actions")}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {displayedUsers.map((u, idx) => (
                      <tr key={`${u.email}-${idx}`}>
                        <td>{idx + 1 + (pageNumber - 1) * pageSize}</td>
                        <td>
                          {u.userName}
                          {u.hasReport && (
                            <span className="ml-2 tooltip tooltip-right text-warning" data-tip={t("useradmin.reports_modal_title")}>
                              ⚠️
                            </span>
                          )}
                        </td>
                        <td>{u.email}</td>
                        <td>{u.roles}</td>
                        <td>{u.createdAt ? new Date(u.createdAt).toLocaleDateString() : "-"}</td>
                        <td>
                          {u.isBanned ? (
                            <span className="badge badge-error badge-sm">{t("useradmin.banned")}</span>
                          ) : (
                            <span className="badge badge-success badge-sm">{t("useradmin.active")}</span>
                          )}
                        </td>
                        <td>
                          <div className="flex justify-end gap-2 flex-wrap">
                            {u.hasReport && (
                              <button
                                className="btn btn-xs btn-warning btn-outline"
                                type="button"
                                onClick={() => openReportsModal(u)}
                                title={t("useradmin.reports_button_tooltip")}
                              >
                                !
                              </button>
                            )}

                            <button className="btn btn-xs" type="button" onClick={() => openRoleModal(u)}>
                              {t("useradmin.role_button")}
                            </button>
                            
                            {!u.isBanned && (
                              <button className="btn btn-xs btn-error" type="button" onClick={() => openBanModal(u)}>
                                {t("useradmin.ban_button")}
                              </button>
                            )}
                            
                            {u.isBanned && (
                              <>
                                <button className="btn btn-xs btn-outline" type="button" onClick={() => openReviewModal(u)}>
                                  {t("useradmin.ban_details")}
                                </button>
                                <button className="btn btn-xs btn-primary" type="button" onClick={() => openUnlockModal(u)}>
                                  {t("useradmin.unlock_button")}
                                </button>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                    {displayedUsers.length === 0 && users.length > 0 && (
                      <tr>
                        <td colSpan={7} className="text-center text-sm py-4 text-base-content/60">
                          {t("useradmin.reports_modal_no_reports") || "Brak zgłoszonych użytkowników na tej stronie."}
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
              <div className="mt-4 flex justify-between items-center text-sm">
                {renderPageButtons()}
                <div className="text-base-content/70">{t("useradmin.page_info", { page: pageNumber, total: totalPages })}</div>
              </div>
            </>
          )}
        </div>
      </main>
      <Footer />

      <ChangeRoleModal isOpen={activeModal === "role"} onClose={closeModal} user={selectedUser} onConfirm={handleChangeRoleConfirm} />
      <BanUserModal isOpen={activeModal === "ban"} onClose={closeModal} user={selectedUser} onConfirm={handleBanConfirm} />
      <ReviewBanModal isOpen={activeModal === "review"} onClose={closeModal} user={selectedUser} banInfo={banInfo} loading={banInfoLoading} error={banInfoError} />
      <UnlockUserModal isOpen={activeModal === "unlock"} onClose={closeModal} user={selectedUser} onConfirm={handleUnlockConfirm} /> 
      <UserReportsModal isOpen={activeModal === "reports"} onClose={closeModal} user={selectedUser} />
    </div>
  );
}