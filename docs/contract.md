# Project Contract: performance-hub

This file is the single source of truth between the two developers. If something here changes, change this file in a Pull Request and tell the other person before merging.

## 1. Overview

An employee Performance Management System. Employees set their own objectives, managers approve or reject them and evaluate performance, and HR sees everything through a general dashboard.

| Part | Owner |
|---|---|
| Users, Departments, Login, JWT, Authorization, General Dashboard (HR) | Jana |
| Objectives, Evaluations, User Dashboard (Employee and Manager views) | Ruaa |

## 2. Stack and Repo

- Backend: .NET 10 Web API (Controllers, not Minimal APIs), EF Core, SQL Server
- Frontend: React (Vite) in `frontend/`
- Auth: JWT Bearer
- Repo layout:

```
performance-hub/
├── backend/      (PerformanceApi.csproj is directly inside this folder)
├── frontend/
├── docs/
│   └── contract.md
└── README.md
```

- Frontend dev URL: `http://localhost:5173` (must be allowed in CORS)
- Backend URL: use the port from `backend/Properties/launchSettings.json`. Put it in `frontend/.env` as `VITE_API_URL`. Do not commit `.env`.

## 3. Git Rules

- Nobody pushes directly to `main`. Every task is a branch plus a Pull Request, and the other person reviews it.
- Branch names: `feature/jana-<task>` and `feature/ruaa-<task>`.
- Run `git pull origin main` before starting work and again before opening a PR.
- Shared files are the ones that cause conflicts: `AppDbContext.cs`, `Program.cs`. Tell the other person before editing them and merge those PRs quickly.
- Never commit secrets: connection strings, JWT key, passwords. Use `appsettings.Development.json` (git-ignored) and keep an `appsettings.Example.json` with fake values.

## 4. Roles (fixed)

`Employee`, `Manager`, `HR`

There are no custom roles. Role values are stored and sent as these exact strings.

## 5. Entities

### Department (owner: Jana)
| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| Name | string | unique, required |

### User (owner: Jana)
| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| FullName | string | required |
| Email | string | unique, required |
| PasswordHash | string | never returned by the API |
| Role | enum Role | Employee / Manager / HR |
| DepartmentId | int | FK to Department |
| ManagerId | int? | self reference, null for top level or HR |
| IsActive | bool | default true |

"A manager's team" means all users whose `ManagerId` equals the manager's `Id`.

### Objective (owner: Ruaa)
| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| UserId | int | FK to User (the owner of the objective) |
| Title | string | required |
| Description | string | optional |
| StartDate | date | required |
| EndDate | date | required, after StartDate |
| Progress | int | 0 to 100 |
| Status | enum ObjectiveStatus | see flow below |
| RejectionReason | string? | filled when rejected |
| ReviewedByUserId | int? | the manager who approved or rejected |
| CreatedAt | datetime (UTC) | |
| UpdatedAt | datetime (UTC) | |

### Evaluation (owner: Ruaa)
| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| UserId | int | the employee being evaluated |
| ManagerId | int | the manager who wrote it |
| Period | string | for example `2026-Q4` |
| Score | decimal | 0 to 100 |
| Comment | string | manager's comment |
| CreatedAt | datetime (UTC) | |

One evaluation per (UserId, Period).

## 6. Objective Status Flow

```
Draft -> PendingApproval -> Approved -> InProgress -> Completed
              |
              v
          Rejected -> (employee edits) -> PendingApproval
```

Enum `ObjectiveStatus`: `Draft`, `PendingApproval`, `Approved`, `Rejected`, `InProgress`, `Completed`.

Rules:
- Employee can edit title, description and dates only when status is `Draft` or `Rejected`.
- Employee submits for approval: `Draft` or `Rejected` becomes `PendingApproval`.
- Manager approves or rejects only objectives with status `PendingApproval` and only for their own team. Rejecting requires a reason.
- Progress can be updated only after approval (`Approved`, `InProgress`). When progress becomes more than 0 the status becomes `InProgress`, and at 100 it becomes `Completed`.
- Delete is allowed only for `Draft`.

## 7. Authentication and JWT

The token must contain these claims:

| Claim | Meaning |
|---|---|
| `userId` | the user's Id |
| `role` | `Employee`, `Manager` or `HR` |
| `departmentId` | the user's department Id |

- Header: `Authorization: Bearer <token>`
- Token lifetime: 8 hours (can be changed later)
- Authorization is enforced in the backend. Hiding buttons in React is only for UX.
- Ruaa's controllers must read `userId` and `role` from the token. They must never trust a user id sent by the client for "my data" endpoints.

