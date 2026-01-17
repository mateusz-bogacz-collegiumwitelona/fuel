# Fuel App

Kompleksowa platforma do monitorowania, wizualizacji i społecznościowego zgłaszania cen paliw na stacjach. System umożliwia użytkownikom wyszukiwanie najlepszych cen na mapie, filtrowanie wyników oraz przesyłanie własnych aktualizacji cen wraz z weryfikacją fotograficzną.

## Funkcjonalności

- **Konta użytkowników:** Klasyczna rejestracja (e-mail/hasło) oraz logowanie społecznościowe (Facebook, Google).
- **Interaktywna mapa i lista:** Przeglądanie stacji paliw z zaawansowanym filtrowaniem (marka, rodzaj paliwa, aktualność ceny).
- **Społecznościowy charakter:** Użytkownicy mogą proponować aktualizacje cen za pomocą formularzy z dowodem fotograficznym.
- **Grywalizacja:** System punktowy nagradza użytkowników, których propozycje cen zostały zaakceptowane.
- **Panel Administracyjny:**
  - Zarządzanie użytkownikami (podgląd szczegółów, blokowanie).
  - Zarządzanie markami stacji, rodzajami paliw i lokalizacjami stacji.
  - Kolejka moderacji propozycji cen od użytkowników (akceptacja/odrzucenie ze statystykami).

## Architektura i Stos Technologiczny

Projekt oparty jest na nowoczesnej, skonteneryzowanej architekturze, oddzielającej backendowe REST API od frontendowego klienta.

