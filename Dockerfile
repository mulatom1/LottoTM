# Etap 1: Budowanie aplikacji frontendowej (React + Vite)
# Używamy lekkiego, oficjalnego obrazu Node.js.
FROM node:20-alpine AS frontend
WORKDIR /app

# Kopiujemy pliki package.json i package-lock.json, aby wykorzystać cache'owanie warstw Dockera.
COPY src/client/app01/package*.json ./

# Instalujemy zależności npm.
RUN npm install

# Kopiujemy resztę kodu źródłowego frontendu.
COPY src/client/app01/. ./

# Budujemy aplikację w trybie produkcyjnym.
# Wynikiem będzie folder /app/dist zawierający wszystkie statyczne pliki.
RUN npm run build

# ---

# Etap 2: Budowanie i publikacja aplikacji backendowej (.NET 8)
# Używamy oficjalnego obrazu .NET 8 SDK.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiujemy plik solucji i pliki projektów, aby przywrócić zależności w osobnej warstwie.
# Dostosuj ścieżki, jeśli masz więcej projektów w solucji.
COPY LottoTM.sln .
COPY src/server/LottoTM.Server.Api/LottoTM.Server.Api.csproj src/server/LottoTM.Server.Api/

# Przywracamy pakiety NuGet dla całej solucji.
RUN dotnet restore "LottoTM.sln"

# Kopiujemy cały kod źródłowy do kontenera.
COPY . .

# Publikujemy aplikację API w konfiguracji Release.
# --no-restore pomija ponowne przywracanie pakietów, które już wykonaliśmy.
RUN dotnet publish "src/server/LottoTM.Server.Api/LottoTM.Server.Api.csproj" -c Release -o /app/publish --no-restore

# ---

# Etap 3: Tworzenie finalnego obrazu produkcyjnego
# Używamy lekkiego obrazu środowiska uruchomieniowego ASP.NET 8.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Ustawiamy domyślny port, na którym nasłuchują aplikacje .NET 8 w kontenerach.
EXPOSE 8080

# Kopiujemy opublikowane pliki backendu z etapu budowania.
COPY --from=build /app/publish .

# Kopiujemy zbudowane pliki frontendu (z etapu 'frontend') do folderu wwwroot.
# Backend musi być skonfigurowany, aby serwować pliki z tego folderu.
COPY --from=frontend /app/dist ./wwwroot

# Ustawiamy punkt wejścia dla kontenera, aby uruchomić aplikację.
ENTRYPOINT ["dotnet", "LottoTM.Server.Api.dll"]