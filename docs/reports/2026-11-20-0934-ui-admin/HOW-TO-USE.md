# How to use the redesigned admin pages

## Prerequisites
API and website running; log in as `admin@klasly.test` (password in `docs/TEST-ACCOUNTS.md`).

## Steps
1. **Admin home** lists Terms, Subjects, Accounts, Classes and Failed emails.
2. **Terms:** every term shows Active or Inactive. **Make active** asks first and says which term it replaces.
3. **Accounts:** press **New account** and pick a role (with a role filter on, the button says "New teacher", "New parent", …). On a phone the email and ID show under the name.
4. **Edit an account:** fields are grouped into Login, details and Status. For a parent, the **Linked students** panel below saves each Link/Unlink straight away; if you typed changes above and try to leave, the browser warns you.
5. **Classes:** pick a term and the list updates. Click a class name to see its students; **Enroll a student** is at the top.
6. **Failed emails:** alerts the email service couldn't deliver; follow the link to the parent accounts to fix an address.

## Example
Terms list: "1st Sem 2026-27" in bold with a green "Active" badge, "2nd Sem 2026-27" with a grey "Inactive" badge and a **Make active** button.

## Troubleshooting
- **The term filter doesn't change the list**: JavaScript is off; use the **Show** button that appears then.
- **"The previous teacher's account is inactive"** on Edit class: pick a new teacher before saving.
- **Leave warning on an account page**: you typed something without pressing Save.
