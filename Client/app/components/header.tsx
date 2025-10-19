import * as React from "react";

export default function Header() {
  const [theme, setTheme] = React.useState<string>(() => {
    if (typeof window === "undefined") return "dark";
    return localStorage.getItem("theme") || "dark";
  });

  React.useEffect(() => {
    if (typeof document !== "undefined") {
      document.documentElement.setAttribute("data-theme", theme);
      localStorage.setItem("theme", theme);
    }
  }, [theme]);

  const toggleTheme = () => setTheme((t) => (t === "dark" ? "light" : "dark"));

  const handleLogout = () => {
    if (typeof window !== "undefined") {
      localStorage.removeItem("token");
      localStorage.removeItem("token_expiration");
      window.location.href = "/login";
    }
  };

  return (
    <header className="w-full bg-gray-800 shadow-sm text-white">
      <div className="mx-auto max-w-6xl px-4 py-3 flex items-center justify-between">
        {/* LEFT: logo + title */}
        <div className="flex items-center gap-3">
          <a href="/dashboard" className="flex items-center gap-2">
            {/* jeśli używasz png -> /fuelstats.png lub jeśli w images -> /images/fuelstats.png */}
            <img src="/fuelstats.png" alt="FuelStats" className="h-8 w-8 rounded-sm" />
            <span className="text-xl font-bold">FuelStats</span>
          </a>

          {/* main nav (desktop) */}
          <nav className="hidden md:flex gap-2 items-center ml-4">
            <a href="/dashboard" className="btn btn-ghost btn-sm">Dashboard</a>
            <a href="/map" className="btn btn-ghost btn-sm">Mapa</a>
            <a href="/list" className="btn btn-ghost btn-sm">Lista</a>
          </nav>
        </div>

        {/* RIGHT: theme toggle, logout, hamburger */}
        <div className="flex items-center gap-3">
          <button
            onClick={toggleTheme}
            aria-label="Toggle theme"
            className="btn btn-ghost btn-sm"
            title="Zmień motyw"
          >
            {theme === "dark" ? "🌚" : "🌞"}
          </button>

          <button onClick={handleLogout} className="btn btn-error btn-sm">
            Wyloguj
          </button>

          {/* hamburger dropdown (mobile / extra menu) */}
          <div className="dropdown dropdown-end">
            <label tabIndex={0} className="btn btn-ghost btn-square">
              {/* hamburger icon */}
              <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 6h16M4 12h16M4 18h16" />
              </svg>
            </label>
            <ul tabIndex={0} className="menu menu-compact dropdown-content mt-3 p-2 shadow bg-base-100 rounded-box w-52">
              <li><a href="/settings">Ustawienia</a></li>
              <li><a href="/account">Ustawienia konta</a></li>
              <li>
                <details>
                  <summary>Język</summary>
                  <ul className="pl-4">
                    <li><button className="w-full text-left">Polski</button></li>
                    <li><button className="w-full text-left">English</button></li>
                  </ul>
                </details>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </header>
  );
}