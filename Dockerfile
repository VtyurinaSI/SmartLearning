# ===================== Builder (SDK) =====================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS sdk
WORKDIR /src

# Аргументы сборки (переопределяются из docker-compose)
ARG PROJECT_PATH=SmartLearning/ObjectStorageService/ObjectStorageService.csproj
ARG CONFIGURATION=Release
ARG PUBLISH_DIR=/app/publish
ARG DLL_NAME=ObjectStorageService.dll

# Если есть .sln/Directory.Build.* — это улучшит кеш
COPY *.sln ./
COPY Directory.Build.* ./  # опционально; не страшно, если файлов нет

# Копируем только csproj для быстрого restore (адаптируй под свою структуру)
COPY SmartLearning/**/**/*.csproj SmartLearning/

# Restore
RUN dotnet restore "$PROJECT_PATH"

# Копируем исходники проекта
COPY SmartLearning/ SmartLearning/

# Publish
RUN dotnet publish "$PROJECT_PATH" -c "$CONFIGURATION" -o "$PUBLISH_DIR" --no-restore

# ===================== Runtime (ASP.NET) =====================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

# Имя DLL передаём через ENV (не обязательно, но удобно)
ARG DLL_NAME=ObjectStorageService.dll
ENV SERVICE_DLL=${DLL_NAME}

ARG PUBLISH_DIR=/app/publish
COPY --from=sdk "$PUBLISH_DIR" .

# Универсальный entrypoint — запускает SERVICE_DLL, либо первую найденную .dll
COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
