-- Pre-flight duplicate-detection query for the DorsalNormalizado migration
-- (public-runner-registration-manual-payment, design.md "Migration / Rollout").
--
-- MUST be run against the production database and reviewed by an operator BEFORE
-- deploying the "AddRegistrationsAndDorsalNormalization" migration. This is a
-- deploy-blocking step, not optional tooling (tasks.md 1.11).
--
-- Why: today's Runners.Dorsal uniqueness is a plain textual index
-- (IX_Runners_RaceId_Dorsal), so "101" and "0101" can legally coexist in the same
-- race. The new migration backfills a normalized column (DorsalNormalizado, ignoring
-- leading zeros — see DorsalNormalizer.Normalize) and then creates an ADDITIVE unique
-- index on (RaceId, DorsalNormalizado). If any race already has two runners whose
-- dorsals normalize equally, that CREATE UNIQUE INDEX step will fail the deployment —
-- by design, it is the collision detector. Running this query first surfaces any such
-- collision in a readable form so an operator can re-number one of the colliding
-- runners manually before the migration runs; there is no safe automatic winner.
--
-- Usage: run this against the SAME production database the migration is about to
-- target (Postgres) — or against Sqlite for local/staging verification — and confirm
-- the result set is EMPTY before proceeding with deployment. Any returned row is a
-- blocking collision that must be resolved by hand first.
--
-- NOTE: this mirrors DorsalNormalizer.Normalize as closely as portable SQL allows
-- (strip leading zeros from a trailing run of digits, uppercase, trim). It is a
-- pre-flight AID, not a substitute for the migration's own backfill+unique-index step,
-- which remains the authoritative detector.

WITH normalized AS (
    SELECT
        r."Id"          AS "RunnerId",
        r."RaceId"      AS "RaceId",
        r."Dorsal"      AS "Dorsal",
        -- Strip leading zeros from a purely-numeric dorsal; anything else (letters,
        -- symbols) passes through trimmed/uppercased only, same passthrough rule as
        -- DorsalNormalizer.Normalize for non-conforming values.
        CASE
            WHEN UPPER(TRIM(r."Dorsal")) ~ '^[0-9]+$'
                THEN CAST(CAST(UPPER(TRIM(r."Dorsal")) AS BIGINT) AS TEXT)
            ELSE UPPER(TRIM(r."Dorsal"))
        END AS "DorsalNormalizadoAproximado"
    FROM "Runners" r
)
SELECT
    "RaceId",
    "DorsalNormalizadoAproximado",
    COUNT(*)                       AS "Colisiones",
    STRING_AGG(CAST("RunnerId" AS TEXT) || ':' || "Dorsal", ', ' ORDER BY "RunnerId") AS "RunnerIdsYDorsales"
FROM normalized
GROUP BY "RaceId", "DorsalNormalizadoAproximado"
HAVING COUNT(*) > 1
ORDER BY "RaceId", "DorsalNormalizadoAproximado";

-- Sqlite equivalent (no regex/STRING_AGG): run this instead for local/staging Sqlite
-- checks. It only catches the purely-numeric leading-zero case portably, which is the
-- dominant real-world collision shape; treat it as a quick local sanity check, not a
-- substitute for running the Postgres query above against the real production data.
--
-- WITH normalized AS (
--     SELECT
--         r."Id"     AS RunnerId,
--         r."RaceId" AS RaceId,
--         r."Dorsal" AS Dorsal,
--         CASE
--             WHEN UPPER(TRIM(r."Dorsal")) GLOB '[0-9]*'
--                  AND UPPER(TRIM(r."Dorsal")) NOT GLOB '*[^0-9]*'
--                 THEN CAST(CAST(UPPER(TRIM(r."Dorsal")) AS INTEGER) AS TEXT)
--             ELSE UPPER(TRIM(r."Dorsal"))
--         END AS DorsalNormalizadoAproximado
--     FROM Runners r
-- )
-- SELECT RaceId, DorsalNormalizadoAproximado, COUNT(*) AS Colisiones,
--        GROUP_CONCAT(RunnerId || ':' || Dorsal, ', ') AS RunnerIdsYDorsales
-- FROM normalized
-- GROUP BY RaceId, DorsalNormalizadoAproximado
-- HAVING COUNT(*) > 1
-- ORDER BY RaceId, DorsalNormalizadoAproximado;
