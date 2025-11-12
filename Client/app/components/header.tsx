import React from "react";
import ThemeController from "../components/ThemeController";
import { useTheme } from "../context/ThemeContext";

const API_BASE = "http://localhost:5111";

export default function Header() {
  const { theme, setTheme } = useTheme();

  const handleLogout = async () => {
    try {
      await fetch(`${API_BASE}/api/logout`, {
        method: "POST",
        credentials: "include",
      });
    } catch (e) {
      console.warn("Logout request failed:", e);
    }
    localStorage.removeItem("token");
    localStorage.removeItem("token_expiration");
    if (typeof window !== "undefined") window.location.href = "/login";
  };


    return (
        <header className="w-full bg-base-300 shadow-sm text-base-content">
            <div className="mx-auto max-w-6xl px-4 py-3 flex items-center justify-between">
                <div className="flex items-center gap-3">
                    <a href="/dashboard" className="flex items-center gap-2">
                        <img src="/images/fuelstats.png" alt="FuelStats" className="h-8 w-8 rounded-sm" />
                        <span className="text-xl font-bold">FuelStats</span>
                    </a>
                </div>

                <div className="flex items-center gap-3">
                    <ThemeController theme={theme} setTheme={setTheme} />

                    <div className="dropdown">
                        <div tabIndex={0} role="button" className="btn m-1">Język</div>
                        <ul tabIndex={0} className="dropdown-content menu bg-base-100 rounded-box z-[1] w-52 p-2 shadow-sm">
                            <li><a>Polski</a></li>
                            <li><a>English</a></li>
                        </ul>
                    </div>

                    <div className="dropdown dropdown-end">
                        <label tabIndex={0} className="btn btn-ghost btn-square">
                            <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 6h16M4 12h16M4 18h16" />
                            </svg>
                        </label>
                        <ul tabIndex={0} className="menu menu-compact dropdown-content mt-3 p-2 shadow bg-base-100 rounded-box w-52">
                            <li><a href="/settings">Ustawienia</a></li>
                            <li><a href="/dashboard">Dashboard</a></li>
                            <li><a href="/map">Mapa</a></li>
                            <li><a href="/list">Lista</a></li>
                            <li><button onClick={handleLogout} className="w-full text-left">Wyloguj</button></li>
                        </ul>
                    </div>
                </div>
            </div>
        </header>
    );
}