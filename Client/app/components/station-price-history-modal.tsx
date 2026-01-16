import * as React from "react";
import { useTranslation } from "react-i18next";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
import { API_BASE } from "./api";

type FuelHistoryItem = {
  fuelType: string;
  fuelCode: string;
  validFrom: (string | null)[];
  validTo: (string | null)[];
  price: number[];
};

type StationPriceHistoryModalProps = {
  isOpen: boolean;
  onClose: () => void;
  station: {
    brandName: string;
    street: string;
    houseNumber: string;
    city: string;
  } | null;
};

export function StationPriceHistoryModal({ isOpen, onClose, station }: StationPriceHistoryModalProps) {
  const { t, i18n } = useTranslation();
  
  const [historyData, setHistoryData] = React.useState<FuelHistoryItem[]>([]);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  const [activeTabCode, setActiveTabCode] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (isOpen && station) {
      fetchHistory();
    } else {
      setHistoryData([]);
      setActiveTabCode(null);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, station]);

  const fetchHistory = async () => {
    if (!station) return;
    setLoading(true);
    setError(null);

    try {
      const params = new URLSearchParams({
        BrandName: station.brandName,
        Street: station.street,
        HouseNumber: station.houseNumber,
        City: station.city,
      });

      const res = await fetch(`${API_BASE}/api/station/fuel-price/history?${params.toString()}`, {
        method: "GET",
        headers: { Accept: "application/json" },
        credentials: "include",
      });

      if (!res.ok) throw new Error(`Error: ${res.status}`);

      const json: any = await res.json();
      
      let items: FuelHistoryItem[] = [];

      if (Array.isArray(json)) {
        items = json;
      } else if (json.data && Array.isArray(json.data)) {
        items = json.data;
      }

      if (items.length > 0) {
        setHistoryData(items);
        setActiveTabCode(items[0].fuelCode);
      } else {
        setHistoryData([]);
      }

    } catch (err) {
      console.error(err);
      setError(t("station.history_error"));
    } finally {
      setLoading(false);
    }
  };

  const getChartData = () => {
    if (!activeTabCode) return [];
    
    const fuelData = historyData.find(f => f.fuelCode === activeTabCode);
    if (!fuelData) return [];

    const chartPoints = fuelData.validFrom.map((dateStr, index) => {
      if (!dateStr) return null;
      const dateObj = new Date(dateStr);
      
      if (isNaN(dateObj.getTime())) return null;

      return {
        dateOriginal: dateStr,
        dateLabel: dateObj.toLocaleDateString(i18n.language, { day: '2-digit', month: '2-digit', year: '2-digit' }),
        fullDate: dateObj.toLocaleString(i18n.language),
        price: fuelData.price[index]
      };
    }).filter(item => item !== null);

    return chartPoints.sort((a, b) => new Date(a!.dateOriginal).getTime() - new Date(b!.dateOriginal).getTime());
  };

  const currentChartData = getChartData();
  const currentFuelName = historyData.find(f => f.fuelCode === activeTabCode)?.fuelType || activeTabCode;

  const prices = currentChartData.map(d => d!.price);
  const minPrice = prices.length ? Math.min(...prices) - 0.20 : 0;
  const maxPrice = prices.length ? Math.max(...prices) + 0.20 : 10;

  if (!isOpen) return null;

  return (
    <div className="modal modal-open" style={{ zIndex: 1000 }}>
      <div className="modal-box w-11/12 max-w-4xl relative">
        <button className="btn btn-sm btn-circle btn-ghost absolute right-2 top-2" onClick={onClose}>✕</button>
        
        <h3 className="font-bold text-lg mb-4">{t("station.history_title")}</h3>
        
        {station && (
          <p className="text-sm text-base-content/70 mb-6">
             {station.brandName} – {station.city}, {station.street} {station.houseNumber}
          </p>
        )}

        {loading ? (
           <div className="flex justify-center py-12">
             <span className="loading loading-spinner loading-lg"></span>
             <span className="ml-3">{t("station.history_loading")}</span>
           </div>
        ) : error ? (
           <div className="alert alert-error">{error}</div>
        ) : historyData.length === 0 ? (
           <div className="alert alert-info">{t("station.history_no_data")}</div>
        ) : (
          <div>

            <div className="tabs tabs-boxed mb-6 bg-base-200 p-1 gap-1 flex-wrap">
              {historyData.map((fuel) => (
                <a 
                  key={fuel.fuelCode}
                  className={`tab transition-all duration-200 ${activeTabCode === fuel.fuelCode ? 'tab-active font-bold' : ''}`}
                  onClick={() => setActiveTabCode(fuel.fuelCode)}
                >
                  {fuel.fuelCode}
                </a>
              ))}
            </div>


            <div className="h-[400px] w-full mt-4">
              <h4 className="text-center font-semibold mb-2 text-primary">{currentFuelName}</h4>
              
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={currentChartData as any[]} margin={{ top: 20, right: 30, left: 0, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" opacity={0.3} />
                  <XAxis 
                    dataKey="dateLabel" 
                    tick={{ fontSize: 12 }}
                    interval="preserveStartEnd"
                  />
                  <YAxis 
                    domain={[minPrice, maxPrice]} 
                    unit=" zł" 
                    tick={{ fontSize: 12 }}
                    width={50}
                  />
                  <Tooltip 
                    contentStyle={{ backgroundColor: "#1f2937", borderColor: "#374151", color: "#fff" }}
                    itemStyle={{ color: "#4ade80" }}
                    labelStyle={{ color: "#9ca3af", marginBottom: '0.5rem' }}
                    labelFormatter={(label, payload) => {
                        if (payload && payload.length > 0) {
                            return payload[0].payload.fullDate;
                        }
                        return label;
                    }}
                    formatter={(value: number) => [`${value.toFixed(2)} zł`, t("station.history_chart_price")]}
                  />
                  <Line 
                    type="stepAfter" 
                    dataKey="price" 
                    stroke="#00a96e" 
                    strokeWidth={3}
                    dot={{ r: 4, fill: "#00a96e" }}
                    activeDot={{ r: 7 }}
                    animationDuration={1000}
                  />
                </LineChart>
              </ResponsiveContainer>
              
              {currentChartData.length > 0 && (
                <p className="text-center text-xs text-base-content/50 mt-2">
                  {t("station.history_note")}
                </p>
              )}
            </div>
          </div>
        )}

        <div className="modal-action">
          <button className="btn" onClick={onClose}>{t("common.close")}</button>
        </div>
      </div>
      <label className="modal-backdrop" onClick={onClose}>{t("common.close")}</label>
    </div>
  );
}