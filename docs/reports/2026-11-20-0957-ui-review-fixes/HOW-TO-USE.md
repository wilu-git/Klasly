# What changed for users after the review

## Prerequisites
API and website running from `ui-redesign`.

## Steps
1. **Grade sheet:** if saving fails (a red message next to a score), your typed scores are still on the page and still unsaved. Trying to leave now asks first, so you don't lose them.
2. **Admin › Classes:** choosing a term in the Term box reloads the list straight away, in every browser.

## Example
A teacher types 999 for a 100-point quiz in a browser without validation, presses Save, sees "Score must be between 0 and 100.", and gets a "Leave site?" prompt if they click away before fixing it.

## Troubleshooting
- **No leave prompt after fixing and saving**: correct; a successful save reloads the page with nothing unsaved.
- **Term box does nothing**: JavaScript is off; use the **Show** button that appears then.
