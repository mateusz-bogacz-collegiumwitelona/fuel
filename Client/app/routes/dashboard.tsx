import React from "react";
import { Link } from "react-router";
import Header from "../components/header";
import Footer from "../components/footer";
import { useTranslation } from "react-i18next";
import { API_BASE } from "../components/api";
import { useUserGuard } from "../components/useUserGuard";

type RequestItem = {
  id: string;
  createdAt: string;
  title: string;
  status: "pending" | "accepted" | "rejected" | string;
};

type Station = {
  id?: string;
  brandName: string;
  street: string;
  houseNumber: number;
  postalCode: string;
  latitude?: number;
  longitude?: number;
  city?: string;
  imageUrl?: string;
  address?: string;
  distanceMeters?: number;
  fuelPrices?: Record<string, number | string>;
};

export default function Dashboard(): JSX.Element {
  const { state, email } = useUserGuard();
  const { t } = useTranslation();
  React.useEffect(() => {
    document.title = t("footer.links.dashboard") + " - FuelStats";
  }, [t]);
  const [requests, setRequests] = React.useState<RequestItem[] | null>(null);
  const [requestsLoading, setRequestsLoading] = React.useState(true);

  const [stations, setStations] = React.useState<Station[] | null>(null);
  const [stationsLoading, setStationsLoading] = React.useState(true);
  const [stationsError, setStationsError] = React.useState<string | null>(null);

  const [stats, setStats] = React.useState<any>(null);
  const [statsLoading, setStatsLoading] = React.useState(true);
  const [statsError, setStatsError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (state !== "allowed") return;

    (async () => {
      await Promise.all([
        fetchRequests(),
        fetchProposalStats(),
      ]);
    })();
  }, [state]);

  async function fetchRequests() {
    setRequestsLoading(true);
    try {
      const headers: Record<string, string> = { Accept: "application/json" };

      const res = await fetch(`${API_BASE}/api/user/requests`, {
        headers,
        credentials: "include",
      });
      if (!res.ok) throw new Error("fetch-error");
      const data = await res.json();
      setRequests(data);
    } catch (err) {
      console.warn(
        "Nie udało się pobrać zgłoszeń — używam danych przykładowych.",
        err,
      );
      setRequests([
        {
          id: "1",
          createdAt: new Date(
            Date.now() - 1000 * 60 * 60 * 24 * 2,
          ).toISOString(),
          title: "Propozycja: korekta ceny na stacji X",
          status: "pending",
        },
        {
          id: "2",
          createdAt: new Date(
            Date.now() - 1000 * 60 * 60 * 24 * 10,
          ).toISOString(),
          title: "Propozycja: dodanie nowej stacji Y",
          status: "accepted",
        },
      ]);
    } finally {
      setRequestsLoading(false);
    }
  }

  async function fetchProposalStats() {
    setStatsLoading(true);
    setStatsError(null);

    const tryEndpoints = [
      "/api/proposals/statistics",
      "/api/proposal-statistic",
      "/api/proposal-statistics",
      "/api/proposals/statistic",
    ];

    for (const ep of tryEndpoints) {
      try {
        const headers: Record<string, string> = {
          Accept: "application/json",
        };

        const res = await fetch(`${API_BASE}${ep}`, {
          headers,
          credentials: "include",
        });

        if (!res.ok) {
          if (res.status === 404) continue;
          throw new Error(`stats-fetch-error ${res.status}`);
        }

        const data = await res.json();

        if (data && typeof data === "object" && !Array.isArray(data)) {
          const normalized = {
            total:
              data.total ??
              data.totalProposals ??
              data.total_proposals ??
              data.count ??
              data.itemsCount ??
              0,
            accepted:
              data.approved ??
              data.approvedProposals ??
              data.approved_proposals ??
              data.accepted ??
              data.acceptedProposals ??
              0,
            rejected:
              data.rejected ??
              data.rejectedProposals ??
              data.rejected_proposals ??
              data.denied ??
              0,
            acceptedRate:
              data.acceptedRate ??
              data.acceptanceRate ??
              data.accepted_rate ??
              data.approvedRate ??
              data.acceptedRate ??
              null,
          };
          setStats(normalized);
          setStatsLoading(false);
          return;
        }

        if (Array.isArray(data)) {
          const total = data.length;
          const accepted = data.filter(
            (x: any) => x.status === "accepted",
          ).length;
          const rejected = data.filter(
            (x: any) => x.status === "rejected",
          ).length;
          const acceptedRate =
            total > 0 ? Math.round((accepted / total) * 100) : null;
          setStats({ total, accepted, rejected, acceptedRate });
          setStatsLoading(false);
          return;
        }

        console.warn("Nieoczekiwany format statystyk:", data);
        setStats(null);
        setStatsLoading(false);
        return;
      } catch (err) {
        console.warn(`Błąd pobierania statystyk z ${ep}:`, err);
      }
    }

    setStatsError(t("dashboard.statsFetchFailed") || "Nie udało się pobrać statystyk z serwera.");
    setStatsLoading(false);
  }

  const handleLogout = () => {
    try {
      if (typeof localStorage !== "undefined") {
        localStorage.removeItem("token");
        localStorage.removeItem("token_expiration");
      }
      if (typeof window !== "undefined") window.location.href = "/login";
    } catch (err) {
      console.error("Logout error", err);
    }
  };

  const formatDate = (iso?: string) => (iso ? new Date(iso).toLocaleString() : "-");

  const formatDistance = (m?: string | number) => {
    if (m == null) return "-";
    const mm = Number(m);
    if (Number.isNaN(mm)) return "-";
    if (mm >= 1000) return `${(mm / 1000).toFixed(2)} km`;
    return `${Math.round(mm)} m`;
  };

  return (
    <div className="min-h-screen bg-base-200 text-base-content">
      <Header />

      <main className="mx-auto max-w-350 px-1 py-8">
        {email && (
          <h1 className="text-2xl md:text-3xl font-bold mb-4">
            {t("dashboard.welcome")} {email}
          </h1>
        )}

        <section className="mb-8">
          <div className="carousel w-full">
            <div id="slide1" className="carousel-item relative w-full">
              <img src="/images/stacjaOrlen.png" className="w-full" alt="stacja Orlen" />
              <div className="absolute left-5 right-5 top-1/2 flex -translate-y-1/2 transform justify-between">
                <a href="#slide4" className="btn btn-circle">
                  ❮
                </a>
                <a href="#slide2" className="btn btn-circle">
                  ❯
                </a>
              </div>
            </div>
            <div id="slide2" className="carousel-item relative w-full">
              <img src="/images/stacjaMoya.png" className="w-full" alt="stacja Moya" />
              <div className="absolute left-5 right-5 top-1/2 flex -translate-y-1/2 transform justify-between">
                <a href="#slide1" className="btn btn-circle">
                  ❮
                </a>
                <a href="#slide3" className="btn btn-circle">
                  ❯
                </a>
              </div>
            </div>
            <div id="slide3" className="carousel-item relative w-full">
              <img src="/images/stacjaBp.png" className="w-full" alt="stacja BP" />
              <div className="absolute left-5 right-5 top-1/2 flex -translate-y-1/2 transform justify-between">
                <a href="#slide2" className="btn btn-circle">
                  ❮
                </a>
                <a href="#slide4" className="btn btn-circle">
                  ❯
                </a>
              </div>
            </div>
          </div>
        </section>

        <section className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
          <a href="/map" className="block rounded-xl overflow-hidden shadow-lg">
            <div className="relative h-56 md:h-72 bg-gray-700 flex items-center justify-center">
              <img src="/images/map-preview.png" alt="map" className="object-cover w-full h-full" />
              <div className="absolute inset-0 bg-black/30"></div>
              <div className="absolute z-10 text-2xl font-bold">{t("dashboard.map")}</div>
            </div>
          </a>

          <a href="/list" className="block rounded-xl overflow-hidden shadow-lg">
            <div className="relative h-56 md:h-72 bg-gray-700 flex items-center justify-center">
              <img src="/images/list-preview.png" alt="list" className="object-cover w-full h-full" />
              <div className="absolute inset-0 bg-black/30"></div>
              <div className="absolute z-10 text-2xl font-bold">{t("dashboard.list")}</div>
            </div>
          </a>
        </section>

        <section className="bg-base-300 p-6 rounded-xl shadow-md">
          <h2 className="text-xl font-semibold mb-4">{t("dashboard.yourstatistics")}</h2>

          {statsLoading ? (
            <div>{t("dashboard.loadstatistics")}</div>
          ) : statsError ? (
            <div className="text-error">{statsError}</div>
          ) : stats ? (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="p-4 bg-base-100 rounded text-center">
                <div className="text-3xl text-base-content font-bold">{stats.total ?? requests?.length ?? 0}</div>
                <div className="text-sm text-base-content mt-1">{t("dashboard.allreports")}</div>
              </div>
              <div className="p-4 bg-base-100 rounded text-center">
                <div className="text-3xl text-success font-bold">{stats.accepted ?? 0}</div>
                <div className="text-sm text-success mt-1">{t("dashboard.accepted")}</div>
              </div>
              <div className="p-4 bg-base-100 rounded text-error text-center">
                <div className="text-3xl font-bold">{stats.rejected ?? 0}</div>
                <div className="text-sm mt-1">{t("dashboard.rejected")}</div>
              </div>
              <div className="p-4 bg-base-100 rounded text-info text-center">
                <div className="text-3xl font-bold">{stats.acceptedRate != null ? `${stats.acceptedRate}%` : "-"}</div>
                <div className="text-sm mt-1">{t("dashboard.acceptrate")}</div>
              </div>
            </div>
          ) : (
            <div className="text-gray-300">{t("dashboard.nostatistics")}</div>
          )}
        </section>
      </main>
      <Footer />
    </div>
  );
}
