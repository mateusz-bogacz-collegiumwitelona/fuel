import * as React from "react";
import { API_BASE } from "./api";

type ProposalItem = {
  userName: string;
  fuelCode: string;
  proposedPrice: number;
  createdAt: string;
};

type ProposalResponse = {
  items: ProposalItem[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
};

type ViewProposalsModalProps = {
  isOpen: boolean;
  onClose: () => void;
  station: {
    brandName: string;
    street: string;
    houseNumber: string;
    city: string;
  } | null;
};

const MIN_REPORT_LENGTH = 3;

export function ViewProposalsModal({ isOpen, onClose, station }: ViewProposalsModalProps) {
  const [items, setItems] = React.useState<ProposalItem[]>([]);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [page, setPage] = React.useState(1);
  const [totalPages, setTotalPages] = React.useState(1);

  const [reportModalOpen, setReportModalOpen] = React.useState(false);
  const [userToReport, setUserToReport] = React.useState<string | null>(null);
  const [reportReason, setReportReason] = React.useState("");
  const [reportLoading, setReportLoading] = React.useState(false);
  const [reportMessage, setReportMessage] = React.useState<{ type: 'success' | 'error', text: string } | null>(null);

  React.useEffect(() => {
    if (isOpen) {
      setPage(1);
      fetchProposals(1);
    } else {
      setItems([]);
      closeReportModal();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, station]);

  const fetchProposals = async (pageNumber: number) => {
    if (!station) return;
    
    setLoading(true);
    setError(null);

    try {
      const params = new URLSearchParams({
        BrandName: station.brandName,
        Street: station.street,
        HouseNumber: station.houseNumber,
        City: station.city,
        PageNumber: String(pageNumber),
        PageSize: "10",
      });

      const res = await fetch(`${API_BASE}/api/station/price-proposal?${params.toString()}`, {
        method: "GET",
        headers: { Accept: "application/json" },
        credentials: "include",
      });

      if (!res.ok) throw new Error(`Error: ${res.status}`);

      const json: any = await res.json();
      
      let loadedItems: ProposalItem[] = [];
      let loadedTotalPages = 1;
      let loadedPage = 1;

      if (json && Array.isArray(json.items)) {
          loadedItems = json.items;
          loadedTotalPages = json.totalPages;
          loadedPage = json.pageNumber;
      } 
      else if (json.data && Array.isArray(json.data.items)) {
          loadedItems = json.data.items;
          loadedTotalPages = json.data.totalPages;
          loadedPage = json.data.pageNumber;
      }

      setItems(loadedItems);
      setTotalPages(loadedTotalPages || 1);
      setPage(loadedPage || 1);

    } catch (err) {
      console.error(err);
      setError("Nie udało się pobrać propozycji.");
    } finally {
      setLoading(false);
    }
  };

  const handlePageChange = (newPage: number) => {
    if (newPage >= 1 && newPage <= totalPages) {
      fetchProposals(newPage);
    }
  };

  const openReportModal = (userName: string) => {
    setUserToReport(userName);
    setReportReason("");
    setReportMessage(null);
    setReportModalOpen(true);
  };

  const closeReportModal = () => {
    setReportModalOpen(false);
    setUserToReport(null);
    setReportReason("");
    setReportMessage(null);
  };

  const handleSendReport = async () => {
    if (!userToReport) return;
    
    if (reportReason.length < MIN_REPORT_LENGTH) {
      setReportMessage({ type: 'error', text: `Powód zgłoszenia musi mieć co najmniej ${MIN_REPORT_LENGTH} znaków.` });
      return;
    }

    setReportLoading(true);
    setReportMessage(null);

    try {
      const res = await fetch(`${API_BASE}/api/user/report`, {
        method: "POST",
        headers: { 
          "Content-Type": "application/json",
          Accept: "application/json" 
        },
        credentials: "include",
        body: JSON.stringify({
          reportedUserName: userToReport,
          reason: reportReason
        })
      });

      const json = await res.json().catch(() => ({}));

      if (!res.ok || json.success === false) {
        // Jeśli backend nadal wymusza 50 znaków, błąd pojawi się tutaj w 'message' lub 'errors'
        const msg = json.message || (json.errors ? JSON.stringify(json.errors) : "Wystąpił błąd podczas wysyłania zgłoszenia.");
        throw new Error(msg);
      }

      setReportMessage({ type: 'success', text: "Użytkownik został zgłoszony pomyślnie." });
      
      setTimeout(() => {
        closeReportModal();
      }, 1500);

    } catch (err: any) {
      console.error(err);
      setReportMessage({ type: 'error', text: err.message || "Błąd połączenia." });
    } finally {
      setReportLoading(false);
    }
  };

  const formatDate = (dateStr: string) => {
    if (!dateStr) return "-";
    return new Date(dateStr).toLocaleString("pl-PL");
  };

  if (!isOpen) return null;

  return (
    <div className="modal modal-open" style={{ zIndex: 999 }}>
      <div className="modal-box w-11/12 max-w-3xl relative">
        <div className="flex justify-between items-center mb-4">
          <h3 className="font-bold text-lg">
            Zgłoszone propozycje cen (oczekujące)
          </h3>
          <button className="btn btn-sm btn-ghost" onClick={onClose}>✕</button>
        </div>

        {error ? (
           <div className="alert alert-error text-sm">{error}</div>
        ) : loading ? (
          <div className="flex justify-center py-8">
            <span className="loading loading-spinner loading-lg"></span>
          </div>
        ) : items.length === 0 ? (
          <div className="py-8 text-center text-base-content/60">
            Brak zgłoszonych propozycji dla tej stacji.
          </div>
        ) : (
          <>
            <div className="overflow-x-auto min-h-[300px]">
              <table className="table table-zebra w-full text-sm">
                <thead>
                  <tr>
                    <th>Data zgłoszenia</th>
                    <th>Użytkownik</th>
                    <th>Paliwo</th>
                    <th className="text-right">Cena</th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((item, idx) => (
                    <tr key={idx}>
                      <td className="text-xs text-base-content/70">{formatDate(item.createdAt)}</td>
                      <td className="font-medium flex items-center gap-2">
                        {item.userName}
                        <button 
                          className="btn btn-xs btn-circle btn-ghost text-error tooltip tooltip-right" 
                          data-tip="Zgłoś użytkownika"
                          onClick={() => openReportModal(item.userName)}
                        >
                          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className="w-4 h-4">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
                          </svg>
                        </button>
                      </td>
                      <td><span className="badge badge-ghost badge-sm">{item.fuelCode}</span></td>
                      <td className="text-right font-bold text-base">
                        {item.proposedPrice.toFixed(2)} zł
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {totalPages > 1 && (
              <div className="flex justify-center gap-2 mt-4">
                <button 
                  className="btn btn-sm" 
                  disabled={page === 1} 
                  onClick={() => handlePageChange(page - 1)}
                >
                  «
                </button>
                <button className="btn btn-sm btn-disabled">
                  Strona {page} z {totalPages}
                </button>
                <button 
                  className="btn btn-sm" 
                  disabled={page === totalPages} 
                  onClick={() => handlePageChange(page + 1)}
                >
                  »
                </button>
              </div>
            )}
          </>
        )}

        <div className="modal-action">
          <button className="btn" onClick={onClose}>Zamknij</button>
        </div>

        {reportModalOpen && (
          <div className="absolute inset-0 bg-base-100/90 flex items-center justify-center z-50 rounded-2xl p-4">
            <div className="bg-base-200 p-6 rounded-xl shadow-xl w-full max-w-md border border-base-300">
              <h4 className="text-lg font-bold mb-2 text-error">Zgłoś użytkownika: {userToReport}</h4>
              <p className="text-sm text-base-content/70 mb-4">
                Opisz dlaczego zgłaszasz tego użytkownika.
              </p>
              
              <div className="form-control">
                <textarea 
                  className="textarea textarea-bordered h-24 w-full" 
                  placeholder="Powód zgłoszenia (np. spam, wulgaryzmy)..."
                  value={reportReason}
                  onChange={(e) => setReportReason(e.target.value)}
                ></textarea>
                <label className="label">
                  <span className={`label-text-alt ${reportReason.length < MIN_REPORT_LENGTH ? 'text-error' : 'text-success'}`}>
                    {reportReason.length} / 1000 (min. {MIN_REPORT_LENGTH})
                  </span>
                </label>
              </div>

              {reportMessage && (
                <div className={`alert ${reportMessage.type === 'error' ? 'alert-error' : 'alert-success'} text-sm mt-2 py-2`}>
                  {reportMessage.text}
                </div>
              )}

              <div className="flex justify-end gap-2 mt-4">
                <button 
                  className="btn btn-sm btn-ghost" 
                  onClick={closeReportModal}
                  disabled={reportLoading}
                >
                  Anuluj
                </button>
                <button 
                  className="btn btn-sm btn-error"
                  onClick={handleSendReport}
                  disabled={reportLoading || reportReason.length < MIN_REPORT_LENGTH}
                >
                  {reportLoading ? <span className="loading loading-spinner loading-xs"></span> : "Wyślij zgłoszenie"}
                </button>
              </div>
            </div>
          </div>
        )}

      </div>
      <label className="modal-backdrop" onClick={onClose}>Zamknij</label>
    </div>
  );
}