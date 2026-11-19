# Multi-stage build for the RiseUp MVC host.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/RiseUp.Web.UI/RiseUp.Web.UI.csproj
RUN dotnet publish src/RiseUp.Web.UI/RiseUp.Web.UI.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN groupadd -r riseup && useradd -r -g riseup riseup && mkdir -p /var/riseup/private && chown -R riseup:riseup /var/riseup
COPY --from=build /app .
USER riseup
ENV ASPNETCORE_URLS=http://+:8080 Storage__RootPath=/var/riseup/private
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "RiseUp.Web.UI.dll"]
