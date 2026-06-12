INSERT INTO "League" ("Id", "Name", "Slug", "SoccerdataId") OVERRIDING SYSTEM VALUE VALUES
  (8, 'Unknown', 'unknown', 0)
ON CONFLICT ("SoccerdataId") DO NOTHING;

INSERT INTO "Season" ("Id", "LeagueId", "Year") OVERRIDING SYSTEM VALUE VALUES
  (8, 8, 'N/A')
ON CONFLICT ("LeagueId", "Year") DO NOTHING;

INSERT INTO "Stage" ("Id", "SeasonId", "Name", "SoccerdataId") OVERRIDING SYSTEM VALUE VALUES
  (8, 8, 'Unknown', 0)
ON CONFLICT ("SoccerdataId") DO NOTHING;
