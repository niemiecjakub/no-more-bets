INSERT INTO "Stage" ("Id", "SeasonId", "Name", "SoccerdataId") OVERRIDING SYSTEM VALUE VALUES
  (9, 9, 'Premier League', 14840),
  (10, 10, 'Ekstraklasa', 14823),
  (11, 11, 'LaLiga', -1),
  (12, 12, 'Bundesliga', -2),
  (13, 13, 'Serie A', 14802),
  (14, 14, 'Ligue 1', 14816)
ON CONFLICT ("SoccerdataId") DO NOTHING;
