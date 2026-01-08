import React, { useEffect } from 'react';
import { useTranslation } from "react-i18next";

declare global {
    interface Window {
        fbAsyncInit: () => void;
        FB: any;
    }
}

interface FacebookLoginButtonProps {
    onLoginSuccess: (data: any) => void;
    onLoginFailure: (msg: string) => void;
    buttonText?: string;
}

const FacebookLoginButton: React.FC<FacebookLoginButtonProps> = ({ 
    onLoginSuccess, 
    onLoginFailure, 
    buttonText 
}) => {
    const { t } = useTranslation();

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


    const loginToServer = async (accessToken: string) => {
        try {
            let res = await fetch("/api/facebook/login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ accessToken }),
                credentials: "include"
            });

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
                onLoginFailure(data.message || t("login.error_server"));
            }
        } catch (err) {
            console.error(err);
            onLoginFailure(t("login.connection_error"));
        }
    };

    const handleLogin = () => {
        if (!window.FB) {
            onLoginFailure(t("login.error_facebook_sdk"));
            return;
        }

        window.FB.login((response: any) => {
            if (response.authResponse) {
                loginToServer(response.authResponse.accessToken);
            } else {
                onLoginFailure(t("login.error_login_cancelled"));
            }
        }, { scope: 'public_profile,email' });
    };

    return (
        <button type="button" onClick={handleLogin} className="btn btn-primary w-full mt-2">
            {buttonText || t("login.facebook_button")}
        </button>
    );
};

export default FacebookLoginButton;