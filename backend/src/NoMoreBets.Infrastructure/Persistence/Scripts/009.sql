CREATE TABLE "ClubSeason" (
  "ClubId" int4 NOT NULL,
  "SeasonId" int4 NOT NULL,
  CONSTRAINT "ClubSeason_pkey" PRIMARY KEY ("ClubId", "SeasonId"),
  CONSTRAINT fk_clubseason_club FOREIGN KEY ("ClubId") REFERENCES "Club"("Id") ON DELETE CASCADE,
  CONSTRAINT fk_clubseason_season FOREIGN KEY ("SeasonId") REFERENCES "Season"("Id") ON DELETE CASCADE
);

CREATE INDEX idx_clubseason_season ON "ClubSeason" ("SeasonId");

-- Preserve season memberships demonstrated by existing table snapshots.
INSERT INTO "ClubSeason" ("ClubId", "SeasonId")
SELECT DISTINCT r."ClubId", s."SeasonId"
FROM "LeagueTableSnapshotRow" r
JOIN "LeagueTableSnapshot" s ON s."Id" = r."SnapshotId"
WHERE s."SeasonId" <= 8
ON CONFLICT DO NOTHING;

-- Preserve season memberships demonstrated by existing matches.
INSERT INTO "ClubSeason" ("ClubId", "SeasonId")
SELECT evidence."ClubId", evidence."SeasonId"
FROM (
  SELECT DISTINCT m."HomeClubId" AS "ClubId", s."SeasonId"
  FROM "Match" m
  JOIN "Stage" s ON s."Id" = m."StageId"
  WHERE s."SeasonId" <= 8
  UNION
  SELECT DISTINCT m."AwayClubId" AS "ClubId", s."SeasonId"
  FROM "Match" m
  JOIN "Stage" s ON s."Id" = m."StageId"
  WHERE s."SeasonId" <= 8
) evidence
ON CONFLICT DO NOTHING;

-- The old relationship is reliable only for the seasons that existed before 008.sql.
INSERT INTO "ClubSeason" ("ClubId", "SeasonId")
SELECT club."Id", season."Id"
FROM "Club" club
JOIN "Season" season ON season."LeagueId" = club."LeagueId"
WHERE season."Id" <= 8
ON CONFLICT DO NOTHING;

DO $$
BEGIN
  IF EXISTS (
    SELECT 1
    FROM "Club" club
    WHERE NOT EXISTS (
      SELECT 1
      FROM "ClubSeason" membership
      WHERE membership."ClubId" = club."Id"
    )
  ) THEN
    RAISE EXCEPTION 'ClubSeason backfill left clubs without a season membership';
  END IF;
END $$;

ALTER TABLE "Club" DROP CONSTRAINT fk_club_league;
DROP INDEX idx_club_league;
ALTER TABLE "Club" DROP COLUMN "LeagueId";
