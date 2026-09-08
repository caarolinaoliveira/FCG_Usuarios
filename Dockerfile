FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore FCG.Presentation/FCG.Presentation.csproj
RUN dotnet publish FCG.Presentation/FCG.Presentation.csproj -c Release -o /app/publish

FROM base AS final

RUN apt-get update && apt-get install -y wget ca-certificates \
    && wget -q https://download.newrelic.com/dot_net_agent/latest_release/newrelic-dotnet-agent_amd64.tar.gz -O /tmp/newrelic-dotnet-agent.tar.gz \
    && mkdir -p /usr/local/newrelic-dotnet-agent \
    && tar -xzf /tmp/newrelic-dotnet-agent.tar.gz -C /usr/local/newrelic-dotnet-agent --strip-components=1 \
    && rm /tmp/newrelic-dotnet-agent.tar.gz \
    && rm -rf /var/lib/apt/lists/*

ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
    CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
    CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so \
    NEW_RELIC_APP_NAME="FCG-UsuariosAPI"

WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FCG.Presentation.dll"]