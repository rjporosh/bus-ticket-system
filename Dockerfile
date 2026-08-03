# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Core/BusTicketing.Domain/BusTicketing.Domain.csproj", "src/Core/BusTicketing.Domain/"]
COPY ["src/Core/BusTicketing.Application/BusTicketing.Application.csproj", "src/Core/BusTicketing.Application/"]
COPY ["src/Infrastructure/BusTicketing.Infrastructure/BusTicketing.Infrastructure.csproj", "src/Infrastructure/BusTicketing.Infrastructure/"]
COPY ["src/Presentation/BusTicketing.Api/BusTicketing.Api.csproj", "src/Presentation/BusTicketing.Api/"]
RUN dotnet restore "src/Presentation/BusTicketing.Api/BusTicketing.Api.csproj"

COPY . .
WORKDIR /src/src/Presentation/BusTicketing.Api
RUN dotnet publish "BusTicketing.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN addgroup --system --gid 1000 appgroup \
 && adduser --system --uid 1000 --ingroup appgroup appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

USER appuser

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD wget -qO- http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "BusTicketing.Api.dll"]
