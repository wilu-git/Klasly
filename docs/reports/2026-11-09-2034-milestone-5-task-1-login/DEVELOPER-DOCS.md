# Milestone 5 · Task 1 — Web MVC login and logout

## Summary
Asked: `/build` the next task of Milestone 5 (Task 1 in `tasks/todo.md`).
Done: the website now logs users in through `POST /api/auth/login`. The returned JWT is stored **inside** the encrypted ASP.NET Core auth cookie (`Klasly.Auth`, HttpOnly + Secure + SameSite=Lax, a session cookie that expires with the JWT). Admin is sent to `/Admin` (that page arrives in Task 2), teachers and parents see a "coming soon" welcome page, and students are refused with "Students use the Klasly mobile app." The template Privacy page is removed; the layout shows the user's name and a Log out button.

## Projects touched
WEB MVC only. No API, EF Core, database, Worker or Android changes.

## Files changed
| Path | Change | Why |
|---|---|---|
| `src/Klasly.Web/Program.cs` | modified | Cookie authentication, typed `HttpClient` for the auth repository, `UseAuthentication()` |
| `src/Klasly.Web/appsettings.Development.json` | modified | `Api:BaseUrl` = `https://localhost:7256/` (not a secret) |
| `src/Klasly.Web/Models/Api/LoginDtos.cs` | added | Copies of the API's login request/response shapes (Web may not reference Klasly.Core) |
| `src/Klasly.Web/Models/Api/ProblemDetailsDto.cs` | added | The parts of an API error body the site shows (message + field errors) |
| `src/Klasly.Web/Infrastructure/ApiException.cs` | added | A failed API call: status code, message, field errors, `IsFormError` (400/409) |
| `src/Klasly.Web/Infrastructure/ApiResponse.cs` | added | One helper: success → read JSON, failure → throw `ApiException` with the API's message |
| `src/Klasly.Web/Repositories/AuthRepository.cs` + `Interfaces/IAuthRepository.cs` | added | Calls `POST api/auth/login`; returns `null` on 401 (wrong password) |
| `src/Klasly.Web/Services/AuthService.cs` + `Interfaces/IAuthService.cs` | added | Turns a login into cookie claims + stored JWT; blocks the Student role |
| `src/Klasly.Web/Controllers/AccountController.cs` | added | Login GET/POST, Logout POST, AccessDenied |
| `src/Klasly.Web/Controllers/HomeController.cs` | modified | `[Authorize]` Index: Admin → `/Admin`, others → welcome; Privacy action removed |
| `src/Klasly.Web/ViewModels/LoginViewModel.cs` | added | Login form fields with validation messages |
| `src/Klasly.Web/Views/Account/Login.cshtml`, `AccessDenied.cshtml` | added | Login form; "no access" page |
| `src/Klasly.Web/Views/Shared/_Alerts.cshtml` | added | Shows TempData `Success` / `Error` messages on every page |
| `src/Klasly.Web/Views/Shared/_Layout.cshtml` | modified | Klasly brand, user name + Log out form, alerts partial, no Privacy links |
| `src/Klasly.Web/Views/Home/Index.cshtml` | modified | "Hi, {name}. Your {role} pages are coming soon." |
| `src/Klasly.Web/Views/_ViewImports.cshtml` | modified | `ViewModels` and `System.Security.Claims` usings |
| `src/Klasly.Web/Views/Home/Privacy.cshtml` | deleted | Template leftover |

## Key code
Storing the JWT inside the cookie ticket (`Services/AuthService.cs`):
```csharp
var properties = new AuthenticationProperties { ExpiresUtc = login.ExpiresAtUtc, IsPersistent = false };
properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = login.Token }]);
return new LoginResult(new ClaimsPrincipal(identity), properties, null);
```
A wrong password is a form result, not an exception (`Repositories/AuthRepository.cs`):
```csharp
if (response.StatusCode == HttpStatusCode.Unauthorized) return null;
return await ApiResponse.ReadAsync<LoginResponse>(response, ct);
```
Open-redirect guard (`Controllers/AccountController.cs`):
```csharp
return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl) : RedirectToAction("Index", "Home");
```

## Database changes
None.

## How it was verified
- `dotnet build Klasly.slnx` → **0 warnings, 0 errors**.
- No test project (SPEC assumption 12), so verified by hand with the API + website running, in the browser pane and with curl:
  - Anonymous `/` → `/Account/Login?ReturnUrl=%2F`.
  - Wrong password → "Invalid email or password.", email kept, password cleared.
  - Admin login → redirect to `/Admin` (404 until Task 2, expected); "Klasly Administrator" in the top bar.
  - Teacher (Rosa Villanueva) and parent (Marites Mendoza) → welcome page naming their role.
  - Student (Aira Mendoza) → "Students use the Klasly mobile app.", **no** auth cookie set.
  - `Set-Cookie: Klasly.Auth=…; path=/; secure; samesite=lax; httponly`, no `expires` (session cookie), about 1 KB.
  - `document.cookie` in the page is empty → page JavaScript can't read the JWT.
  - POST without the anti-forgery token → `400`. `ReturnUrl=https://evil.example/` → redirected to `/` instead.
  - Log out → back to login; `/` asks to log in again.
- **Caveat:** checked over `http://localhost` (API 5201, website 5139, `Api__BaseUrl` overridden by an environment variable) because the dev HTTPS certificate on this PC is not trusted yet. The https profiles need `dotnet dev-certs https --trust` first.

## Known limits / TODO
- `/Admin` returns 404 until Task 2.
- If the API is down during login, the developer error page shows. The friendly "can't reach the server" page and the 401 "session ended" handling come with `ApiExceptionFilter` in Task 2.
- `/impeccable` runs on all views in Task 9.
