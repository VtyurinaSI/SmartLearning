FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG PROJECT_PATH
WORKDIR /src
COPY . .
RUN dotnet restore SmartLearning/SmartLearning.sln \
 && dotnet publish "$PROJECT_PATH" -c Release -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV APP_DLL=App.dll
EXPOSE 8080
ENTRYPOINT ["sh","-c","dotnet /app/$APP_DLL"]
