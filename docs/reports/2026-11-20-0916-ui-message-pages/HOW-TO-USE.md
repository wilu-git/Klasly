# How to see the new message pages

## Prerequisites
API (`https://localhost:7256`) and website (`https://localhost:7144`) running; accounts from `docs/TEST-ACCOUNTS.md`.

## Steps
1. **Not found:** log in as the admin and open `https://localhost:7144/Admin/Classes/Details/999999`. You see "We couldn't find that page." with the normal menu and a "Go to my home page" button.
2. **Access denied:** log in as a teacher and open `/Admin`. You see "You can't open that page." and a button back to your own home page.
3. **Access denied with a reason:** log in as parent Marites Mendoza and open `/Parent/Children/Details/3` (not her child). The page says "You can only view your own children".
4. **Server unavailable:** stop the API and reload any page. You see "Klasly can't reach the server right now." Start the API again and press **Try again**.

## Example
A message page is one heading, one sentence, one teal button, and (for unexpected errors) a small "Reference" code to give the admin.

## Troubleshooting
- **You see a developer error page with a stack trace instead of "Something went wrong"**: you're running in Development; the friendly Error page only shows on the deployed site.
- **The browser asks to resend a form after "Try again"**: the failed request was a form post; resending is safe once the API is back.
