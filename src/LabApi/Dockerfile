# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Installer libkrb5 for Npsql (unngår warning om libgssapi_krb5.so.2)
RUN apt-get update && apt-get install -y libkrb5-3 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

RUN useradd -m appuser
USER appuser

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT [ "dotnet", "LabApi.dll" ]