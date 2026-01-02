FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
ENV HOME=/root
ENV DOTNET_CLI_CONTEXT_ANSI=true
ENV DOTNET_CLI_UI_LANGUAGE=en

COPY ["Data/Data.csproj", "Data/"]
COPY ["Services/Services.csproj", "Services/"]
COPY ["DTO/DTO.csproj", "DTO/"]
COPY ["Controllers/Controllers.csproj", "Controllers/"]
COPY ["Tests/Tests.csproj", "Tests/"]

RUN dotnet restore "Tests/Tests.csproj"

COPY . .

WORKDIR /src/Tests

RUN dotnet build "Tests.csproj" -c Release --no-restore

CMD dotnet test Tests.csproj \
    --logger "console;verbosity=detailed" \
    --logger "trx;LogFileName=/shared/volume/test_results.trx" \
    --blame \
    --no-build \
    -c Release \
    -- -parallel none -diagnostics \
    | tee /dev/tty
