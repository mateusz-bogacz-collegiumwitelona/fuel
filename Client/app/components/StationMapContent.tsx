import React, { useEffect, useRef } from "react";
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

const brandColors: Record<string, string> = {
  Default: "black",
  Orlen: "red",
  BP: "green",
  Shell: "yellow",
  "Circle K": "orange",
  Moya: "blue",
  Lotos: "gold",
};

type StationMapProps = {
  brandName: string;
  latitude: number;
  longitude: number;
  city: string;
  street: string;
  houseNumber: string;
};

const StationMapContent = ({ 
  brandName, 
  latitude, 
  longitude,
  city,
  street,
  houseNumber 
}: StationMapProps) => {
  const mapContainerRef = React.useRef<HTMLDivElement>(null);
  const mapInstanceRef = React.useRef<L.Map | null>(null);

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
    if (!mapContainerRef.current) return;

    if (!mapInstanceRef.current) {
      const map = L.map(mapContainerRef.current).setView([latitude, longitude], 15);
      
      L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        attribution: '&copy; OpenStreetMap contributors'
      }).addTo(map);

      mapInstanceRef.current = map;
    } else {
      mapInstanceRef.current.setView([latitude, longitude], 15);
    }

    const map = mapInstanceRef.current;
    
    map.eachLayer((layer) => {
        if (layer instanceof L.Marker) {
            map.removeLayer(layer);
        }
    });

    const marker = L.marker([latitude, longitude], { 
        icon: getMarkerIcon(brandName) 
    }).addTo(map);

    const popupContent = `
        <div style="font-family: sans-serif;">
            <strong>${brandName}</strong><br/>
            ${city}, ${street} ${houseNumber}
        </div>
    `;
    marker.bindPopup(popupContent);

    return () => {
    };
  }, [latitude, longitude, brandName, city, street, houseNumber]); 

  return <div ref={mapContainerRef} style={{ height: "100%", width: "100%", zIndex: 0 }} />;
};

export default StationMapContent;