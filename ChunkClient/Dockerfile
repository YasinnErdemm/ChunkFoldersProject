FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ChunkClient.csproj", "./"]
RUN dotnet restore "ChunkClient.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "ChunkClient.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChunkClient.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChunkClient.dll"]
