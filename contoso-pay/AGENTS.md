# AGENTS.md

## Git workflow

- Never commit automatically at the end of a task.
- Leave committing to the user to do manually.

## Implement gate (technical plan)

- Before implementing a ticket, present a technical plan and code only after user approval.
- The plan must state: DDL delta, classes/fields/sizes, libraries, and seams/ports.
- If the code already exists and matches the plan, approval is a no-op.

## Agent skills

### Issue tracker

Issues live as local markdown files under `.specs/scratch/`. See `.specs/agents/issue-tracker.md`.

### Domain docs

Single-context layout inside `.specs/` (`GLOSSARY.md` + `adr/`). See `.specs/agents/domain.md`.
