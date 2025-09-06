# Stage 1: Build the application
# Use the official .NET SDK image. Replace '8.0' with your target framework version.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy the project file and restore dependencies first
# This leverages Docker layer caching to speed up future builds
COPY hitster-mapper-server/*.csproj .
RUN dotnet restore

# Copy the rest of the source code and publish the application
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Create the final runtime image
# Use the smaller ASP.NET runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy the published output from the 'build' stage
COPY --from=build /app/publish .

# Define the entry point for the container
# Replace 'YourAppName.dll' with the actual name of your project's DLL file.
ENTRYPOINT ["dotnet", "hitster-mapper-server.exe"]
