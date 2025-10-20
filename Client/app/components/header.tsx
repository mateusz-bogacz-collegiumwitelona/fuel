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
            <img src="/images/fuelstats.png" alt="FuelStats" className="h-8 w-8 rounded-sm" />
            <span className="text-xl font-bold">FuelStats</span>
          </a>

          {/* main nav (desktop) */}
          <nav className="hidden md:flex gap-2 items-center ml-4">
            <a href="/dashboard" className="btn btn-outline">Dashboard</a>
            <a href="/map" className="btn btn-outline">Mapa</a>
            <a href="/list" className="btn btn-outline">Lista</a>
          </nav>
        </div>

        {/* RIGHT: theme toggle, logout, hamburger */}
        <div className="flex items-center gap-3">
          <label class="flex cursor-pointer gap-2">
            <svg
              xmlns="http://www.w3.org/2000/svg"
              width="20"
              height="20"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round">
              <circle cx="12" cy="12" r="5" />
              <path
                d="M12 1v2M12 21v2M4.2 4.2l1.4 1.4M18.4 18.4l1.4 1.4M1 12h2M21 12h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4" />
            </svg>
            <input type="checkbox" value="synthwave" class="toggle theme-controller" />
            <svg
              xmlns="http://www.w3.org/2000/svg"
              width="20"
              height="20"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round">
              <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"></path>
            </svg>
          </label>

          <button onClick={handleLogout} class="btn btn-dash btn-error">Wyloguj</button>

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