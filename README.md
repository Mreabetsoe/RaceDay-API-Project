# RaceDay API

A backend API for managing running events. Organisers can create events and categories, and participants can enrol in events and view their enrolments.

## Features

- Register and log in as an Organiser or a Participant
- Organisers create events and link them to categories
- Participants enrol in an event category
- Role-based access control (for example, participants cannot create events)
- Automated security tests

## Technologies

- ASP.NET Core Web API (C#)
- Entity Framework Core
- SQL Server
- xUnit for testing

## Getting Started

1. Install the .NET SDK and Visual Studio 2022 or later.
2. Clone the repository:
3. Open `RaceDay.sln` in Visual Studio.
4. Update the connection string in `appsettings.json` to point to your SQL Server.
5. Run the project and open the API in your browser or use `RaceDay.API.http` to send test requests.

## Running the Tests


## Project Structure

- `RaceDay.API/`: the Web API (controllers, models, data context, migrations)
- `RaceDay.Tests/`: automated tests

## Author

Reabetsoe Modiri (Mreabetsoe)
