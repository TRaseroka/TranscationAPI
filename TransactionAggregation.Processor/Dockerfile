FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ["TransactionAggregation.Processor/TransactionAggregation.Processor.csproj", "TransactionAggregation.Processor/"]
COPY ["TransactionAggregation.Contracts/TransactionAggregation.Contracts.csproj", "TransactionAggregation.Contracts/"]
COPY ["TransactionAggregation.Persistence/TransactionAggregation.Persistence.csproj", "TransactionAggregation.Persistence/"]

RUN dotnet restore "TransactionAggregation.Processor/TransactionAggregation.Processor.csproj"

COPY . .

WORKDIR "/src/TransactionAggregation.Processor"

RUN dotnet publish "TransactionAggregation.Processor.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TransactionAggregation.Processor.dll"]