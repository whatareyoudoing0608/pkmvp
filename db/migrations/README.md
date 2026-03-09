# DB Migrations (Oracle)

This folder stores versioned SQL migrations for Jira-style collaboration features.

## Files
- `V1__jira_core_schema.sql`: core schema changes (project/board/sprint, comments, notifications, audit)

## Apply order
1. Run `V1__jira_core_schema.sql` in DEV
2. Validate row counts, constraints, and indexes
3. Promote same script to QA/PROD

## Notes
- Scripts are written with existence checks to be re-runnable safely.
- Oracle schema prefix assumes `PKMVP`.
