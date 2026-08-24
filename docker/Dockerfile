# Stage 1: Build & Publish with IL Trimming
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution and project files for caching restore layer
COPY MonoSlice.slnx ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY global.json ./
COPY src/MonoSlice.Shared/MonoSlice.Shared.Abstractions/*.csproj ./src/MonoSlice.Shared/MonoSlice.Shared.Abstractions/
COPY src/MonoSlice.Shared/MonoSlice.Shared.Infrastructure/*.csproj ./src/MonoSlice.Shared/MonoSlice.Shared.Infrastructure/
COPY src/Modules/MonoSlice.Modules.Users/*.csproj ./src/Modules/MonoSlice.Modules.Users/
COPY src/Modules/MonoSlice.Modules.Catalog/*.csproj ./src/Modules/MonoSlice.Modules.Catalog/
COPY src/MonoSlice.Host/*.csproj ./src/MonoSlice.Host/

RUN dotnet restore -r linux-x64 src/MonoSlice.Host/MonoSlice.Host.csproj

# Copy source tree and publish self-contained trimmed binary
COPY . .
WORKDIR /app/src/MonoSlice.Host
RUN dotnet publish -c Release -r linux-x64 --self-contained \
    -p:PublishTrimmed=true \
    -p:TrimMode=partial \
    -p:StripSymbols=true \
    -o /out

# Stage 2: Ultra-lightweight Chiseled / Distroless Runtime with ICU support (~60MB total image size)
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled-extra AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /out .

# Run self-contained executable directly
ENTRYPOINT ["./MonoSlice.Host"]
