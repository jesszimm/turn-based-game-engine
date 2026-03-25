# TurnBasedGame.Web

A small full‑stack tactical game prototype that demonstrates clean API design, deterministic game rules, and a React UI that stays in sync with the backend.

## Why This Project (Recruiter‑Focused)
- Clear separation of concerns: domain logic lives outside the UI, and the web layer only consumes DTOs.
- End‑to‑end flow: frontend → API → game engine → DTOs → frontend rendering.
- Focus on correctness: state is always refreshed after actions to avoid stale data and invalid moves.

## Tech Stack
- Backend: ASP.NET Core Web API (.NET)
- Frontend: React (Create React App)
- Architecture: Domain + Application + Web (API/UI)

## What You Can Do In The UI
- Start a new game and see the board render.
- Move a unit or attack an adjacent enemy.
- Watch AI turns execute automatically.
- See unit stats and game‑over states update immediately.

## API Endpoints (Phase 1)
- `POST /api/game/create` → create a new game session
- `GET /api/game/{id}` → get current state
- `POST /api/game/{id}/move` → move a unit
- `POST /api/game/{id}/attack` → attack a unit

## How To Run Locally

### Backend
```bash
dotnet run --project TurnBasedGame.Web/backend --urls http://localhost:5187
```

### Frontend
```bash
cd TurnBasedGame.Web/frontend
npm install
npm start
```

The frontend expects the API at `http://localhost:5187`.

## Notes For Reviewers
- DTOs are intentionally minimal and decoupled from the domain model.
- Game state is always replaced (never merged) after each action to avoid stale unit IDs.
- The UI highlights valid moves and reflects game‑over conditions immediately.

## Folder Structure
- `TurnBasedGame.Web/backend` — ASP.NET Core API
- `TurnBasedGame.Web/frontend` — React UI
- `TurnBasedGame.Domain` — core game rules
- `TurnBasedGame.Application` — orchestration and services
