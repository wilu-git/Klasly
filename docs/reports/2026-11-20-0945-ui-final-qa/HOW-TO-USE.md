# How to try the redesigned Klasly website

## Prerequisites
SQL Server with the Klasly database, the API (`https://localhost:7256`) and the website (`https://localhost:7144`) running, all from branch `ui-redesign`. Accounts: `docs/TEST-ACCOUNTS.md`.

## Steps
1. Open the website: the new login panel with the teal K + check logo.
2. **Teacher** (`rosa.villanueva@klasly.test`): My classes → class page. An open session shows a teal "Session in progress" strip with **Show QR**. The session page has a big "checked in" number; the grade sheet has a Save bar that stays on screen.
3. **Parent** (`marites.mendoza@klasly.test`): the overview starts with one sentence saying whether every class is on track; classes needing attention come first.
4. **Admin** (`admin@klasly.test`): Terms, Subjects, Accounts, Classes and Failed emails share one table and button style; forms have breadcrumbs.
5. Try a phone-size window: menus fold behind **Menu**, and tables keep their key columns.

## Example
Teacher class page on a phone: logo symbol and Menu button, breadcrumb, class title, the teal live strip, then assessments with "20 pts · 30%" under each title.

## Troubleshooting
- **Old look after switching branches**: rebuild and hard-refresh (Ctrl+F5).
- **"Klasly can't reach the server right now."**: start the API.
- **Android icon unchanged**: the app must be rebuilt with Android Studio (`./gradlew assembleDebug`).
