# Stage 1: Build the application
# Use the official .NET SDK image. Replace '8.0' with your target framework version.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# 1. Copy the project file into its own subdirectory to preserve the path
COPY ["hitster-mapper-server/hitster-mapper-server.csproj", "hitster-mapper-server/"]

# 2. Run restore on the specific project file
RUN dotnet restore "hitster-mapper-server/hitster-mapper-server.csproj"

# 3. Copy the rest of the project's source code
COPY ["hitster-mapper-server/.", "hitster-mapper-server/"]

# 4. Run publish on the specific project, which can now find the restored assets
RUN dotnet publish "hitster-mapper-server/hitster-mapper-server.csproj" -c Release -o /app/publish --no-restore

# Stage 2: Create the final runtime image
# Use the smaller ASP.NET runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy the published output from the 'build' stage
COPY --from=build /app/publish .

# Define the entry point for the container
# Replace 'YourAppName.dll' with the actual name of your project's DLL file.
ENTRYPOINT ["dotnet", "hitster-mapper-server.exe"]
