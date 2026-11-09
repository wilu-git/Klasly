# Milestone 3 — How to use login (Web API)

## Prerequisites
- .NET 10 SDK (pinned by `global.json`).
- SQL Server LocalDB with the `Klasly` database created (Milestone 2):
  `dotnet ef database update -p src/Klasly.Infrastructure -s src/Klasly.API`
- **A JWT key on your machine.** Each teammate does this once. The key is never committed.
  ```bash
  dotnet user-secrets set "Jwt:Key" "<at least 32 random characters>" --project src/Klasly.API
  ```
  Tip: `openssl rand -base64 48` (Git Bash) prints a good random key.

The MVC site, Worker and Android app are **not** needed for this milestone.

## Step by step
1. From the solution folder, start the API:
   ```bash
   dotnet run --project src/Klasly.API --launch-profile https
   ```
2. Open **https://localhost:7256/swagger** (or http://localhost:5201/swagger).
3. Open **POST /api/auth/login** → **Try it out**, and paste:
   ```json
   { "email": "admin@klasly.test", "password": "Klasly@123" }
   ```
   Click **Execute**, then copy the `token` value from the response.
4. Click **Authorize** (top right), paste the token (without the word "Bearer"), click **Authorize**, then **Close**.
5. Open **GET /api/auth/me** → **Try it out** → **Execute**. You should see your own user ID, email, name and role.

You can do the same from Visual Studio or VS Code with `src/Klasly.API/Klasly.API.http`. Run request 1 first, because request 6 reuses its token automatically.

### Test accounts (fictional, all use the password `Klasly@123`)
| Role | Email |
|---|---|
| Admin | `admin@klasly.test` |
| Teacher | `rosa.villanueva@klasly.test`, `mark.delossantos@klasly.test` |
| Student | `aira.mendoza@klasly.test`, `bryan.lacsamana@klasly.test` (+4 more) |
| Parent | `marites.mendoza@klasly.test`, `rodel.dimaano@klasly.test`, `linda.bautista@klasly.test` |

## Example
Request:
```http
POST /api/auth/login
Content-Type: application/json

{ "email": "aira.mendoza@klasly.test", "password": "Klasly@123" }
```
Response `200 OK`:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9....",
  "expiresAtUtc": "2026-11-09T12:01:31Z",
  "userId": 4,
  "fullName": "Aira Mendoza",
  "role": "Student"
}
```
Wrong password → `401`:
```json
{ "title": "Unauthorized", "status": 401, "detail": "Invalid email or password." }
```
The token lasts **60 minutes**. After that, log in again.

## Troubleshooting
| Problem | Fix |
|---|---|
| API stops at startup with *"Jwt:Key is missing or shorter than 32 characters"* | Run the `dotnet user-secrets set "Jwt:Key" ...` command above. The key needs at least 32 characters. |
| Login always returns 401 for the test accounts | Check that the database was created from the Milestone 2 migration (`dotnet ef database update ...`) and that you typed `Klasly@123` exactly (capital K). |
| `/swagger` shows 404 | Swagger only runs in **Development**. Use the `http`/`https` launch profile, which sets `ASPNETCORE_ENVIRONMENT=Development`. |
| `/me` returns 401 in Swagger | The token expired (60 min) or was pasted with extra spaces or quotes. Log in again and paste only the token. |
