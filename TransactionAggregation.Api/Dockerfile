FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ["TransactionAggregation.Api/TransactionAggregation.Api.csproj", "TransactionAggregation.Api/"]

RUN dotnet restore "TransactionAggregation.Api/TransactionAggregation.Api.csproj"

COPY . .

WORKDIR "/src/TransactionAggregation.Api"

RUN dotnet build "TransactionAggregation.Api.csproj" \
    -c Release \
    -o /app/build

FROM build AS publish

RUN dotnet publish "TransactionAggregation.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TransactionAggregation.Api.dll"]