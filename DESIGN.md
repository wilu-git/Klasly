# Klasly — Design

> Design file for the Klasly website (`src/Klasly.Web`), read by `/impeccable` and by anyone changing a view.
> Decisions come from `SPEC.md` §A–B (UI redesign, approved 2026-11-20). Implementation: `wwwroot/css/site.css` (tokens at the top).

## Direction

**Mode: Operate.** Klasly is a working tool for teachers in front of a class, admins at a desk and parents on a phone. It should feel dependable, organized and calm, not like a children's app or a marketing site.

- Refine Bootstrap 5.3.3; don't fight it. The Teacher area is the house-style reference.
- Restrained borders, consistent spacing, modest radii, white content surfaces on a pale grey canvas.
- **Never:** gradients, glass, hero sections, card grids, decorative dashboards, icon decoration, oversized rounded components, shadows on panels, graduation caps.
- One primary (teal) action per page region. Everything else is quieter.

## Brand

- **Mark:** a geometric K whose lower leg is a checkmark (K = Klasly, check = attendance verified). Rounded caps, one stroke weight. Files: `wwwroot/img/klasly-mark.svg` (symbol, favicon), `klasly-logo.svg` (symbol + wordmark), `klasly-logo-reverse.svg` (white). Concept art: `docs/brand/`.
- **In the navbar and login** the mark is inline SVG (`currentColor` = teal) plus the word "Klasly" as HTML text, Inter 600, navy. Symbol only below 576 px.
- **Android:** white mark on teal adaptive icon.

## Colour

| Token | Value | Use |
|---|---|---|
| `--k-teal` (`--bs-primary`) | `#126B64` | Primary buttons, links, logo, active nav text, focus ring |
| `--k-teal-hover` | `#0E5650` | Hover / pressed primary |
| `--k-teal-subtle` | `#E8F3F1` | Selected nav item, selected pill, live-session strip |
| `--k-navy` (headings) | `#17324D` | Headings, strong text, nav links |
| `--k-canvas` (`--bs-body-bg`) | `#F5F7F8` | Page background |
| `--k-surface` | `#FFFFFF` | Navbar, forms, tables, panels, cards |
| `--k-text` | `#263746` | Body text |
| `--k-text-muted` | `#5A6A80` | Meta lines, help text, empty states (5.1:1 on canvas) |
| `--k-border` | `#DCE3E8` | Dividers, table rules, panel edges |
| `--k-border-control` | `#8696A7` | Input, select, checkbox and secondary-button outlines (3:1) |

Statuses use Bootstrap's success / warning / danger / info / secondary **only for statuses**. **No teal badges, no teal section backgrounds.** No hex colours in `.cshtml` files.

## Status badges

One partial, `Views/Shared/_StatusBadge.cshtml`, model `(Text, Variant)`. Calm subtle badge by default; only **At risk** is filled (`danger-strong`).

| Variant | Statuses |
|---|---|
| success | Active, Present, Open, On track |
| warning | Late, weights under 100 % |
| danger | Absent, Cancelled · `danger-strong`: At risk |
| info | Excused, Scheduled |
| secondary | Inactive, Dropped, Ended, Closed, Finalized, Not computed yet, Not yet |

## Type

Inter (bundled `wwwroot/fonts/InterVariable.woff2`, OFL) → `system-ui` fallback. Body **16 px at every width**. Tabular numerals in tables and `.num`.

| Role | Markup | Size / weight |
|---|---|---|
| Page title | `h1.h3` | 28 px below 992 px, 32 px from 992 px · 600 navy |
| Section | `h2.h5` | 20 px · 600 |
| Subsection | `h3.h6` | 16 px · 600 |
| Table text / headers | `.table` / `thead th` | 15 px / 14 px 600 muted |
| Labels | `.form-label` | 15 px · 500 |
| Help, meta | `.small`, `.form-text` | 14 px muted |
| Buttons | `.btn` / `.btn-sm` | 15 px / 14 px · 600 |

## Space and layout

- Bootstrap spacers only (4, 8, 16, 24, 48 px). Container max 1200 px, 16 px side gutter.
- Navbar → content 24 px; breadcrumb → title `mb-2`; header → content `mb-4`; between sections `mt-5`; field groups `mb-3`.
- Panels (`.k-panel`, `.card`): white, 1 px `--k-border`, 8 px radius, `p-3` / `p-4` from 768 px, no shadow.
- Radii: inputs and buttons 6 px, panels 8 px, badges 4 px. Only nav pills are pill-shaped.
- Breakpoints: phone < 576 (single column, collapsed nav, secondary columns move under the name), tablet 576–991 (Admin nav still collapsed), desktop ≥ 992.
- Sideways scrolling only inside `.table-responsive`, and only for the failed-emails table and the grade sheet.

## Components

| Component | Rule |
|---|---|
| Primary button | `btn-primary`, one per page region |
| Secondary | `btn-outline-secondary` |
| Destructive in a row | `btn btn-sm btn-link link-danger`, last in the row, with `data-confirm` |
| Destructive on a page | `btn-outline-danger` (End session) with `data-confirm` |
| Inline add (Enroll, Link) | `btn-outline-primary` in `form.row.g-2.align-items-end` |
| Row destination | The record's name is the link |
| Repeated row actions | `Edit<span class="visually-hidden"> {name}</span>` |
| Forms | Visible labels, `.form-text` help, `ModelOnly` summary at top, Save + Cancel left aligned |
| Tables | `.table-responsive > table.table.align-middle` (white panel), `th scope="col"`, actions `text-end text-nowrap` |
| Breadcrumbs | Every page below a list: `List › Record › Sub-page`, last item `aria-current="page"` |
| Flash alerts | Inside `main`; success `role="status"` + dismiss; error `role="alert"`, not dismissible |
| Empty state | One muted sentence naming the next step, with its link/button |
| Pending submit | `site.js`: a confirmed, valid POST disables its button and shows `data-pending-text` (default "Saving…") |
| Unsaved guard | `<form data-warn-unsaved>` warns before leaving with typed changes |
| Message page | Heading, one sentence, one primary action, optional small "Reference" |

## Accessibility

- Focus: `outline: 2px solid` teal, offset 2 px, on every focusable element (inputs: flush outline + teal border).
- Skip link "Skip to content" → `main#main`. Navbar toggler labelled "Menu" with `aria-controls` / `aria-expanded`.
- Active nav item has `aria-current="page"` and pale-teal background, not colour alone. Statuses always have text.
- Contrast measured: body 11.4:1 on canvas, muted 5.1:1 on canvas, links and primary buttons 6.3:1, active nav 5.6:1, input borders 3.0:1.
