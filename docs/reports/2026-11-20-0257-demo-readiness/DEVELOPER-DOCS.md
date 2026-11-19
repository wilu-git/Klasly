# Demo readiness: Reset phone button, lan profile fix, VPS deploy files

## Summary
Asked: list everything needed for tomorrow's demo (attendance, at-risk indicators, admin controls, the milestone features), with the API, Worker and MVC hosted on an Ubuntu VPS (domain + HTTPS) and the Android app shipped as an APK. Firebase and Brevo are set up too.
Done in code:
- the `lan` launch profile also listens on `https://localhost:7256`, so the local MVC site keeps working during a phone rehearsal;
- the **Admin › Accounts › Reset phone** button that the M14/M15 docs already pointed to (the API endpoint existed, the Web side didn't);
- a `deploy/` folder with systemd units, the nginx site and an env-file template.
The demo checklist itself is in `HOW-TO-USE.md`.

## Projects touched
WEB MVC, WEB API (launch profile only). Deploy config for WEB API, WEB MVC and CONSOLE WORKER. No EF Core, MSSQL or Android change.

## Files changed
| Path | Change | Why |
|---|---|---|
| `src/Klasly.API/Properties/launchSettings.json` | modified | `lan` profile also binds `https://localhost:7256`, the MVC site's `Api:BaseUrl` |
| `src/Klasly.Web/Repositories/Interfaces/IAccountRepository.cs` | modified | `ResetDeviceAsync` |
| `src/Klasly.Web/Repositories/AccountRepository.cs` | modified | `DELETE api/accounts/{id}/device` |
| `src/Klasly.Web/Services/Interfaces/IAccountService.cs` | modified | `ResetDeviceAsync` |
| `src/Klasly.Web/Services/AccountService.cs` | modified | pass-through |
| `src/Klasly.Web/Areas/Admin/Controllers/AccountsController.cs` | modified | `ResetDevice` POST action |
| `src/Klasly.Web/Areas/Admin/Views/Accounts/Index.cshtml` | modified | "Reset phone" button on Student rows, with a confirm prompt |
| `deploy/klasly-api.service` | added | systemd unit for the API on `127.0.0.1:5000` |
| `deploy/klasly-web.service` | added | systemd unit for the MVC site on `127.0.0.1:5001` |
| `deploy/klasly-worker.service` | added | systemd unit for the Hangfire Worker |
| `deploy/nginx-klasly.conf` | added | one domain: `/api/` → API, everything else → MVC; certbot adds HTTPS |
| `deploy/klasly.env.example` | added | every production setting as an environment variable, with placeholders |
| `.gitignore` | modified | ignores a filled-in `deploy/klasly.env` and Android `*.jks` / `*.keystore` |

## Key code
```csharp
// Areas/Admin/Controllers/AccountsController.cs
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> ResetDevice(int id, string? role, CancellationToken ct)
{
    await accounts.ResetDeviceAsync(id, ct);
    TempData["Success"] = "Phone reset. The student's next login on the app registers their new phone.";
    return RedirectToAction(nameof(Index), new { role });
}
```
- There's no try/catch. The API's `ResetDeviceAsync` only throws 404 for an unknown account, and that already goes through `ApiExceptionFilter`. Resetting a student with no phone is a no-op.
- Deploy: secrets come only from `/etc/klasly/klasly.env` (`chmod 600`). `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` lets the apps see nginx's HTTPS without a code change. The services run as user `klasly` (with a home dir, so the MVC site's data-protection keys survive restarts).

## Database changes
None.

## How it was verified
- `dotnet build Klasly.slnx`: 0 warnings, 0 errors.
- `dotnet test Klasly.slnx`: 162/162 passed.
- Browser (local `https` profiles, as the seeded admin):
  - Accounts › Student shows Reset phone on all 7 student rows, and the confirm text names the student;
  - clicking it for Aira → green "Phone reset…" message, and the Student filter is kept;
  - at 375 px the page has no sideways scroll (the table scrolls inside its own box, as before).
- `graphify update .` run.

## Known limits / TODO
- The deploy files are not tested on a real VPS (none is set up yet). Part B of HOW-TO-USE lists the steps.
- The env file is shared by all three services, so the MVC process can see the connection string even though it never uses it. If that matters, split it into per-app files.
- No Android change, so `./gradlew assembleDebug` wasn't run. This PC has no JDK anyway.
- The test accounts share a password that's in the repo docs, and the site will be public. Stop the services or deactivate the test accounts after the demo.
