# How to use: logging in to the Klasly website

## Prerequisites
- SQL Server LocalDB with the `Klasly` database (Milestone 2 migration + seed).
- A trusted dev HTTPS certificate (one time, on each PC): `dotnet dev-certs https --trust`
- The Web API running: `dotnet run --project src/Klasly.API --launch-profile https`
- The website running: `dotnet run --project src/Klasly.Web --launch-profile https`

## Step by step
1. Open `https://localhost:7144`. You are sent to **Log in to Klasly**.
2. Type an email and password. The seed accounts all use the test password `Klasly@123`:
   - Admin: `admin@klasly.test`
   - Teacher: `rosa.villanueva@klasly.test`
   - Parent: `marites.mendoza@klasly.test`
3. Click **Log in**.
   - Admin goes to the Admin portal (`/Admin`, built in the next task).
   - A teacher or parent sees "Hi, {name}. Your {role} pages are coming soon."
4. Your name shows at the top right. Click **Log out** to end the session.

## Example (screen description)
A centred form titled "Log in to Klasly" with Email and Password boxes and a blue **Log in** button. A wrong password shows a red box "Invalid email or password." and keeps the email you typed.

## Troubleshooting
| Problem | Fix |
|---|---|
| Error page mentioning SSL or "the remote certificate is invalid" right after clicking Log in | The dev certificate isn't trusted. Run `dotnet dev-certs https --trust`, then restart both apps. |
| A student account says "Students use the Klasly mobile app." | Expected: students use the Android app, not the website. |
| You are logged out after about an hour | Expected: the login lasts as long as the API's token (`Jwt:ExpiryMinutes`, 60). Log in again. |
