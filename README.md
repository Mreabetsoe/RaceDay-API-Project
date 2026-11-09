# RaceDay

RaceDay is an event management platform for running, walking and cycling events. Organisers create and manage events, and Participants browse events, enrol in a category and view their race results.

This repository contains the Part 3 work for PROG6212 (Programming 2B) at The Independent Institute of Education.

## Projects

| Project | Description |
|---|---|
| `RaceDay.API` | ASP.NET Core Web API with Entity Framework Core and SQL Server. Handles authentication, events, categories, enrolments, results and profiles. Uploads banner images to Azure Blob Storage (or Azurite locally). |
| `RaceDay.Web` | ASP.NET Core MVC front end. Consumes the API for all data. Contains no database access. |
| `RaceDay.Tests` | xUnit tests for the API. |

## Prerequisites

- .NET 8 SDK or later
- SQL Server (LocalDB or Express)
- Docker Desktop (for Azurite and the containerised app)
- Visual Studio 2022 or later

## Running locally

### 1. Start Azurite (local Blob Storage)

```powershell
docker run -d --name azurite -p 10000:10000 mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0 --skipApiVersionCheck
```

### 2. Set up the API

From the `RaceDay.API` folder:

```powershell
dotnet user-secrets set "ConnectionStrings:AzureStorage" "UseDevelopmentStorage=true"
dotnet user-secrets set "BlobContainerName" "raceday-images"
dotnet ef database update
dotnet run
```

The API runs at `http://localhost:5195`. Swagger is available at `http://localhost:5195/swagger`.

### 3. Run the MVC app

Set both projects to start: right-click the solution, choose **Configure Startup Projects**, select **Multiple startup projects**, and set `RaceDay.API` and `RaceDay.Web` to **Start**. Press **F5**.

The MVC app reads the API address from `RaceDay.Web/appsettings.json`:

```json
"ApiSettings": {
  "BaseUrl": "http://localhost:5195/"
}
```

## Running with Docker

The single command to run the containerised app:

```powershell
docker build -t raceday-web -f RaceDay.Web/Dockerfile .
docker run -d -p 8080:8080 --name raceday-web -e ApiSettings__BaseUrl=http://host.docker.internal:5195/ raceday-web
```

Then open `http://localhost:8080`.

## Test accounts

| Role | Email | Password |
|---|---|---|
| Organiser | thabo@raceday.co.za | Organiser123! |
| Participant | lerato@gmail.com | Participant123! |

These accounts are created by the seed data when the database is first set up.

## Running the tests

```powershell
dotnet test
```

## Azure Blob Storage

Banner images are uploaded through `POST /api/events/{id}/banner`. The API stores each image in the `raceday-images` container and saves the URL on the event. The MVC app never talks to Azure directly.

## Video demonstration

YouTube (unlisted): [add link here]

## Status

- Part 2 API: complete, all tests passing.
- Part 3 API: authentication, events, categories, enrolments, results, profile and banner upload.
- Part 3 MVC: connection to the API and events listing working. Login, Organiser and Participant pages in progress.
- Docker: Dockerfile in progress.