# Etapa 1: Compilación
FROM ://microsoft.com AS build
WORKDIR /src

# Copia el archivo de proyecto y restaura las dependencias
COPY ["OfertaAcademica.Web.csproj", "./"]
RUN dotnet restore "OfertaAcademica.Web.csproj"

# Copia el resto de los archivos y compila en modo Release
COPY . .
RUN dotnet publish "OfertaAcademica.Web.csproj" -c Release -o /app/publish

# Etapa 2: Ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render asigna el puerto dinámicamente (usualmente el 8080 o 10000)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OfertaAcademica.Web.dll"]
