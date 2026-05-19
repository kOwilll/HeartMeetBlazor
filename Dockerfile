FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/HeartMeet.Domain/HeartMeet.Domain.csproj",  "HeartMeet.Domain/"]
COPY ["src/HeartMeet.Data/HeartMeet.Data.csproj",      "HeartMeet.Data/"]
COPY ["src/HeartMeet.Services/HeartMeet.Services.csproj","HeartMeet.Services/"]
COPY ["src/HeartMeet.Web/HeartMeet.Web.csproj",        "HeartMeet.Web/"]
RUN dotnet restore "HeartMeet.Web/HeartMeet.Web.csproj"
COPY src/ .
RUN dotnet publish "HeartMeet.Web/HeartMeet.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads
ENTRYPOINT ["dotnet","HeartMeet.Web.dll"]
