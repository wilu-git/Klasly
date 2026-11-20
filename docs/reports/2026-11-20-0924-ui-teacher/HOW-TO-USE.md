# How to use the redesigned teacher pages

## Prerequisites
API and website running; log in as teacher `rosa.villanueva@klasly.test` (password in `docs/TEST-ACCOUNTS.md`).

## Steps
1. **My classes:** click the class name (for example "ELECT3 – BSCpE 3-A") to open it. On a phone the term, schedule and room show under the name.
2. **Class page with a session running:** a pale-teal "Session in progress" bar is at the top. Press **Show QR** to go back to the QR. To start a different session anyway, press **Start another session**.
3. **Start a session:** fill "Ends at" and "Late after", press **Start session** (it says "Starting…" while it works). If something is wrong, the message appears inside the Start form only.
4. **Session page:** the big number is how many students have checked in. When the end time has passed but the session is still open, press **End session** to close it, then correct results.
5. **Correct a result:** pick a new status in the Correct column and press **Save**. The page reopens at that student's row.
6. **Grade sheet:** type scores. The bar at the bottom of the screen shows "N of M graded" and the **Save grades** button. If you try to leave before saving, the browser asks first.

## Example
Class page on a laptop: breadcrumb, class title, the teal "Session in progress" strip with Show QR, the Assessments table, the Add an assessment form, then the Sessions table.

## Troubleshooting
- **No "Session in progress" bar** although a session is open: its end time has passed; open it from the Sessions table and press End session.
- **The page doesn't jump to the corrected row**: the class is small enough that the row is already on screen.
- **The leave warning appears after saving**: it shouldn't; a successful save reloads the page. If the save failed (red message), the scores are still unsaved.
