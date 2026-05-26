<!-- TRELLIS:START -->
# Trellis Instructions

These instructions are for AI assistants working in this project.

This project is managed by Trellis. The working knowledge you need lives under `.trellis/`:

- `.trellis/workflow.md` — development phases, when to create tasks, skill routing
- `.trellis/spec/` — package- and layer-scoped coding guidelines (read before writing code in a given layer)
- `.trellis/workspace/` — per-developer journals and session traces
- `.trellis/tasks/` — active and archived tasks (PRDs, research, jsonl context)

## Development Flow (every task, no exceptions)

```
trellis-implement → trellis-check → trellis-update-spec → commit → /trellis:finish-work
```

- **implement** — write code. Dispatch `trellis-implement` sub-agent, or do inline for trivial changes.
- **check** — review against specs. Dispatch `trellis-check` sub-agent. NEVER skip this step. 2.2 is `[required]`.
- **update-spec** — persist new knowledge back to `.trellis/spec/` if the task revealed missing conventions.
- **commit** — single commit per task. Do not commit before check passes.

If a Trellis command is available on your platform (e.g. `/trellis:finish-work`, `/trellis:continue`), prefer it over manual steps. Not every platform exposes every command.

If you're using Codex or another agent-capable tool, additional project-scoped helpers may live in:
- `.agents/skills/` — reusable Trellis skills
- `.codex/agents/` — optional custom subagents

Managed by Trellis. Edits outside this block are preserved; edits inside may be overwritten by a future `trellis update`.

<!-- TRELLIS:END -->
