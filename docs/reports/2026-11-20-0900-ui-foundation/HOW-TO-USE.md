# How to see the new Klasly look

## Prerequisites
SQL Server with the Klasly database, the Web API (`https://localhost:7256`) and the website (`https://localhost:7144`) running. Log in with an account from `docs/TEST-ACCOUNTS.md`.

## Steps
1. Open the website. The page background is a pale grey, content sits on white panels, and the top bar shows the teal K + check logo with "Klasly".
2. Log in as a teacher. "My classes" is highlighted on every teacher page, including a class, a session and a grade sheet.
3. Make the window narrow (phone size). The menu folds behind the "Menu" button; on very small screens only the logo symbol shows.
4. Log in as the admin and narrow the window to tablet size: the five admin links fold into the menu below 992 px.
5. Press Tab once on any page: a "Skip to content" link appears. Every focused control shows a teal outline.
6. Submit any form (for example Admin › Terms › Edit › Save). The button changes to "Saving…" and can't be clicked twice. If a confirm box is cancelled or a field is invalid, the button stays as it was.
7. After a successful save, the green message has a close (×) button.

## Example
Teacher class page on a laptop: white "Start a session" panel with a teal "Start session" button, assessments table on a white panel, navy headings, 16 px Inter text.

## Troubleshooting
- **Text isn't Inter**: hard-refresh (Ctrl+F5); `site.css` is versioned, but an old cached page can keep the old stylesheet.
- **"Klasly can't reach the server right now"**: the API isn't running or is still starting; start it, wait a few seconds, then retry.
- **A table is wider than the phone screen** on My classes, Accounts or Classes: known; those pages are reworked in the next milestones.