| Komponent                | Technologia                  | Opis                                                                               |
| :----------------------- | :--------------------------- | :--------------------------------------------------------------------------------- |
| **Backend API**          | .NET 8 (C#)                  | Główna logika aplikacji, REST API, Identity, Swagger.                              |
| **Klient Frontend**      | Node.js based                | Niezależna aplikacja kliencka SPA (serwowana przez Nginx).                         |
| **Baza Danych**          | PostgreSQL + PostGIS         | Relacyjna baza danych z rozszerzeniem przestrzennym dla danych geograficznych.     |
| **Caching/Kolejkowanie** | Redis                        | Używany do wydajnego cache'owania i potencjalnie do obsługi wiadomości.            |
| **Magazyn Obiektów**     | Azure Blob Storage (Azurite) | Używany do przechowywania zdjęć propozycji cen (emulowany lokalnie przez Azurite). |
| **Serwis E-mail**        | Mailpit                      | Emulator serwera SMTP do przechwytywania e-maili wysyłanych podczas developmentu.  |
| **Odwrócone Proxy**      | Nginx                        | Serwuje frontend, rutuje zapytania API i obsługuje terminację SSL lokalnie.        |

## Wymagania wstępne

Jedynym wymaganiem do uruchomienia całego środowiska deweloperskiego jest **Docker** wraz z **Docker Compose**.

- [Docker Desktop](https://www.docker.com/products/docker-desktop) (Windows/Mac) lub Docker Engine + wtyczka Compose (Linux).

## Pierwsze kroki

Wykonaj poniższe kroki, aby uruchomić projekt lokalnie.

### 1. Sklonuj repozytorium

```bash
git clone https://github.com/mateusz-bogacz-collegiumwitelona/fuel
cd fuel
```

### 2. Skonfiguruj Zmienne Środowiskowe

Projekt w dużym stopniu polega na zmiennych środowiskowych. Dostarczony jest przykładowy plik konfiguracyjny.

1.  Skopiuj przykładowy plik, aby utworzyć lokalny plik `.env`:
    ```bash
    cp .env.example .env
    ```
2.  Otwórz plik `.env`. Do podstawowego lokalnego developmentu domyślne wartości są wystarczające. Jeśli jednak chcesz przetestować logowanie społecznościowe (Facebook/Google), musisz podać własne klucze aplikacji OAuth w odpowiednich sekcjach.

**Aktualna konfiguracja `.env`:**

```dotenv
#.net
API_PORT=5111

#postgres/postgis
POSTGRES_USER=user
POSTGRES_PASSWORD=pass
POSTGRES_DB=database
POSTGRES_PORT=5432
POSTGRES_HOST=postgis

#mailpit
MAILPIT_PORT=63854

#redis
REDIS_HOST=redis
REDIS_PORT=6379

#nginx
NGINX_HTTP_PORT=8080
NGINX_HTTPS_PORT=443

#client
CLIENT_PORT=4000

#azurite
AZURITE_BLOB_PORT=10000
AZURITE_QUEUE_PORT=10001
AZURITE_TABLE_PORT=10002

#facebook oauth
FACEBOOK_OAUTH_CLIENT_ID=
FACEBOOK_OAUTH_CLIENT_SECRET=

#google oauth
GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
```

3. Przejdź do katalogu Client

```bash
cd Client
```

4. Skopiuj przykładowy plik, aby utworzyć lokalny plik `.env`:

   ```bash
   cp .env.example .env
   ```

5. Otwórz plik `.env`. Do podstawowego lokalnego developmentu domyślne wartości są wystarczające. Jeśli jednak chcesz przetestować logowanie społecznościowe (Facebook/Google), musisz podać własne klucze aplikacji OAuth w odpowiednich sekcjach.

```dotenv
VITE_FACEBOOK_CLIENT_ID=
VITE_GOOGLE_CLIENT_ID=
```

### 3. Uruchom aplikację

Uruchom cały stos technologiczny za pomocą Docker Compose:

```bash
docker compose up -d
```

> **Uwaga dotycząca pierwszego uruchomienia:** Aplikacja backendowa jest skonfigurowana tak, aby automatycznie przeprowadzać migracje bazy danych i seedować (wypełniać) dane początkowe przy starcie. Pierwsze uruchomienie może potrwać minutę lub dwie, podczas gdy PostgreSQL się inicjalizuje. Możesz śledzić logi za pomocą `docker compose logs -f controllers`.

## Dostęp do serwisów

Po uruchomieniu wszystkich kontenerów możesz uzyskać dostęp do poszczególnych serwisów przez przeglądarkę. Główny punkt wejścia działa przez HTTPS z certyfikatem "self-signed", który należy zaakceptować w przeglądarce.

| Serwis                    | URL                             | Opis                                                                                                |
| :------------------------ | :------------------------------ | :-------------------------------------------------------------------------------------------------- |
| **Główna Aplikacja**      | `https://localhost:8443`        | Główny interfejs frontendowy serwowany przez Nginx. Należy zaakceptować ostrzeżenie o certyfikacie. |
| **Backend API (Swagger)** | `http://localhost:5111/swagger` | Bezpośredni dostęp do dokumentacji i testowania API backendowego.                                   |
| **Mailpit**               | `http://localhost:63854`        | Interfejs webowy do podglądu e-maili wysyłanych przez aplikację (np. potwierdzenia rejestracji).    |

## Uruchamianie testów

Projekt zawiera dedykowany zestaw testów, który działa w izolowanym środowisku kontenerowym, zapewniając spójność między środowiskiem deweloperskim a potokami CI/CD.

Aby uruchomić wszystkie testy lokalnie, wykonaj komendę:

```bash
docker compose run --build --rm tests
```

Ta komenda automatycznie:

1.  Zbuduje kontener testowy.
2.  Uruchomi niezbędne zależności (PostGIS, Redis), jeśli jeszcze nie działają.
3.  Wykona testy.
4.  Usunie kontener testowy po zakończeniu.

## Zarządzanie i Komendy CLI

Aplikacja backendowa posiada wbudowane komendy CLI (Command Line Interface) do wykonywania zadań administracyjnych, takich jak zarządzanie użytkownikami.

Ponieważ aplikacja działa wewnątrz kontenera Dockera, komendy te należy wykonywać używając `docker compose exec`, odnosząc się do serwisu `controllers`.

> **Uwaga:** Poniższe komendy wymagają, aby kontener backendu (`controllers`) był uruchomiony.

### Dostępne komendy

#### 1. Stworzenie nowego użytkownika

Pozwala na ręczne utworzenie użytkownika w bazie danych z poziomu terminala.

```bash
docker compose exec controllers dotnet /app/bin/Debug/net8.0/Controllers.dll user:create
```

Po uruchomieniu komenda interaktywnie poprosi o podanie niezbędnych danych (np. email, hasło).

#### 2. Nadanie uprawnień administratora

Nadaje rolę administratora istniejącemu użytkownikowi.

```bash
docker compose exec controllers dotnet /app/bin/Debug/net8.0/Controllers.dll user:promote
```

Komenda poprosi o podanie adresu email użytkownika, który ma zostać awansowany.

## Trwałość Danych (Data Persistence)

Projekt wykorzystuje nazwane wolumeny Dockera (`docker volumes`) do przechowywania danych. Oznacza to, że dane w bazie PostgreSQL, Redis oraz pliki w Azurite Blob Storage **są zachowywane** pomiędzy restartami kontenerów (`docker compose down` / `docker compose up`).

### Resetowanie środowiska

Jeśli chcesz całkowicie wyczyścić środowisko i zacząć "od zera" (np. usunąć całą bazę danych i wszystkie wgrane pliki), użyj flagi `-v` (volumes) przy zatrzymywaniu kontenerów:

```bash
# UWAGA: Ta komenda usunie wszystkie dane z bazy i storage'u!
docker compose down -v
```

Po wykonaniu tej komendy i ponownym uruchomieniu (`docker compose up -d`), aplikacja zainicjalizuje się na czysto, wykonując migracje i seedowanie danych od nowa.

## Rozwiązywanie problemów (Troubleshooting)

### Konflikt portów

Jeśli podczas uruchamiania `docker compose up` otrzymasz błąd o zajętym porcie (np. `bind: address already in use`), oznacza to, że inny proces na Twoim komputerze używa już jednego z portów zdefiniowanych w `.env`.

**Rozwiązanie:** Zmień numer portu po lewej stronie w pliku `.env` (np. zmień `NGINX_HTTPS_PORT=8443` na `84433`) i uruchom ponownie docker compose.

### Zmiany w kodzie nie są widoczne

W obecnej konfiguracji deweloperskiej zmiany w kodzie backendu (.NET) **nie są** automatycznie przeładowywane (brak Hot Reload dla C# w tej konfiguracji kontenera).

**Rozwiązanie:** Po wprowadzeniu znaczących zmian w kodzie backendu, należy przebudować i zrestartować kontener:

```bash
docker compose up -d --build controllers
```

## Autorzy i Licencja

**Autorzy:**

- Mateusz Bogacz-Drewniak – Team Leader / Backend Developer / DevOps
- Szymon Mikołajek – Tester / Project Manager
- Mateusz Chimkowski – Frontend Developer / UX/UI Designer
- Paweł Kruk – Frontend Developer / DevOps
- Michał Nocuń - Tester
