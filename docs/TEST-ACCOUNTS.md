# Klasly — Test Login Accounts

All accounts are **fictional seed data** (RA 10173) from `src/Klasly.Infrastructure/Data/KlaslySeed.cs`.
`deploy/demo-data.sql` adds no accounts, so the same logins work locally and on the demo site.

**Password for every account:** `Klasly@123`

> Only valid until someone changes a password through Admin › Accounts. Never reuse it for a real account.

## Where each role logs in

| Role    | Web MVC portal | Android app |
| ------- | -------------- | ----------- |
| Admin   | Yes            | No          |
| Teacher | Yes            | No          |
| Parent  | Yes            | Yes         |
| Student | No portal      | Yes         |

A student account binds to the **first phone** it logs in on. Logging in on a second phone gets
"Another device is already registered…" (409). Use a different student per phone/emulator.

## Admin

| Name                 | Email               |
| -------------------- | ------------------- |
| Klasly Administrator | `admin@klasly.test` |

## Teachers

| Name              | Email                          | Teaches                    |
| ----------------- | ------------------------------ | -------------------------- |
| Rosa Villanueva   | `rosa.villanueva@klasly.test`  | ELECT3 — BSCpE 3-A (MWF)   |
| Mark Delos Santos | `mark.delossantos@klasly.test` | IT302 — BSCpE 3-A (TTh)    |

## Students

| Name            | Email                         | Student No. | Classes         |
| --------------- | ----------------------------- | ----------- | --------------- |
| Aira Mendoza    | `aira.mendoza@klasly.test`    | 2026-00001  | ELECT3, IT302   |
| Bryan Lacsamana | `bryan.lacsamana@klasly.test` | 2026-00002  | ELECT3, IT302   |
| Carla Dimaano   | `carla.dimaano@klasly.test`   | 2026-00003  | ELECT3, IT302   |
| Dennis Orpilla  | `dennis.orpilla@klasly.test`  | 2026-00004  | ELECT3          |
| Ellen Bautista  | `ellen.bautista@klasly.test`  | 2026-00005  | ELECT3          |
| Francis Nuqui   | `francis.nuqui@klasly.test`   | 2026-00006  | ELECT3          |

## Parents

| Name            | Email                         | Children                   |
| --------------- | ----------------------------- | -------------------------- |
| Marites Mendoza | `marites.mendoza@klasly.test` | Aira, Bryan (Mother)       |
| Rodel Dimaano   | `rodel.dimaano@klasly.test`   | Carla, Dennis (Father)     |
| Linda Bautista  | `linda.bautista@klasly.test`  | Ellen, Francis (Guardian)  |
