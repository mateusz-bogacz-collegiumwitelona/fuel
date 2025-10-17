import * as React from "react";

// Prosta funkcja do odczytania danych z tokena JWT
// - Token JWT ma trzy części rozdzielone kropkami: header.payload.signature
// - Payload (druga część) jest zakodowane base64url — tutaj używamy atob do dekodowania
// - Funkcja zwraca zdekodowany obiekt JSON lub null, jeśli dekodowanie nie powiodło się
function parseJwt(token: string) {
    try {
        // token.split('.')[1] -> payload
        // atob -> dekodowanie base64
        // JSON.parse -> obiekt JS
        return JSON.parse(atob(token.split(".")[1]));
    } catch (e) {
        // Jeśli coś pójdzie nie tak, logujemy i zwracamy null
        console.error("Nie można zdekodować tokena JWT:", e);
        return null;
    }
}

export default function Login() {
    // Local component state
    const [email, setEmail] = React.useState(""); // wartość pola email
    const [password, setPassword] = React.useState(""); // wartość pola hasła
    const [message, setMessage] = React.useState(""); // komunikaty np. błędy/logowanie

    // Funkcja wywoływana przy submit formularza
    // - Wysyła request POST do endpointu logowania
    // - Oczekuje w odpowiedzi JSON z polem `token` i `expiration` (jak w Twoim backendzie)
    const handleLogin = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setMessage("Logowanie...");

        try {
            const response = await fetch("http://localhost:5111/api/login", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    Accept: "application/json",
                },
                body: JSON.stringify({ email, password }),
            });

            // Jeśli status odpowiedzi nie jest ok (200..299) to pokazujemy komunikat
            if (!response.ok) {
                setMessage("Błędny email lub hasło");
                return;
            }

            const data = await response.json();

            // Oczekujemy, że backend zwróci token i opcjonalnie expiration
            if (data.token) {
                // Zapisujemy token w localStorage aby strony klienckie mogły wiedzieć, że użytkownik jest zalogowany
                localStorage.setItem("token", data.token);
                // Zakładamy, że backend przesyła expiration w formacie rozpoznawalnym przez Date
                localStorage.setItem("token_expiration", data.expiration);

                // Dekodujemy token aby odczytać role (przykład claim z .NET)
                const decoded = parseJwt(data.token);
                const role =
                    decoded?.[
                    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                    ];

                setMessage("Zalogowano pomyślnie!");

                // Po krótkim delayu przekierowujemy użytkownika zależnie od roli
                setTimeout(() => {
                    if (typeof window !== "undefined") {
                        if (role === "Admin") {
                            window.location.href = "/admin-dashboard";
                        } else {
                            window.location.href = "/dashboard";
                        }
                    }
                }, 1000);
            } else {
                // Jeśli backend nie zwróci tokena - informujemy użytkownika
                setMessage("Nie otrzymano tokena od serwera");
            }
        } catch (error) {
            // Błąd sieciowy
            console.error(error);
            setMessage("Błąd połączenia z serwerem");
        }
    };

    // useEffect uruchamiany raz po renderze — sprawdza czy mamy ważny token i ewentualnie przekierowuje
    React.useEffect(() => {
        const token = localStorage.getItem("token");
        const expiration = localStorage.getItem("token_expiration");

        // Jeśli jest token i data wygaśnięcia i jest w przyszłości -> automatyczne przekierowanie
        if (token && expiration && new Date(expiration) > new Date()) {
            const decoded = parseJwt(token);
            const role =
                decoded?.[
                "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                ];

            if (typeof window !== "undefined") {
                if (role === "Admin") {
                    window.location.href = "/admin-dashboard";
                } else {
                    window.location.href = "/dashboard";
                }
            }
        }
    }, []);

    // Render formularza logowania
    // Stylizacja używa Tailwind + DaisyUI (klasy wyglądają jak Tailwind — w Twoim projekcie
    // upewnij się, że Tailwind + DaisyUI są poprawnie skonfigurowane)
    return (
        <div className="flex justify-center items-center min-h-screen bg-gray-900 text-white">
            <form
                onSubmit={handleLogin}
                className="bg-gray-800 p-8 rounded-2xl shadow-lg flex flex-col gap-4 w-full max-w-sm"
            >
                <h2 className="text-2xl font-bold text-center mb-2">Logowanie</h2>

                <input
                    type="email"
                    placeholder="Email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                    className="p-3 rounded-md bg-gray-700 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
                />

                <input
                    type="password"
                    placeholder="Hasło"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                    className="p-3 rounded-md bg-gray-700 border border-gray-600 focus:border-blue-500 focus:ring-2 focus:ring-blue-400 outline-none"
                />

                <button
                    type="submit"
                    className="mt-2 bg-blue-600 hover:bg-blue-500 transition-colors text-white font-semibold py-2 rounded-md"
                >
                    Zaloguj
                </button>

                {message && (
                    <p className="text-center text-sm text-gray-300 mt-2">{message}</p>
                )}
            </form>
        </div>
    );
}
