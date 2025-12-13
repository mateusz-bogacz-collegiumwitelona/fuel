import React, { useEffect } from "react";
import { API_BASE } from "./api";

interface FacebookButtonProps {
  onLoginSuccess: (data: any) => void;
  onLoginFailure: (msg: string) => void;
  buttonText?: string;
}

declare global {
  interface Window {
    fbAsyncInit: () => void;
    FB: any;
  }
}

export default function FacebookButton({
  onLoginSuccess,
  onLoginFailure,
  buttonText = "Kontynuuj z Facebook",
}: FacebookButtonProps) {
  
  useEffect(() => {
    window.fbAsyncInit = function () {
      window.FB.init({
        appId: import.meta.env.FACEBOOK_OAUTH_CLIENT_ID,
        cookie: true,
        xfbml: true,
        version: "v18.0",
      });
    };

    if (!document.getElementById("facebook-jssdk")) {
      const js = document.createElement("script");
      js.id = "facebook-jssdk";
      js.src = "https://connect.facebook.net/en_US/sdk.js";
      js.async = true;
      js.defer = true;
      document.body.appendChild(js);
    }
  }, []);

  const handleLoginClick = () => {
    if (!window.FB) {
      onLoginFailure("Facebook SDK nie jest jeszcze załadowany.");
      return;
    }

    window.FB.login(
      function (response: any) {
        if (response.authResponse) {
          const token = response.authResponse.accessToken;
          console.log("FB Token received, sending to backend...");

          fetch(`${API_BASE}/api/login/facebook`, {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
              Accept: "application/json",
            },
            body: JSON.stringify({ accessToken: token }),
          })
            .then((res) => res.json().then((data) => ({ status: res.status, body: data })))
            .then(({ status, body }) => {
              if (status >= 200 && status < 300) {
                onLoginSuccess(body);
              } else {
                onLoginFailure(body.message || "Błąd logowania przez Facebook.");
              }
            })
            .catch((err) => {
              console.error(err);
              onLoginFailure("Błąd połączenia z serwerem.");
            });
        } else {
          console.log("User cancelled login or did not fully authorize.");
          onLoginFailure("Logowanie anulowane.");
        }
      },
      { scope: "public_profile,email" }
    );
  };

  return (
    <button
      onClick={handleLoginClick}
      type="button"
      className="btn w-full bg-[#1877F2] hover:bg-[#166fe5] text-white border-none mt-2 flex gap-2 no-animation"
    >
      <svg
        xmlns="http://www.w3.org/2000/svg"
        width="24"
        height="24"
        viewBox="0 0 24 24"
        fill="currentColor"
      >
        <path d="M9 8h-3v4h3v12h5v-12h3.642l.358-4h-4v-1.667c0-.955.192-1.333 1.115-1.333h2.885v-5h-3.808c-3.596 0-5.192 1.583-5.192 4.615v3.385z" />
      </svg>
      {buttonText}
    </button>
  );
}
