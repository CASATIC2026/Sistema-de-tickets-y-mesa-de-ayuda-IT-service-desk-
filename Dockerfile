FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia todo el contexto para poder compilar
COPY . .

# 1. La carpeta interna se llama HelpDeskAPI y el archivo es HelpDeskAPI.csproj
RUN dotnet restore "HelpDeskAPI/HelpDeskAPI.csproj"
RUN dotnet publish "HelpDeskAPI/HelpDeskAPI.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

# 2. La DLL de salida que genera .NET sigue siendo HelpDeskAPI.dll
ENTRYPOINT ["dotnet", "HelpDeskAPI.dll"]