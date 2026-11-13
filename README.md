<<<<<<< HEAD
# RaceDay - PROG6212 Programming 2B

| | |
|---|---|
| Module | PROG6212 - Programming 2B |
| Student | Benedict Letebele |
| Student Number | ST10483847 |
| Year | 2026 |

[![Planning document check](https://github.com/Benedict-Letebele1/PROG6212P1/actions/workflows/docs-check.yml/badge.svg)](https://github.com/Benedict-Letebele1/PROG6212P1/actions/workflows/docs-check.yml)
[![RaceDay - Build and Test (API and MVC)](https://github.com/Benedict-Letebele1/PROG6212P1/actions/workflows/api-build-test.yml/badge.svg?branch=part-3)](https://github.com/Benedict-Letebele1/PROG6212P1/actions/workflows/api-build-test.yml)

## Branches

| Branch | Contents |
|---|---|
| `main` | Part 1: planning documents (ERD, endpoint plan, SQL script) and the planning document check |
| `part-2` | Part 2: the ASP.NET Core Web API, with EF Core code-first, session login, Swagger and unit tests |
| `part-3` | **Part 3 (POE): the ASP.NET Core MVC web app, Azure Blob Storage in the API, and Docker.** This branch also holds Parts 1 and 2. |

## System Overview

RaceDay is an event management platform for South Africa's road running, walking and cycling community. Organisers create and manage events, their categories and results. Participants browse events, enrol in a category and track their own results.

The solution has four projects:

| Project | What it is |
|---|---|
| `RaceDayApi` | Part 2 REST API (ASP.NET Core Web API, EF Core, SQL Server, session login, Swagger). In Part 3 it also stores images in Azure Blob Storage. |
| `RaceDayWeb` | Part 3 MVC web app. It has **no database access** and no business rules of its own. Every page calls the API through `RaceDayApiClient`. |
| `RaceDayApi.Tests` | API tests (xUnit, in-memory SQLite). |
| `RaceDayWeb.Tests` | MVC tests (xUnit, the MVC app hosted in memory against a fake API). |

```
Browser ──> RaceDayWeb (MVC, port 5090 or Docker 8080)
                 │  HTTP + the user's API session cookie
                 ▼
            RaceDayApi (port 5088) ──> SQL Server (LocalDB)
                 │
                 └──> Azure Blob Storage (banners and profile pictures)
```

## Roles

**Organiser**
- Dashboard with every event they created, total enrolments per event and upcoming event dates
- Creates, edits and deletes their own events (name, description, date, location, distance, type) and uploads a banner
- Adds, edits and deletes the age or distance categories of each event
- Views every participant enrolled in an event, with their category and colour-coded status, and confirms or cancels entries
- Captures and publishes finish times and positions after the event

**Participant**
- Home page with upcoming events and filters for type, date and location
- Event page with full details and categories, and an **Enter event** button that records the enrolment immediately
- My enrolments, with colour-coded statuses (green Confirmed, amber Pending, red Cancelled)
- My results, with event name, date, category, finish time and position, for example "47th out of 312 finishers"
- Profile page to update personal details and upload a profile picture

Guests can browse events. Protected pages send guests to the login page, and the wrong role gets an Access Denied page. The API checks the role again on every call, and returns 401 when nobody is logged in and 403 when the wrong role calls it.

## Setup and Run (Visual Studio)

Requirements: Visual Studio 2022 (17.14 or later) or Visual Studio 2026 with the **ASP.NET and web development** workload, the .NET 10 SDK, and SQL Server LocalDB (installed with Visual Studio). Docker Desktop is only needed for the Docker run.

1. Clone the repository and switch to the Part 3 branch:
   ```
   git clone https://github.com/Benedict-Letebele1/PROG6212P1.git
   cd PROG6212P1
   git checkout part-3
   ```
2. Open `RaceDay.sln` in Visual Studio.
3. In the toolbar's start drop-down, choose **API + Web (Part 3)** (from `RaceDay.slnLaunch`) and press **F5**.
   If the profile isn't listed: right-click the solution > **Configure Startup Projects** > **Multiple startup projects** > set `RaceDayApi` and `RaceDayWeb` to **Start**.
4. The web app opens at **http://localhost:5090**. Swagger is at **http://localhost:5088/swagger**.
5. The first run creates `RaceDayDB` in LocalDB, applies the migrations and loads the demo data.

The API connection string is in `RaceDayApi/appsettings.json`. To use another database without editing that file, put the connection string in `RaceDayApi/appsettings.Local.json` (git-ignored) or an environment variable:

```json
{
  "ConnectionStrings": {
    "RaceDayDb": "Server=(localdb)\\MSSQLLocalDB;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

### Run from the command line

```
dotnet tool restore
dotnet run --project RaceDayApi --launch-profile http     # terminal 1: API on http://localhost:5088
dotnet run --project RaceDayWeb --launch-profile http     # terminal 2: web app on http://localhost:5090
```

### Demo accounts

Every demo account uses the password `Password123!`.

| Role | Email |
|---|---|
| Organiser (Johannesburg Running Events) | events@joburgrunning.co.za |
| Organiser (Cape Town Cycling Tours) | info@capetowncycling.com |
| Participant | thomas.n@email.com, priya.s@email.com, kyle.b@email.com, zanele.m@email.com |

Thomas has finished races with results, and a confirmed entry for an upcoming 2027 event. Priya has a pending entry that the Cape Town organiser can confirm.

## Docker

The MVC app runs in a container. The `Dockerfile` in the repository root is a multi-stage build: the .NET 10 SDK image restores (cached on its own layer) and publishes a Release build, and the smaller ASP.NET Core runtime image runs it on port 8080 as a non-root user.

1. Start Docker Desktop, then start the API (Visual Studio, or `dotnet run --project RaceDayApi --launch-profile http`).
2. Build the image once (and again after code changes):
   ```
   docker build -t raceday-web .
   ```
3. Start the web app with one command:
   ```
   docker run -d -p 8080:8080 --name raceday-web raceday-web
   ```
4. Open **http://localhost:8080**. It behaves the same as the app run from Visual Studio.

Stop and remove it with `docker rm -f raceday-web`.

Inside the container the app reaches the API at `http://host.docker.internal:5088` (`Api__BaseUrl`). The browser loads API-hosted images from `http://localhost:5088` (`Api__PublicUrl`). Both can be changed with `-e`, for example `docker run -d -p 8080:8080 -e Api__BaseUrl=http://my-api:5088 --name raceday-web raceday-web`.

## Azure Blob Storage

Images go through the API, never straight from the MVC app to Azure:

1. The organiser picks a banner on the event form (or a participant picks a profile picture).
2. The MVC app streams the file to `POST /api/events/{eventId}/upload-banner` (or `POST /api/users/profile/picture`).
3. The API checks the size and type, and `AzureBlobImageStorage` uploads it to the `raceday-images` container (`banners/` or `profile-pictures/`) with the right content type.
4. The API saves the blob URL in `Events.BannerImageURL` (or `Participants.ProfilePictureURL`).
5. The MVC pages show the image straight from that Azure URL.

When no Azure connection string is set, the API uses `LocalImageStorage` and keeps images under `RaceDayApi/wwwroot/uploads`, so the project still runs on any machine. Both classes implement `IImageStorage`, and `Program.cs` chooses one at startup.

To connect your own storage account:

1. In the Azure portal create a **Storage account** (Standard, LRS is enough).
2. In the account go to **Settings > Configuration**, set **Allow Blob anonymous access** to **Enabled** and save. The container is created with blob-level read access, so browsers can load an image from its URL but cannot list the container.
3. Go to **Security + networking > Access keys** and copy a **Connection string**.
4. Create `RaceDayApi/appsettings.Local.json` (git-ignored, so the key is never committed):
   ```json
   {
     "AzureStorage": {
       "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
     }
   }
   ```
5. Restart the API. The `raceday-images` container is created on the first upload.

## Pages

| Page | URL | Who | API calls |
|---|---|---|---|
| Home (events and filters) | `/` | Everyone | `GET /api/events` |
| Event details and Enter event | `/Events/Details/{id}` | Everyone (enter: Participant) | `GET /api/events/{id}`, `POST /api/events/{id}/enrol` |
| Register, Log in, Log out | `/Account/...` | Everyone | `POST /api/auth/register`, `/login`, `/logout` |
| My enrolments | `/Participant/MyEvents` | Participant | `GET /api/enrolments` |
| My results | `/Participant/MyResults` | Participant | `GET /api/results/me` |
| Profile and picture | `/Profile` | Logged in (picture: Participant) | `GET/PUT /api/users/profile`, `POST /api/users/profile/picture` |
| Dashboard | `/Organiser/Dashboard` | Organiser | `GET /api/events/mine` |
| My events, create, edit, delete | `/ManageEvents` | Organiser | `POST/PUT/DELETE /api/events`, `POST /api/events/{id}/upload-banner` |
| Categories | `/ManageEvents/{id}/Categories` | Organiser (owner) | `POST /api/events/{id}/categories`, `PUT/DELETE /api/categories/{id}` |
| Enrolments | `/ManageEvents/{id}/Enrolments` | Organiser (owner) | `GET /api/events/{id}/enrolments`, `PUT /api/enrolments/{id}/status` |
| Results entry | `/ManageEvents/{id}/Results` | Organiser (owner) | `GET/POST /api/events/{id}/results`, `PUT /api/results/{id}` |

### MVC design notes

- **Sessions.** On login the API creates its own server-side session and returns the `RaceDayApi.Session` cookie. The MVC app keeps that value in its own session (`RaceDayWeb.Session`) together with the user ID, name and role, and `ApiSessionHandler` attaches it to every API call made for that user. The API therefore still enforces every role rule from Part 2.
- **Role navigation.** `[RequireRole]` sends guests to the login page with a return URL, and sends the wrong role to Access Denied. The menu in `_Layout.cshtml` changes with the role.
- **Graceful errors.** Forms show the API's messages next to the fields. `ApiErrorFilter` turns an expired API session into a fresh login, a 403 into Access Denied, a 404 into a Not Found page, and an unreachable API into a "taking a breather" page. Unknown URLs show the Not Found page. No stack trace or raw error reaches the user.
- **UI.** Bootstrap 5 with one RaceDay theme in `wwwroot/css/site.css`: asphalt and race-orange colours, Barlow Condensed headings and Inter body text. Enrolment statuses share one `_StatusBadge` partial: green Confirmed, amber Pending, red Cancelled.
- **No replicated logic.** The view models only check that a form is filled in (required fields, lengths, email format). Rules such as age ranges, duplicate entries, ownership and delete protection stay in the API.

## Endpoints

Every endpoint is listed in Swagger with its purpose, request body and responses. The table below matches the approved plan in `docs/endpoint-plan.pdf`, except for the documented deviations.

| Method | Route | Access | Purpose |
|---|---|---|---|
| POST | /api/auth/register | Public | Create an Organiser or Participant account |
| POST | /api/auth/login | Public | Log in and start a session |
| POST | /api/auth/logout | Logged in | End the session |
| GET | /api/users/profile | Logged in | View own profile |
| PUT | /api/users/profile | Logged in | Update own profile |
| POST | /api/users/profile/picture | Participant | **Part 3.** Upload a profile picture (stored in Azure Blob Storage) |
| GET | /api/events | Public | List events, filtered by type, date and location |
| GET | /api/events/mine | Organiser | **Part 3.** Own events with category, enrolment and result counts (dashboard) |
| GET | /api/events/{id} | Public | One event with its categories |
| POST | /api/events | Organiser | Create an event |
| PUT | /api/events/{id} | Organiser (owner) | Update an event |
| DELETE | /api/events/{id} | Organiser (owner) | Delete an event |
| POST | /api/events/{eventId}/upload-banner | Organiser (owner) | Upload a banner image (stored in Azure Blob Storage) |
| GET | /api/events/{eventId}/categories | Public | List the categories of an event |
| POST | /api/events/{eventId}/categories | Organiser (owner) | Add a category |
| PUT | /api/categories/{id} | Organiser (owner) | Update a category |
| DELETE | /api/categories/{id} | Organiser (owner) | Delete a category |
| GET | /api/enrolments | Logged in | Organiser: enrolments in own events. Participant: own enrolments |
| GET | /api/events/{eventId}/enrolments | Organiser (owner) | Enrolments for one event |
| POST | /api/events/{eventId}/enrol | Participant | Enrol with a category of the event |
| PUT | /api/enrolments/{id}/status | Organiser (owner) | Set status to Pending, Confirmed or Cancelled |
| GET | /api/results/me | Participant | Own results |
| GET | /api/events/{eventId}/results | Organiser (owner) | Results for one event |
| POST | /api/events/{eventId}/results | Organiser (owner) | Record the finish time and position of an enrolment |
| PUT | /api/results/{id} | Organiser (owner) | Change a recorded result |

### Deviations from the Part 1 plan

These are deliberate. Each one is listed so the marker can see it.

1. **Organiser registration also takes `contactNumber`.** The plan lists only organisation name for organisers, but `Organisers.ContactNumber` is NOT NULL in the schema.
2. **Enrolment checks the age range.** `POST /api/events/{eventId}/enrol` returns 400 when the participant's age on the event date is outside the category's range.
3. **Deleting an event or a category that has enrolments returns 409 Conflict.** The 409 stops enrolment records from being lost.
4. **Duplicate enrolments and duplicate results return 400.** Both use 400 so the status codes stay within the ones the plan lists.
5. **`GET /api/events/{id}` includes the categories,** so the event page shows its categories in one call.
6. **Login returns the user's identity and role in the body.** The session cookie is `RaceDayApi.Session`.
7. **Banner images are stored in Azure Blob Storage (Part 3)** through `IImageStorage`. Without a connection string they fall back to `wwwroot/uploads`.
8. **Profile pictures are uploaded with `POST /api/users/profile/picture` (Part 3).** The plan had no endpoint for them. It is Participant only, because `ProfilePictureURL` belongs to the Participants table.
9. **The API builds its own schema from migrations.** Run `docs/RaceDay-Schema.sql` only against a separate database.
10. **`GET /api/events/mine` was added (Part 3).** `GET /api/enrolments` only shows events that already have enrolments, and the dashboard must list every event the organiser created.
11. **Results include `TotalFinishers` (Part 3),** so My results can show "47th out of 312". Organisers only capture results for RaceDay entrants, so it is the larger of the number of results and the highest position recorded.
12. **Three upcoming 2027 demo events are seeded (Part 3)** by `UpcomingEventsSeeder`, because every event in the Part 1 script has already taken place. The Part 1 rows are unchanged.

## Running the tests

```
dotnet test RaceDay.sln
```

| Project | Tests | What they cover |
|---|---|---|
| `RaceDayApi.Tests` | 67 | Registration and login, sessions, event management, role access, enrolments, results and finisher counts, the dashboard endpoint, profile picture uploads, image storage selection, seed data and Swagger |
| `RaceDayWeb.Tests` | 20 | Guests redirected to login, wrong role sent to Access Denied, a menu per role, the API cookie passed on, logout, colour-coded statuses, "47th out of 312", Azure image URLs, API messages on enrol, an expired session, the API being down, and the Not Found page |

Neither project needs SQL Server, LocalDB, Azure or a running API.

## CI/CD

`.github/workflows/api-build-test.yml` runs on every push:

1. Checks that the Part 1, 2 and 3 files are present.
2. Restores the tools and packages, and builds the API and the MVC app.
3. Runs the API tests and the MVC tests.
4. Checks that the migrations match the model.
5. In a second job, builds the Docker image and starts the container to check that the web app answers.

Screenshot of a successful Part 3 build:

![Part 3 green CI build](docs/screenshots/ci-green-build-part3.png)

## Video Presentation

- Part 2 (API, Swagger, tests): [YouTube link - to be added]
- **Part 3 (MVC pages, Blob Storage, Docker): [YouTube link - to be added before submission]**

What the Part 3 video covers:
- The full running application for both roles
- The MVC structure, sessions and role-based navigation
- Azure Blob Storage: a banner going from the MVC form to the API to Azure, and the URL in the database
- The Dockerfile, the `docker run` command and the app running in the container

## AI Use Disclosure

AI tools (Claude) were used to help draft the code, the tests and this README. The endpoint plan, the design choices and the seed-data corrections come from my own Part 1 work, and I checked and ran the code myself.

## Student Declaration

I declare that this work is my own original work and all sources have been properly referenced.

Name: Benedict Letebele
Student Number: ST10483847
Date: 2026/11/09
=======
# PROG6212P3
>>>>>>> 7d5ffdbcd0431466c10a2127a1569b759e6388ff
