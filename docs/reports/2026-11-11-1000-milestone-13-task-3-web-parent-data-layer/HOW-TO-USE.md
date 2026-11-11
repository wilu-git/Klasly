# How to use — Milestone 13 Task 3 (Web data layer for parents)

There is nothing new to click yet. This task connects the website to the parent endpoints so the next task can show the pages.

## Prerequisites
- .NET 10 SDK.

## Step by step
1. Open a terminal in the `Klasly` folder.
2. Run `dotnet build src/Klasly.Web`. It should say **Build succeeded**.

## Example
When a parent opens a child's page (Task 4), the website calls:
- `GET https://localhost:7256/api/parent/children/1/overview`, with the parent's login token attached automatically;
- `GET https://localhost:7256/api/parent/children`, for the "switch child" list.

## Troubleshooting
- **Build error "LinkedStudentResponse is ambiguous"**: there must be only one `LinkedStudentResponse`, in `Models/Api/AccountDtos.cs`. Don't add a second copy.
- **Pages later show "Klasly is unavailable"**: the Web API isn't running. Start it with `dotnet run --project src/Klasly.API --launch-profile https`.
