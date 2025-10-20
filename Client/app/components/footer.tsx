import * as React from "react";

export default function Footer() {
  return (
    <footer className="w-full bg-gray-900 text-gray-300 py-10">
      <div className="mx-auto max-w-6xl px-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {/* Left: Twórcy */}
          <div className="card p-4 bg-transparent border-none">
            <div className="card-body p-0">
              <h3 className="font-semibold text-lg">Twórcy</h3>
              <ul className="mt-2 space-y-1 text-sm">
                <li>Mateusz Bogacz-Drewniak</li>
                <li>Paweł Kruk</li>
                <li>Szymon Mikołajek</li>
                <li>Michał Nocuń</li>
                <li>Mateusz Chimkowski</li>
              </ul>
            </div>
          </div>

          {/* Middle: Odniesienia */}
          <div className="card p-4 bg-transparent border-none">
            <div className="card-body p-0">
              <h3 className="font-semibold text-lg text-center">Odniesienia</h3>
              <ul className="mt-2 space-y-1 text-sm text-center">
                <li><a href="/dashboard" className="link link-hover">Dashboard</a></li>
                <li><a href="/settings" className="link link-hover">Ustawienia</a></li>
                <li><a href="/map" className="link link-hover">Mapa</a></li>
                <li><a href="/list" className="link link-hover">Lista</a></li>
              </ul>
            </div>
          </div>

          {/* Right: prawa autorskie */}
          <div className="card p-4 bg-transparent border-none">
            <div className="card-body p-0 text-right">
              <h3 className="font-semibold text-lg">Prawa</h3>
              <p className="mt-2 text-sm">Dajcie tutaj prawa autorskie i elo</p>
            </div>
          </div>
        </div>

        <div className="text-center text-xs text-gray-500 mt-6">© {new Date().getFullYear()} FuelStats</div>
      </div>
    </footer>
  );
}