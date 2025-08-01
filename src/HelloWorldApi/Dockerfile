FROM mcr.microsoft.com/dotnet/runtime-deps:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
RUN apt-get update && \
    apt-get install -y \
      clang zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*
COPY "HelloWorldApi.csproj" .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["/app/HelloWorldApi"]
