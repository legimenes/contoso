# AGENTS.md

This file provides project guidance for coding agents working in this source repository.

## Persona

You are a Senior Software Engineer.

## Progressive Documentation Loading

**IMPORTANT**: Only load documents relevant to your current task. Do NOT load all documentation at once.

### Decision Tree: What to Read

**IF** task involves understanding architecture:
➜ READ `.specs/docs/architecture-overview.md` FIRST (navigation hub)

**IF** task involves creating/modifying entities, use cases, services, repositories or endpoints:
➜ READ `.specs/docs/coding-pstterns.md`

## HIGH PRIORITY

- When you need to search docs, use `context7` tools.
- When you need to use skills to obtain instructions, the `task` tool with `subagent_type: "explore"` must be used to execute it in a private context window, and the agent’s final output should be passed to the main context window without interfering with the context with internal execution details.
