ALTER TABLE "Season"
	ADD COLUMN "StartDate" date NULL,
	ADD COLUMN "EndDate" date NULL;

-- 2025-2026 domestic league season windows
UPDATE "Season" SET "StartDate" = DATE '2025-08-15', "EndDate" = DATE '2026-05-24' WHERE "Id" = 1; -- Premier League
UPDATE "Season" SET "StartDate" = DATE '2025-07-18', "EndDate" = DATE '2026-05-23' WHERE "Id" = 2; -- Ekstraklasa
UPDATE "Season" SET "StartDate" = DATE '2025-08-15', "EndDate" = DATE '2026-05-24' WHERE "Id" = 3; -- LaLiga
UPDATE "Season" SET "StartDate" = DATE '2025-08-22', "EndDate" = DATE '2026-05-16' WHERE "Id" = 4; -- Bundesliga
UPDATE "Season" SET "StartDate" = DATE '2025-08-23', "EndDate" = DATE '2026-05-24' WHERE "Id" = 5; -- Serie A
UPDATE "Season" SET "StartDate" = DATE '2025-08-15', "EndDate" = DATE '2026-05-16' WHERE "Id" = 6; -- Ligue 1
UPDATE "Season" SET "StartDate" = DATE '2026-06-11', "EndDate" = DATE '2026-07-19' WHERE "Id" = 7; -- FIFA World Cup

-- 2026-2027 domestic league seasons
INSERT INTO "Season" ("Id", "LeagueId", "Year", "StartDate", "EndDate") OVERRIDING SYSTEM VALUE VALUES
  (9, 1, '2026-2027', DATE '2026-08-21', DATE '2027-05-30'), -- Premier League
  (10, 2, '2026-2027', DATE '2026-07-24', DATE '2027-05-22'), -- Ekstraklasa
  (11, 3, '2026-2027', DATE '2026-08-15', DATE '2027-05-30'), -- LaLiga
  (12, 4, '2026-2027', DATE '2026-08-28', DATE '2027-05-22'), -- Bundesliga
  (13, 5, '2026-2027', DATE '2026-08-23', DATE '2027-05-24'), -- Serie A
  (14, 6, '2026-2027', DATE '2026-08-23', DATE '2027-05-29')  -- Ligue 1
ON CONFLICT ("LeagueId", "Year") DO NOTHING;
