import { useEffect, useRef } from "react";
import type L from "leaflet";
import "leaflet/dist/leaflet.css";
import icon from 'leaflet/dist/images/marker-icon.png';
import iconShadow from 'leaflet/dist/images/marker-shadow.png';

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
  initialZoom?: number;
  enableDetailsLink?: boolean; 
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

export default function GlobalMapContent({ 
  stations, 
  searchLabel, 
  initialZoom,
  enableDetailsLink = true
}: GlobalMapContentProps) {
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const markersLayerRef = useRef<L.LayerGroup | null>(null);

  useEffect(() => {
    if (!mapContainerRef.current) return;
    if (mapInstanceRef.current) return;

    let isMounted = true;

    (async () => {
        const L = (await import("leaflet")).default;

        if (!isMounted || mapInstanceRef.current) return;

        const DefaultIcon = L.icon({
            iconUrl: icon,
            shadowUrl: iconShadow,
            iconSize: [25, 41],
            iconAnchor: [12, 41],
            popupAnchor: [1, -34],
        });
        L.Marker.prototype.options.icon = DefaultIcon;

        const map = L.map(mapContainerRef.current).setView([52.2297, 21.0122], initialZoom || 6);

        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            attribution: '© OpenStreetMap contributors'
        }).addTo(map);

        const markersLayer = L.layerGroup().addTo(map);
        
        mapInstanceRef.current = map;
        markersLayerRef.current = markersLayer;

        updateMarkers(L, map, markersLayer, stations, searchLabel, initialZoom, enableDetailsLink);
    })();

    return () => {
      isMounted = false;
      if (mapInstanceRef.current) {
        mapInstanceRef.current.remove();
        mapInstanceRef.current = null;
        markersLayerRef.current = null;
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); 


  useEffect(() => {
    if (!mapInstanceRef.current || !markersLayerRef.current) return;
    
    import("leaflet").then(module => {
        const L = module.default;
        updateMarkers(L, mapInstanceRef.current!, markersLayerRef.current!, stations, searchLabel, initialZoom, enableDetailsLink);
    });

  }, [stations, searchLabel, initialZoom, enableDetailsLink]);


  function updateMarkers(L: any, map: L.Map, layer: L.LayerGroup, stations: Station[], label: string, zoom?: number, showLink?: boolean) {
    layer.clearLayers();

    if (stations.length === 1) {
        map.setView([stations[0].latitude, stations[0].longitude], zoom || 16);
    } 

    stations.forEach((s) => {
      if(!s.latitude || !s.longitude) return;

      const detailsUrl = `/station/${encodeURIComponent(s.brandName)}/${encodeURIComponent(s.city)}/${encodeURIComponent(s.street)}/${encodeURIComponent(s.houseNumber)}`;
      
      const normalizedBrand = Object.keys(brandColors).find(
        (key) => key.toLowerCase() === (s.brandName ?? "").toLowerCase(),
      );
      const color = normalizedBrand ? brandColors[normalizedBrand] : brandColors.Default;

      const customIcon = new L.Icon({
        iconUrl: `https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-${color}.png`,
        shadowUrl: "https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-shadow.png",
        iconSize: [25, 41],
        iconAnchor: [12, 41],
        popupAnchor: [1, -34],
        shadowSize: [41, 41],
      });

      const marker = L.marker([s.latitude, s.longitude], {
        icon: customIcon
      });

      const popupContent = `
        <div style="font-family: sans-serif; min-width: 150px;">
            <div style="margin-bottom: ${showLink ? '8px' : '0'};">
                <strong style="font-size: 1.1em;">${s.brandName}</strong><br/>
                <span style="font-size: 0.9em; color: #555;">
                    ${s.city}, ${s.street} ${s.houseNumber}
                </span>
            </div>
            ${showLink ? `
            <a href="${detailsUrl}" 
               style="display: inline-block; padding: 4px 12px; background-color: #fff; border: 1px solid #ccc; color: #333; text-decoration: none; border-radius: 4px; font-size: 0.85em; width: 100%; text-align: center; box-sizing: border-box;">
               ${label}
            </a>` : ''} 
        </div>
      `;

      marker.bindPopup(popupContent);
      layer.addLayer(marker);
      
      if (!showLink && stations.length === 1) {
          marker.openPopup();
      }
    });
  }

  return <div ref={mapContainerRef} style={{ height: "100%", width: "100%", zIndex: 0 }} />;
}