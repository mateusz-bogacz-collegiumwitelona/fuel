import React, { useEffect } from "react";
import { useTranslation } from "react-i18next";

interface GoogleLoginButtonProps {
    onLoginSuccess: (data: any) => void;
    onLoginFailure: (msg: string) => void;
    buttonText?: string;
}

const GoogleLoginButton: React.FC<GoogleLoginButtonProps> = ({ 
    onLoginSuccess, 
    onLoginFailure, 
    buttonText 
}) => {
    const { t } = useTranslation();

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
            onLoginFailure(t("login.error_google_token"));
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
            else onLoginFailure(data?.message || t("login.invalid_credentials"));
        } catch {
            onLoginFailure(t("login.connection_error"));
        }
    };

    const handleLogin = () => {
        if (!(window as any).google?.accounts?.id) {
            onLoginFailure(t("login.error_google_sdk"));
            return;
        }
        (window as any).google.accounts.id.prompt();
    };

    return (
        <button type="button" onClick={handleLogin} className="btn btn-error w-full mt-2 text-white">
            {buttonText || t("login.google_button")}
        </button>
    );
};

export default GoogleLoginButton;