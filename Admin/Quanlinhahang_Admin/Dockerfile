FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

WORKDIR /src/Admin/Quanlinhahang_Admin/Quanlinhahang_Admin
RUN dotnet restore "Quanlinhahang_Admin.csproj"
RUN dotnet publish "Quanlinhahang_Admin.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "Quanlinhahang_Admin.dll"]