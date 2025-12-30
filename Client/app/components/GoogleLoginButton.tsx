import React, { useEffect } from "react";

interface GoogleLoginButtonProps {
    onLoginSuccess: (data: any) => void;
    onLoginFailure: (msg: string) => void;
}

const GoogleLoginButton: React.FC<GoogleLoginButtonProps> = ({ onLoginSuccess, onLoginFailure }) => {
    useEffect(() => {
        const scriptId = "google-identity-services";
        if (!document.getElementById(scriptId)) {
            const script = document.createElement("script");
            script.id = scriptId;
            script.src = "https://accounts.google.com/gsi/client";
            script.async = true;
            script.defer = true;
            document.body.appendChild(script);
        }

        const interval = setInterval(() => {
            if ((window as any).google?.accounts?.id) {
                (window as any).google.accounts.id.initialize({
                    client_id: "944868537622-hhuf5v6c5jomet4i2qnnop2q2vqk07n1.apps.googleusercontent.com",
                    callback: handleCredentialResponse,
                });
                clearInterval(interval);
            }
        }, 100);

        return () => clearInterval(interval);
    }, []);

    const handleCredentialResponse = async (response: any) => {
        if (!response.credential) {
            onLoginFailure("Brak tokenu od Google");
            return;
        }

        try {
            let res = await fetch("/api/google/login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ idToken: response.credential }),
                credentials: "include",
            });

            if (res.status === 404) {
                res = await fetch("/api/google/register", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ idToken: response.credential }),
                    credentials: "include",
                });
            }

            const data = await res.json();
            if (res.ok && data?.success) onLoginSuccess(data.data);
            else onLoginFailure(data?.message || "Błąd logowania");
        } catch {
            onLoginFailure("Błąd połączenia");
        }
    };

    const handleLogin = () => {
        if (!(window as any).google?.accounts?.id) {
            onLoginFailure("Google SDK nie jest gotowy. Odśwież stronę.");
            return;
        }
        (window as any).google.accounts.id.prompt(); // wywołanie prompt do logowania
    };

    return (
        <button type="button" onClick={handleLogin} className="btn btn-red w-full mt-2">
            Zaloguj przez Google
        </button>
    );
};

export default GoogleLoginButton;