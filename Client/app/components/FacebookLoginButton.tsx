import React, { useEffect } from 'react';

declare global {
    interface Window {
        fbAsyncInit: () => void;
        FB: any;
    }
}

interface FacebookLoginButtonProps {
    onLoginSuccess: (data: any) => void;
    onLoginFailure: (msg: string) => void;
}

const FacebookLoginButton: React.FC<FacebookLoginButtonProps> = ({ onLoginSuccess, onLoginFailure }) => {

    useEffect(() => {
        window.fbAsyncInit = function() {
            window.FB.init({
                appId      : '1367518931827311',
                cookie     : true,
                xfbml      : true,
                version    : 'v18.0'
            });
        };

        if (!document.getElementById('facebook-jssdk')) {
            const js = document.createElement('script');
            js.id = 'facebook-jssdk';
            js.src = "https://connect.facebook.net/pl_PL/sdk.js";
            document.body.appendChild(js);
        }
    }, []);

    // 1. Wydzielona funkcja asynchroniczna do komunikacji z Backendem
// Wewnątrz loginToServer w FacebookLoginButton.tsx

    const loginToServer = async (accessToken: string) => {
        try {
            // 1. Najpierw próbujemy się zalogować
            let res = await fetch("/api/facebook/login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ accessToken }),
                credentials: "include"
            });

            // 2. Jeśli serwer odpowie 404 (User not found), próbujemy REJESTRACJI
            if (res.status === 404) {
                console.log("Użytkownik nie istnieje, próbuję zarejestrować...");
                res = await fetch("/api/facebook/register", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ accessToken }),
                    credentials: "include"
                });
            }

            const data = await res.json();
            if(res.ok) {
                onLoginSuccess(data.data || data);
            } else {
                onLoginFailure(data.message || "Błąd serwera");
            }
        } catch (err) {
            console.error(err);
            onLoginFailure("Błąd połączenia");
        }
    };
    const handleLogin = () => {
        if (!window.FB) {
            onLoginFailure("Facebook SDK nie jest gotowy. Odśwież stronę.");
            return;
        }

        // 2. FB.login przyjmuje teraz zwykłą funkcję (bez async)
        window.FB.login((response: any) => {
            if (response.authResponse) {
                // Wywołujemy funkcję asynchroniczną, ale nie używamy 'await' wewnątrz callbacka SDK
                loginToServer(response.authResponse.accessToken);
            } else {
                onLoginFailure("Anulowano logowanie.");
            }
        }, { scope: 'public_profile,email' });
    };

    return (
        <button type="button" onClick={handleLogin} className="btn btn-primary w-full mt-2">
            Zaloguj przez Facebooka
        </button>
    );
};

export default FacebookLoginButton;