**Until Jana's login is merged**, Jana adds seeded test users and a temporary dev-only endpoint that returns a token, so Ruaa can test:

| Email | Role |
|---|---|
| employee@test.com | Employee |
| manager@test.com | Manager (manager of the employee above) |
| hr@test.com | HR |

## 8. API Endpoints

All routes start with `/api`. JSON uses camelCase, enums are sent as strings, dates are ISO 8601 UTC.

### Auth (Jana)
| Method | Route | Access | Purpose |
|---|---|---|---|
| POST | `/auth/login` | public | email and password, returns `{ token, user }` |
| GET | `/auth/me` | any logged in | current user info |

### Users and Departments (Jana)
| Method | Route | Access | Purpose |
|---|---|---|---|
| GET | `/users` | HR | list all users |
| GET | `/users/{id}` | HR, or the user himself, or his manager | one user |
| GET | `/users/my-team` | Manager | direct reports |
| POST | `/users` | HR | create user |
| PUT | `/users/{id}` | HR | update user |
| DELETE | `/users/{id}` | HR | deactivate user |
| GET | `/departments` | any logged in | list departments |
| POST / PUT / DELETE | `/departments` | HR | manage departments |

### Objectives (Ruaa)
| Method | Route | Access | Purpose |
|---|---|---|---|
| GET | `/objectives/mine` | any logged in | my objectives |
| GET | `/objectives/team` | Manager (own team), HR (all) | team objectives, optional `?status=` and `?userId=` |
| GET | `/objectives/{id}` | owner, owner's manager, HR | one objective |
| POST | `/objectives` | any logged in | create objective for myself (status `Draft`) |
| PUT | `/objectives/{id}` | owner | edit (only `Draft` or `Rejected`) |
| DELETE | `/objectives/{id}` | owner | delete (only `Draft`) |
| POST | `/objectives/{id}/submit` | owner | send for approval |
| POST | `/objectives/{id}/approve` | Manager of the owner | approve |
| POST | `/objectives/{id}/reject` | Manager of the owner | body `{ "reason": "..." }` |
| PATCH | `/objectives/{id}/progress` | owner | body `{ "progress": 0-100 }` |

### Evaluations (Ruaa)
| Method | Route | Access | Purpose |
|---|---|---|---|
| GET | `/evaluations/mine` | any logged in | my evaluations |
| GET | `/evaluations/team` | Manager (own team), HR (all) | team evaluations |
| POST | `/evaluations` | Manager of the employee | create evaluation |
| PUT | `/evaluations/{id}` | Manager who wrote it | edit |

### Dashboard (Jana)
Optional query parameters on all of these: `departmentId`, `from`, `to`.

| Method | Route | Access | Purpose |
|---|---|---|---|
| GET | `/dashboard/summary` | HR (Manager: own department) | totals: employees, objectives, average progress, overdue objectives |
| GET | `/dashboard/objectives-by-status` | HR (Manager: own department) | count per status (pie chart) |
| GET | `/dashboard/progress-by-department` | HR | average progress per department (bar chart) |
| GET | `/dashboard/score-distribution` | HR (Manager: own department) | evaluation score buckets |

Jana's dashboard queries read the `Objectives` and `Evaluations` tables. Ruaa must not rename those tables or columns without telling Jana.

## 9. Permission Summary

| Action | Employee | Manager | HR |
|---|---|---|---|
| Create objective for myself | yes | yes | yes |
| See my own objectives and evaluations | yes | yes | yes |
| See team objectives | no | own team | all |
| Approve or reject objectives | no | own team | no (open question) |
| Write evaluations | no | own team | no |
| Manage users and departments | no | no | yes |
| General dashboard | no | own department | all |

## 10. Conventions

- Errors use standard ProblemDetails JSON. Status codes: 400 validation, 401 not logged in, 403 not allowed, 404 not found.
- Return DTOs, never the EF entities directly. Never return `PasswordHash`.
- CORS: allow `http://localhost:5173`.
- Database: each developer runs a local SQL Server database. Share changes only through EF migrations committed to the repo. Before creating a migration, pull `main` first. If both of you create migrations at the same time, one person re-creates theirs after merging.

## 11. Open Questions for the Supervisor

1. Should HR also be able to approve or reject objectives?
2. Is a self-registration page needed, or does HR create all accounts?
3. Should a rejected or completed objective be allowed to be reopened?
4. Is evaluation per quarter, per month, or free?

Decisions made so far: manager approval is required, roles are fixed, the dashboard shows numbers, charts and a department filter, and evaluation has both a score and a manager comment.
