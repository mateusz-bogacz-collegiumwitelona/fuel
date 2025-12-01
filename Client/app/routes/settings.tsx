import * as React from "react";
import Header from "../components/header";
import Footer from "../components/footer";
import { API_BASE } from "../components/api";
import { useUserGuard } from "../components/useUserGuard";

type ProposalStats = {
  totalProposals?: number;
  approvedProposals?: number;
  rejectedProposals?: number;
  acceptedRate?: number;
  updatedAt?: string;
};

type UserProfile = {
  userName?: string;
  email?: string;
  proposalStatistics?: ProposalStats | null;
  createdAt?: string;
};

export default function SettingsPage() {
  const { state, email } = useUserGuard();

  const [activeTab, setActiveTab] = React.useState<
    "account" | "general" | "appearance"
  >("account");

  const [user, setUser] = React.useState<UserProfile | null>(null);
  const [loadingUser, setLoadingUser] = React.useState(true);
  const [userError, setUserError] = React.useState<string | null>(null);

  // form fields for account
  const [newUserName, setNewUserName] = React.useState("");
  const [changingName, setChangingName] = React.useState(false);
  const [nameMessage, setNameMessage] = React.useState<string | null>(null);

  const [newEmail, setNewEmail] = React.useState("");
  const [changingEmail, setChangingEmail] = React.useState(false);
  const [emailMessage, setEmailMessage] = React.useState<string | null>(null);

  const [currentPassword, setCurrentPassword] = React.useState("");
  const [newPassword, setNewPassword] = React.useState("");
  const [confirmNewPassword, setConfirmNewPassword] = React.useState("");
  const [changingPassword, setChangingPassword] = React.useState(false);
  const [passwordMessage, setPasswordMessage] = React.useState<string | null>(
    null,
  );

  React.useEffect(() => {
    if (state !== "allowed") return;

    (async () => {
      setLoadingUser(true);
      setUserError(null);
      try {
        const headers: Record<string, string> = { Accept: "application/json" };

        const res = await fetch(`${API_BASE}/api/me`, {
          method: "GET",
          headers,
          credentials: "include",
        });

        if (!res.ok) {
          // fallback na endpoint /api/me
          const fallback = await fetch(`${API_BASE}/api/me`, {
            method: "GET",
            headers,
            credentials: "include",
          });
          if (fallback.ok) {
            const data = await fallback.json();
            setUser(data);
            setNewUserName(data.userName ?? "");
            setNewEmail(data.email ?? "");
            setLoadingUser(false);
            return;
          }

          const txt = await res.text().catch(() => "");
          throw new Error(`user-fetch-failed ${res.status} ${txt}`);
        }

        const data = await res.json();
        setUser(data);
        setNewUserName(data.userName ?? "");
        setNewEmail(data.email ?? "");
      } catch (err) {
        console.error("Nie udało się pobrać profilu użytkownika:", err);
        setUserError(
          "Nie udało się pobrać profilu. Upewnij się, że jesteś zalogowany.",
        );
      } finally {
        setLoadingUser(false);
      }
    })();
  }, [state]);

  // Validation helpers
  function validateEmailFormat(email: string) {
    // simple regex for basic validation
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }

  function validatePasswordRules(pw: string) {
    if (pw.length < 6) return "Hasło musi mieć co najmniej 6 znaków.";
    if (!/[A-Z]/.test(pw))
      return "Hasło musi zawierać przynajmniej jedną wielką literę.";
    if (!/[0-9]/.test(pw))
      return "Hasło musi zawierać przynajmniej jedną cyfrę.";
    if (
      !/[!@#$%^&*(),.?":{}|<>\\/;\\[\\]\\-_=+~`]/.test(pw)
    )
      return "Hasło musi zawierać przynajmniej jeden znak specjalny.";
    return null;
  }

  async function handleChangeName(e?: React.FormEvent) {
    if (e) e.preventDefault();
    setNameMessage(null);
    setChangingName(true);
    try {
      const toSend = newUserName.trim();
      if (!toSend) {
        setNameMessage("Nazwa użytkownika nie może być pusta.");
        return;
      }

      const headers: Record<string, string> = { Accept: "application/json" };

      const res = await fetch(
        `${API_BASE}/api/user/change-name?userName=${encodeURIComponent(
          toSend,
        )}`,
        {
          method: "POST",
          headers,
          credentials: "include",
        },
      );

      const data = await res.json().catch(() => null);
      if (!res.ok || (data && data.success === false)) {
        const msg = (data && data.message) || `Błąd serwera: ${res.status}`;
        setNameMessage(msg);
        return;
      }

      setNameMessage("Nazwa użytkownika zmieniona pomyślnie.");
      setUser((u) => (u ? { ...u, userName: toSend } : u));
    } catch (err) {
      console.error("change-name error:", err);
      setNameMessage("Błąd połączenia z serwerem.");
    } finally {
      setChangingName(false);
    }
  }

  async function handleChangeEmail(e?: React.FormEvent) {
    if (e) e.preventDefault();
    setEmailMessage(null);
    setChangingEmail(true);
    try {
      const toSend = newEmail.trim();
      if (!validateEmailFormat(toSend)) {
        setEmailMessage("Nieprawidłowy format adresu email.");
        return;
      }

      const headers: Record<string, string> = { Accept: "application/json" };

      const res = await fetch(
        `${API_BASE}/api/user/change-email?newEmail=${encodeURIComponent(
          toSend,
        )}`,
        {
          method: "POST",
          headers,
          credentials: "include",
        },
      );

      const data = await res.json().catch(() => null);
      if (!res.ok || (data && data.success === false)) {
        const msg = (data && data.message) || `Błąd serwera: ${res.status}`;
        setEmailMessage(msg);
        return;
      }

      setEmailMessage(
        "Email zmieniony pomyślnie. Możesz potrzebować ponownego zalogowania.",
      );
      setUser((u) => (u ? { ...u, email: toSend } : u));
    } catch (err) {
      console.error("change-email error:", err);
      setEmailMessage("Błąd połączenia z serwerem.");
    } finally {
      setChangingEmail(false);
    }
  }

  async function handleChangePassword(e?: React.FormEvent) {
    if (e) e.preventDefault();
    setPasswordMessage(null);
    setChangingPassword(true);
    try {
      if (!currentPassword) {
        setPasswordMessage("Podaj aktualne hasło.");
        return;
      }

      if (newPassword !== confirmNewPassword) {
        setPasswordMessage("Nowe hasła nie są identyczne.");
        return;
      }

      const pwRuleError = validatePasswordRules(newPassword);
      if (pwRuleError) {
        setPasswordMessage(pwRuleError);
        return;
      }

      const headers: Record<string, string> = {
        Accept: "application/json",
        "Content-Type": "application/json",
      };

      const body = {
        currentPassword,
        newPassword,
        confirmNewPassword,
      };

      const res = await fetch(`${API_BASE}/api/user/change-password`, {
        method: "POST",
        headers,
        credentials: "include",
        body: JSON.stringify(body),
      });

      const data = await res.json().catch(() => null);
      if (!res.ok || (data && data.success === false)) {
        const msg = (data && data.message) || `Błąd serwera: ${res.status}`;
        setPasswordMessage(msg);
        return;
      }

      setPasswordMessage("Hasło zmienione pomyślnie.");
      // clear password fields
      setCurrentPassword("");
      setNewPassword("");
      setConfirmNewPassword("");
    } catch (err) {
      console.error("change-password error:", err);
      setPasswordMessage("Błąd połączenia z serwerem.");
    } finally {
      setChangingPassword(false);
    }
  }


  return (
    <div className="min-h-screen bg-base-200 text-base-content">
      <Header />

      <main className="mx-auto max-w-5xl px-4 py-8">
        <h1 className="text-2xl md:text-3xl font-bold mb-4">Ustawienia</h1>

        <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
          {/* Sidebar */}
          <aside className="col-span-1 bg-base-300 p-4 rounded-xl shadow-md">
            <nav className="flex flex-col gap-2">
              <button
                className={`btn btn-ghost justify-start ${activeTab === "account" ? "btn-active" : ""}`}
                onClick={() => setActiveTab("account")}
              >
                Ustawienia konta
              </button>

              <button
                className={`btn btn-ghost justify-start ${activeTab === "general" ? "btn-active" : ""}`}
                onClick={() => setActiveTab("general")}
              >
                Ogólne
              </button>

              <div className="divider my-2"></div>

              <a href="/dashboard" className="btn btn-outline">
                ← Powrót
              </a>
            </nav>
          </aside>

          {/* Content area */}
          <section className="col-span-1 md:col-span-3 bg-base-300 p-6 rounded-xl shadow-md">
            {loadingUser ? (
              <div>Ładowanie profilu...</div>
            ) : userError ? (
              <div className="text-error">{userError}</div>
            ) : (
              <div>
                {activeTab === "account" && (
                  <div className="grid grid-cols-1 gap-6">
                    <div className="flex items-center justify-between">
                      <div>
                        <h2 className="text-xl font-semibold">Ustawienia konta</h2>
                        <p className="text-sm text-gray-400">Zarządzaj swoim loginem, emailem i hasłem.</p>
                      </div>
                    </div>

                    <div className="bg-base-100 p-4 rounded">
                      <h3 className="font-medium mb-2">Dane konta</h3>
                      <div className="grid md:grid-cols-2 gap-4">
                        <div>
                          <div className="text-sm text-gray-400">Nazwa użytkownika</div>
                          <div className="font-medium">{user?.userName ?? "-"}</div>
                        </div>
                        <div>
                          <div className="text-sm text-gray-400">Email</div>
                          <div className="font-medium">{user?.email ?? "-"}</div>
                        </div>
                      </div>

                      <div className="mt-4">
                        <div className="text-sm text-gray-400">Zarejestrowano</div>
                        <div className="text-sm">{user?.createdAt ? new Date(user.createdAt).toLocaleString() : "-"}</div>
                      </div>
                    </div>

                    {/* change name */}
                    <form onSubmit={handleChangeName} className="bg-base-100 p-4 rounded">
                      <h3 className="font-medium mb-2">Zmień nazwę użytkownika</h3>
                      <div className="flex gap-2">
                        <input
                          className="input flex-1"
                          value={newUserName}
                          onChange={(e) => setNewUserName(e.target.value)}
                          placeholder="Nowa nazwa użytkownika"
                        />
                        <button type="submit" className="btn btn-primary" disabled={changingName}>
                          {changingName ? "Zmienianie..." : "Zmień"}
                        </button>
                      </div>
                      {nameMessage && <div className="text-sm text-gray-400 mt-2">{nameMessage}</div>}
                    </form>

                    {/* change email */}
                    <form onSubmit={handleChangeEmail} className="bg-base-100 p-4 rounded">
                      <h3 className="font-medium mb-2">Zmień email</h3>
                      <div className="flex gap-2">
                        <input
                          className="input flex-1"
                          value={newEmail}
                          onChange={(e) => setNewEmail(e.target.value)}
                          placeholder="Nowy email"
                          type="email"
                        />
                        <button type="submit" className="btn btn-primary" disabled={changingEmail}>
                          {changingEmail ? "Zmienianie..." : "Zmień"}
                        </button>
                      </div>
                      {emailMessage && <div className="text-sm text-gray-400 mt-2">{emailMessage}</div>}
                    </form>

                    {/* change password */}
                    <form onSubmit={handleChangePassword} className="bg-base-100 p-4 rounded">
                      <h3 className="font-medium mb-2">Zmień hasło</h3>

                      <div className="grid gap-2">
                        <input
                          className="input"
                          placeholder="Aktualne hasło"
                          type="password"
                          value={currentPassword}
                          onChange={(e) => setCurrentPassword(e.target.value)}
                        />

                        <input
                          className="input"
                          placeholder="Nowe hasło"
                          type="password"
                          value={newPassword}
                          onChange={(e) => setNewPassword(e.target.value)}
                        />

                        <input
                          className="input"
                          placeholder="Powtórz nowe hasło"
                          type="password"
                          value={confirmNewPassword}
                          onChange={(e) => setConfirmNewPassword(e.target.value)}
                        />

                        <div className="text-xs text-gray-400">
                          Hasło musi mieć co najmniej 6 znaków, jedną wielką literę, jedną cyfrę i jeden znak specjalny.
                        </div>

                        <div className="flex gap-2">
                          <button className="btn btn-primary" type="submit" disabled={changingPassword}>
                            {changingPassword ? "Zmienianie..." : "Zmień hasło"}
                          </button>
                          <button
                            className="btn btn-ghost"
                            type="button"
                            onClick={() => {
                              setCurrentPassword("");
                              setNewPassword("");
                              setConfirmNewPassword("");
                              setPasswordMessage(null);
                            }}
                          >
                            Anuluj
                          </button>
                        </div>

                        {passwordMessage && <div className="text-sm text-gray-400">{passwordMessage}</div>}
                      </div>
                    </form>

                    {/* proposal stats (from /api/user response) */}
                    <div className="bg-base-100 p-4 rounded">
                      <h3 className="font-medium mb-2">Statystyki propozycji</h3>
                      {user?.proposalStatistics ? (
                        <div className="grid md:grid-cols-4 gap-4">
                          <div className="p-2 bg-base-200 rounded text-base-content text-center">
                            <div className="font-bold">{user.proposalStatistics.totalProposals ?? 0}</div>
                            <div className="text-sm">Wszystkie</div>
                          </div>
                          <div className="p-2 bg-base-200 rounded text-success text-center">
                            <div className="font-bold">{user.proposalStatistics.approvedProposals ?? 0}</div>
                            <div className="text-sm">Zaakceptowane</div>
                          </div>
                          <div className="p-2 bg-base-200 rounded text-error text-center">
                            <div className="font-bold">{user.proposalStatistics.rejectedProposals ?? 0}</div>
                            <div className="text-sm">Odrzucone</div>
                          </div>
                          <div className="p-2 bg-base-200 text-info rounded text-center">
                            <div className="font-bold">{user.proposalStatistics.acceptedRate ?? "-"}%</div>
                            <div className="text-sm">Wskaźnik akceptacji</div>
                          </div>
                        </div>
                      ) : (
                        <div className="text-sm text-gray-400">Brak statystyk.</div>
                      )}
                    </div>
                  </div>
                )}

                {activeTab === "general" && (
                  <div>
                    <h2 className="text-xl font-semibold mb-2">Ogólne</h2>
                    <div className="bg-base-100 p-4 rounded">
                      <p className="text-sm text-gray-400 mb-2">Losowe rzeczy, nie wiem co tu dać xD:</p>
                      <div className="grid md:grid-cols-2 gap-4">
                        <label className="flex items-center gap-2">
                          <input type="checkbox" className="checkbox" />
                          <span className="text-sm">Otrzymuj powiadomienia o akceptacji propozycji</span>
                        </label>

                        <label className="flex items-center gap-2">
                          <input type="checkbox" className="checkbox" />
                          <span className="text-sm">Automatyczne doładowanie lokalizacji przy starcie</span>
                        </label>
                      </div>
                      <div className="mt-4">
                        <button className="btn btn-primary">Zapisz ustawienia</button>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            )}
          </section>
        </div>
      </main>

      <Footer />
    </div>
  );
}
