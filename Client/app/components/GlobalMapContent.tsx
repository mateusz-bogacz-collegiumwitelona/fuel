import { useEffect, useRef } from "react";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

import icon from 'leaflet/dist/images/marker-icon.png';
import iconShadow from 'leaflet/dist/images/marker-shadow.png';

const DefaultIcon = L.icon({
    iconUrl: icon,
    shadowUrl: iconShadow,
    iconSize: [25, 41],
    iconAnchor: [12, 41],
    popupAnchor: [1, -34],
});
L.Marker.prototype.options.icon = DefaultIcon;

type Station = {
  brandName: string;
  street: string;
  houseNumber: string;
  city: string;
  postcode: string;
  latitude: number;
  longitude: number;
};

type GlobalMapContentProps = {
  stations: Station[];
  searchLabel: string; 
};

const brandColors: Record<string, string> = {
  Default: "black",
  Orlen: "red",
  BP: "green",
  Shell: "yellow",
  "Circle K": "orange",
  Moya: "blue",
  Lotos: "gold",
};

export default function GlobalMapContent({ stations, searchLabel }: GlobalMapContentProps) {
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const markersLayerRef = useRef<L.LayerGroup | null>(null);

  function getMarkerIcon(brand: string) {
    const normalizedBrand = Object.keys(brandColors).find(
      (key) => key.toLowerCase() === (brand ?? "").toLowerCase(),
    );
    const color = normalizedBrand ? brandColors[normalizedBrand] : brandColors.Default;
    
    return new L.Icon({
      iconUrl: `https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-${color}.png`,
      shadowUrl: "https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-shadow.png",
      iconSize: [25, 41],
      iconAnchor: [12, 41],
      popupAnchor: [1, -34],
      shadowSize: [41, 41],
    });
  }

  useEffect(() => {
    if (!mapContainerRef.current || mapInstanceRef.current) return;

    const map = L.map(mapContainerRef.current).setView([52.2297, 21.0122], 6);

    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    const markersLayer = L.layerGroup().addTo(map);
    
    mapInstanceRef.current = map;
    markersLayerRef.current = markersLayer;

    return () => {
      map.remove();
      mapInstanceRef.current = null;
    };
  }, []);
  useEffect(() => {
    const map = mapInstanceRef.current;
    const markersLayer = markersLayerRef.current;

    if (!map || !markersLayer) return;

    markersLayer.clearLayers();

    stations.forEach((s) => {
      const detailsUrl = `/station/${encodeURIComponent(s.brandName)}/${encodeURIComponent(s.city)}/${encodeURIComponent(s.street)}/${encodeURIComponent(s.houseNumber)}`;
      
      const marker = L.marker([s.latitude, s.longitude], {
        icon: getMarkerIcon(s.brandName)
      });

      const popupContent = `
        <div style="font-family: sans-serif; min-width: 150px;">
            <div style="margin-bottom: 8px;">
                <strong>${s.brandName}</strong><br/>
                <span style="font-size: 0.9em; color: #555;">
                    ${s.city}, ${s.street} ${s.houseNumber}
                </span>
            </div>
            <a href="${detailsUrl}" 
               style="display: inline-block; padding: 4px 12px; background-color: #fff; border: 1px solid #secondary; color: #333; text-decoration: none; border-radius: 4px; font-size: 0.85em; width: 100%; text-align: center; box-sizing: border-box;">
               ${searchLabel}
            </a>
        </div>
      `;

      marker.bindPopup(popupContent);
      markersLayer.addLayer(marker);
    });

  }, [stations, searchLabel]); // Uruchom ponownie, gdy zmienią się stacje

  return <div ref={mapContainerRef} style={{ height: "100%", width: "100%", zIndex: 0 }} />;
}