import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";

import {
  ChangeRoleModal,
  BanUserModal,
  ReviewBanModal,
  UnlockUserModal,
} from "../components/user-admin-modals";

import type {
  AdminUser,
  ChangeRoleForm,
  BanForm,
  BanInfo,
} from "../components/user-admin-modals";

import { useAdminGuard } from "../components/useAdminGuard";
import { useTranslation } from "react-i18next";

const API_BASE = "http://localhost:5111";

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
  const [sortDirection, setSortDirection] = React.useState<"asc" | "desc">(
    "asc",
  );

  const [activeModal, setActiveModal] = React.useState<
    "role" | "ban" | "review" | "unlock" | null
  >(null);
  const [selectedUser, setSelectedUser] = React.useState<AdminUser | null>(
    null,
  );

  const [banInfo, setBanInfo] = React.useState<BanInfo | null>(null);
  const [banInfoLoading, setBanInfoLoading] = React.useState(false);
  const [banInfoError, setBanInfoError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (state !== "allowed") return;
    (async () => {
      await loadUsersFromApi(
        pageNumber,
        pageSize,
        search,
        sortBy,
        sortDirection,
      );
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [state, pageNumber, pageSize, search, sortBy, sortDirection]);

  async function loadUsersFromApi(
    page: number,
    size: number,
    searchValue: string,
    sort: string,
    direction: "asc" | "desc",
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

      const res = await fetch(
        `${API_BASE}/api/admin/user/list?${params.toString()}`,
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

  const openReviewModal = async (user: AdminUser) => {
    setSelectedUser(user);
    setActiveModal("review");
    setBanInfo(null);
    setBanInfoError(null);
    setBanInfoLoading(true);

    try {
      const res = await fetch(
        `${API_BASE}/api/admin/user/lock-out/review?email=${encodeURIComponent(
          user.email,
        )}`,
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
        setBanInfoError(
          t("useradmin.error_fetch_baninfo", { status: res.status, text }),
        );
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
        `${API_BASE}/api/admin/user/change-role?email=${encodeURIComponent(
          selectedUser.email,
        )}&newRole=${encodeURIComponent(form.newRole)}`,
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
        throw new Error(
          t("useradmin.error_change_role", { status: res.status, text }),
        );
      }

      await loadUsersFromApi(
        pageNumber,
        pageSize,
        search,
        sortBy,
        sortDirection,
      );
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
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        credentials: "include",
        body: JSON.stringify({
          email: selectedUser.email,
          reason: form.reason,
          days: form.days,
        }),
      });

      if (!res.ok) {
        const text = await res.text();
        throw new Error(t("useradmin.error_ban", { status: res.status, text }));
      }

      await loadUsersFromApi(
        pageNumber,
        pageSize,
        search,
        sortBy,
        sortDirection,
      );
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
        `${API_BASE}/api/admin/user/unlock?userEmail=${encodeURIComponent(
          selectedUser.email,
        )}`,
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

      await loadUsersFromApi(
        pageNumber,
        pageSize,
        search,
        sortBy,
        sortDirection,
      );
      closeModal();
    } catch (e: any) {
      console.error(e);
      alert(e instanceof Error ? e.message : t("useradmin.error_unlock_fallback"));
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
            <h1 className="text-3xl font-bold">{t("useradmin.title")}</h1>
            <p className="text-sm text-base-content/70">
              {email ? t("useradmin.logged_in_as", { email }) : t("useradmin.checking_session")}
            </p>
          </div>
          <a href="/admin-dashboard" className="btn btn-outline btn-sm">
            {t("useradmin.back_to_admin")}
          </a>
        </div>

        {/* FILTRY / SZUKAJKA */}
        <div className="bg-base-300 rounded-xl p-4 shadow-md mb-4 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div className="flex flex-col gap-2 md:flex-row md:items-end">
            <div className="form-control">
              <label className="label">
                <span className="label-text">{t("useradmin.search_label")}</span>
              </label>
              <input
                className="input input-bordered input-sm w-full md:w-64"
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value);
                  setPageNumber(1);
                }}
                placeholder={t("useradmin.search_placeholder")}
              />
            </div>

            <div className="form-control">
              <label className="label">
                <span className="label-text">{t("useradmin.sort_label")}</span>
              </label>
              <select
                className="select select-bordered select-sm"
                value={sortBy}
                onChange={(e) => {
                  setSortBy(e.target.value);
                  setPageNumber(1);
                }}
              >
                <option value="username">{t("useradmin.sort_username")}</option>
                <option value="email">{t("useradmin.sort_email")}</option>
                <option value="roles">{t("useradmin.sort_roles")}</option>
                <option value="isBanned">{t("useradmin.sort_isBanned")}</option>
                <option value="createdAt">{t("useradmin.sort_createdAt")}</option>
              </select>
            </div>

            <div className="form-control">
              <label className="label">
                <span className="label-text">{t("useradmin.sort_dir_label")}</span>
              </label>
              <select
                className="select select-bordered select-sm"
                value={sortDirection}
                onChange={(e) =>
                  setSortDirection(e.target.value as "asc" | "desc")
                }
              >
                <option value="asc">{t("useradmin.sort_dir_asc")}</option>
                <option value="desc">{t("useradmin.sort_dir_desc")}</option>
              </select>
            </div>
          </div>
        </div>

        {/* TABELA UŻYTKOWNIKÓW */}
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
                    {users.map((u, idx) => (
                      <tr key={`${u.email}-${idx}`}>
                        <td>{idx + 1 + (pageNumber - 1) * pageSize}</td>
                        <td>{u.userName}</td>
                        <td>{u.email}</td>
                        <td>{u.roles}</td>
                        <td>
                          {u.createdAt
                            ? new Date(u.createdAt).toLocaleDateString()
                            : "-"}
                        </td>
                        <td>
                          {u.isBanned ? (
                            <span className="badge badge-error badge-sm">
                              {t("useradmin.banned")}
                            </span>
                          ) : (
                            <span className="badge badge-success badge-sm">
                              {t("useradmin.active")}
                            </span>
                          )}
                        </td>
                        <td>
                          <div className="flex justify-end gap-2 flex-wrap">
                            <button
                              className="btn btn-xs"
                              type="button"
                              onClick={() => openRoleModal(u)}
                            >
                              {t("useradmin.role_button")}
                            </button>
                            {!u.isBanned && (
                              <button
                                className="btn btn-xs btn-error"
                                type="button"
                                onClick={() => openBanModal(u)}
                              >
                                {t("useradmin.ban_button")}
                              </button>
                            )}
                            {u.isBanned && (
                              <>
                                <button
                                  className="btn btn-xs btn-outline"
                                  type="button"
                                  onClick={() => openReviewModal(u)}
                                >
                                  {t("useradmin.ban_details")}
                                </button>
                                <button
                                  className="btn btn-xs btn-primary"
                                  type="button"
                                  onClick={() => openUnlockModal(u)}
                                >
                                  {t("useradmin.unlock_button")}
                                </button>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="flex justify-end items-center gap-3 mt-3 text-sm">
                <span>
                  {t("useradmin.page_info", { page: pageNumber, total: totalPages })}
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
      <ChangeRoleModal
        isOpen={activeModal === "role"}
        onClose={closeModal}
        user={selectedUser}
        onConfirm={handleChangeRoleConfirm}
      />

      <BanUserModal
        isOpen={activeModal === "ban"}
        onClose={closeModal}
        user={selectedUser}
        onConfirm={handleBanConfirm}
      />

      <ReviewBanModal
        isOpen={activeModal === "review"}
        onClose={closeModal}
        user={selectedUser}
        banInfo={banInfo}
        loading={banInfoLoading}
        error={banInfoError}
      />

      <UnlockUserModal
        isOpen={activeModal === "unlock"}
        onClose={closeModal}
        user={selectedUser}
        onConfirm={handleUnlockConfirm}
      />
    </div>
  );
}