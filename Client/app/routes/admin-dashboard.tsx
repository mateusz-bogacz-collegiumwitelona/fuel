import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";

const API_BASE = "http://localhost:5111";

function parseJwt(token: string | null) {
    if (!token) return null;
    try {
        const payload = token.split(".")[1];
        const decoded = atob(payload);
        return JSON.parse(decodeURIComponent(escape(decoded)));
    } catch (e) {
        return null;
    }
}

export default function AdminDashboard() {
    const [email, setEmail] = React.useState<string | null>(null);

    React.useEffect(() => {
        (async () => {
            const token = localStorage.getItem("token");
            const expiration = localStorage.getItem("token_expiration");

            // jeśli token jest i nie wygasł – bierzemy z niego maila
            if (token && expiration && new Date(expiration) > new Date()) {
                const decoded = parseJwt(token);
                const userEmail = (decoded && (decoded.email || decoded.sub)) || null;
                setEmail(userEmail ?? "Zalogowany użytkownik");
                return;
            }

            // próba odświeżenia jak w dashboard.tsx
            try {
                const refreshRes = await fetch(`${API_BASE}/api/refresh`, {
                    method: "POST",
                    headers: { Accept: "application/json" },
                    credentials: "include",
                });

                if (refreshRes.ok) {
                    setEmail("Zalogowany użytkownik");
                    return;
                } else {
                    if (typeof window !== "undefined") window.location.href = "/login";
                }
            } catch (err) {
                if (typeof window !== "undefined") window.location.href = "/login";
            }
        })();
    }, []);

    const handleLogout = () => {
        localStorage.removeItem("token");
        localStorage.removeItem("token_expiration");
        if (typeof window !== "undefined") window.location.href = "/login";
    };

    return (
        <div className="min-h-screen bg-base-200 text-base-content flex flex-col">
            <Header />

            <main className="flex-1 mx-auto w-full max-w-6xl px-4 py-10">
                <h1 className="text-3xl font-bold mb-2">Panel administratora</h1>


                <div className="bg-base-300 rounded-xl p-6 shadow-md mb-8">
                    <p className="mb-4 text-lg">
                        Witaj w panelu administratora. Wybierz swoje działanie.
                    </p>
                    <div className="grid gap-4 sm:grid-cols-2 md:grid-cols-4">
                        <a
                            href="/admin/brand"
                            className="btn btn-primary w-full"
                        >
                            Brand panel
                        </a>
                        <a
                            href="/admin/users"
                            className="btn btn-primary w-full"
                        >
                            User panel
                        </a>
                        <a
                            href="/admin/stations"
                            className="btn btn-primary w-full"
                        >
                            Gas station panel
                        </a>
                        <a
                            href="/admin/proposals"
                            className="btn btn-primary w-full"
                        >
                            Proposal panel
                        </a>
                    </div>
                </div>
            </main>

            <Footer />
        </div>
    );
}
