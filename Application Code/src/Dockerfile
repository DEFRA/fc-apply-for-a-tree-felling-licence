# Runtime base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://*:8080 \
    COMPlus_EnableDiagnostics=0

# Create non-root user
RUN addgroup --gid 2000 aspnetgroup \
    && adduser --uid 1000 --gid 2000 aspnetuser \
    && chown aspnetuser:aspnetgroup /app
USER aspnetuser:aspnetgroup

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only project files (helps caching)
COPY ["Host Applications/Forestry.Flo.External.Web/Forestry.Flo.External.Web.csproj", "Host Applications/Forestry.Flo.External.Web/"]
COPY Services/*/*.csproj Services/

# Restore dependencies
RUN dotnet restore "Host Applications/Forestry.Flo.External.Web/Forestry.Flo.External.Web.csproj"

# Now copy everything else
COPY . .

# Publish stage
FROM build AS publish
ARG version
RUN dotnet publish "Host Applications/Forestry.Flo.External.Web/Forestry.Flo.External.Web.csproj" \
    -c Release -o /app/publish /p:Version=$version

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Forestry.Flo.External.Web.dll"]
