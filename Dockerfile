
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS sdk
WORKDIR /src

ARG PROJECT_PATH=ObjectStorageService/ObjectStorageService.csproj
ARG CONFIGURATION=Release
ARG PUBLISH_DIR=/app/publish

COPY . .

RUN dotnet restore "$PROJECT_PATH"

RUN dotnet publish "$PROJECT_PATH" -c "$CONFIGURATION" -o "$PUBLISH_DIR" --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

ARG DLL_NAME=
ENV SERVICE_DLL=${DLL_NAME}

COPY --from=sdk /app/publish .

COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
