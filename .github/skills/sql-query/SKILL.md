---
name: sql-query
description: 'SQL Server query patterns. No SELECT *, parameterized queries only, CTEs for complex reads, proper index awareness, query optimization hints, SQL to EF mapping patterns.'
---

# SQL Query Skill

## Core Rules

1. **No `SELECT *`** — always specify columns explicitly.
2. **Parameterized queries only** — never use string interpolation/concatenation.
3. **CTEs for complex reads** — prefer Common Table Expressions over subqueries.
4. **Index awareness** — check existing indexes before writing queries.

## Parameterized Query Pattern

```sql
-- CORRECT: Parameterized
SELECT p.Id, p.FirstName, p.LastName
FROM Patients p
WHERE p.ModuleId = @ModuleId
  AND p.IsActive = @IsActive;

-- WRONG: String concatenation (SQL injection risk)
SELECT * FROM Patients WHERE Id = '" + id + "'
```

## CTE Pattern

```sql
WITH ActiveReferrals AS (
    SELECT r.Id, r.PatientId, r.Status, r.CreatedAt,
           ROW_NUMBER() OVER (PARTITION BY r.PatientId ORDER BY r.CreatedAt DESC) AS rn
    FROM Referrals r
    WHERE r.Status IN ('Submitted', 'InProgress')
)
SELECT ar.Id, ar.PatientId, p.FullName, ar.Status
FROM ActiveReferrals ar
JOIN Patients p ON p.Id = ar.PatientId
WHERE ar.rn = 1;
```

## Index Awareness

```sql
-- Check existing indexes
SELECT i.name, ic.column_id, c.name AS column_name
FROM sys.indexes i
JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('Patients');
```

## SQL to EF Mapping

| SQL Pattern | EF Core Equivalent |
|-------------|-------------------|
| `WHERE col = @param` | `.Where(x => x.Col == param)` |
| `JOIN` | `.Include()` or `.Join()` |
| `ROW_NUMBER() OVER` | Not directly — use raw SQL or third-party |
| `CTE` | Use `FromSqlRaw()` for complex CTEs |
| `TOP N` | `.Take(n)` |

## Performance Hints

- Add `NOLOCK` only for reporting queries, never for transactional reads.
- Use `OPTION (RECOMPILE)` sparingly for parameter-sensitive queries.
- Prefer covering indexes over table lookups.
