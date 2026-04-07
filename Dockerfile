FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY shared/EventBus/EventBus.csproj shared/EventBus/
COPY services/OrderService/OrderService.csproj services/OrderService/
RUN dotnet restore services/OrderService/OrderService.csproj
COPY . .
RUN dotnet publish services/OrderService -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderService.dll"]
