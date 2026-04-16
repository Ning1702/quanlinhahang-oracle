FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Quanlinhahang-Oracle.sln", "./"]
COPY ["Quanlinhahang.Data/Quanlinhahang.Data.csproj", "Quanlinhahang.Data/"]
COPY ["Khachhang/Quanlinhahang_Customer/Quanlinhahang_Customer.csproj", "Khachhang/Quanlinhahang_Customer/"]

RUN dotnet restore "Khachhang/Quanlinhahang_Customer/Quanlinhahang_Customer.csproj"

COPY . .
WORKDIR "/src/Khachhang/Quanlinhahang_Customer"
RUN dotnet publish "Quanlinhahang_Customer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "Quanlinhahang_Customer.dll"]