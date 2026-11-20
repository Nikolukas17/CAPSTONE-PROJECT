# Etapa 1: Compilación
FROM ://microsoft.com AS build
WORKDIR /src

COPY ["OfertaAcademica.Web.csproj", "./"]
RUN dotnet restore "OfertaAcademica.Web.csproj"

COPY . .
RUN dotnet publish "OfertaAcademica.Web.csproj" -c Release -o /app/publish

# Etapa 2: Ejecución
FROM ://microsoft.com AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OfertaAcademica.Web.dll"]
