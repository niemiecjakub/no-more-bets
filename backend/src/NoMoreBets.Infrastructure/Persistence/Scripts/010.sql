-- 2026-2027 domestic seasons: new clubs + ClubSeason memberships.
-- Season rows 9-14 were seeded in 008.sql.

INSERT INTO "Club" ("Name", "Slug", "SoccerdataId")
VALUES
  -- Premier League
  ('Coventry City', 'coventry-city', 4161),
  ('Hull City', 'hull-city', 4153),
  ('Ipswich Town', 'ipswich-town', 4157),
  -- Ekstraklasa
  ('Slask Wroclaw', 'slask-wroclaw', 3174),
  ('Wieczysta Krakow', 'wieczysta-krakow', 6144),
  ('Wisla Krakow', 'wisla-krakow', 3192),
  -- LaLiga
  ('Racing Santander', 'racing-santander', 4915),
  ('Deportivo La Coruna', 'deportivo-la-coruna', 4900),
  ('Malaga', 'malaga', 4899),
  -- Bundesliga
  ('Schalke 04', 'schalke-04', 4275),
  ('Elversberg', 'elversberg', 4293),
  ('Paderborn', 'paderborn', 3182),
  -- Serie A
  ('Frosinone', 'frosinone', 3045),
  ('Monza', 'monza', 2976),
  ('Venezia', 'venezia', 4416),
  -- Ligue 1
  ('Le Mans', 'le-mans', 4253),
  ('Troyes', 'troyes', 3542)
ON CONFLICT ("SoccerdataId") DO NOTHING;

-- Season 9 = Premier League 2026-2027
INSERT INTO "ClubSeason" ("ClubId", "SeasonId")
SELECT club."Id", 9
FROM "Club" club
WHERE club."Slug" IN (
  'arsenal',
  'aston-villa',
  'afc-bournemouth',
  'brentford',
  'brighton-hove-albion',
  'chelsea',
  'coventry-city',
  'crystal-palace',
  'everton',
  'fulham',
  'hull-city',
  'ipswich-town',
  'leeds-united',
  'liverpool',
  'manchester-city',
  'manchester-united',
  'newcastle-united',
  'nottingham-forest',
  'sunderland',
  'tottenham-hotspur'
)
ON CONFLICT DO NOTHING;

-- Season 10 = Ekstraklasa 2026-2027
INSERT INTO "ClubSeason" ("ClubId", "SeasonId")
SELECT club."Id", 10
FROM "Club" club
WHERE club."Slug" IN (
  'cracovia',
  'gks-katowice',
  'gornik-zabrze',
  'jagiellonia-bialystok',
  'korona-kielce',
  'lech-poznan',
  'legia-warsaw',
  'motor-lublin',
  'piast-gliwice',
  'pogon-szczecin',
  'radomiak-radom',
  'rakow-czestochowa',
  'slask-wroclaw',
  'widzew-lodz',
  'wieczysta-krakow',
  'wisla-krakow',
  'wisla-plock',
  'zaglebie-lubin'
)
ON CONFLICT DO NOTHING;

-- Season 11 = LaLiga 2026-2027
-- Out: Real Oviedo, Girona, Mallorca. In: Racing Santander, Deportivo La Coruna, Malaga.
INSERT INTO "ClubSeason" ("ClubId", "SeasonId")
SELECT club."Id", 11
FROM "Club" club
WHERE club."Slug" IN (
  'alaves',
  'athletic-bilbao',
  'atletico-madrid',
  'barcelona',
  'celta-vigo',
  'deportivo-la-coruna',
  'elche',
  'espanyol',
  'getafe',
  'levante',
  'malaga',
  'osasuna',
  'racing-santander',
  'rayo-vallecano',
  'real-betis',
  'real-madrid',
  'real-sociedad',
  'sevilla',
  'valencia',
  'villarreal'
)
ON CONFLICT DO NOTHING;

-- Season 12 = Bundesliga 2026-2027
-- Out: Heidenheim, St. Pauli, Wolfsburg. In: Schalke 04, Elversberg, Paderborn.
INSERT INTO "ClubSeason" ("ClubId", "SeasonId")
SELECT club."Id", 12
FROM "Club" club
WHERE club."Slug" IN (
  'augsburg',
  'bayer-leverkusen',
  'bayern-munich',
  'borussia-dortmund',
  'borussia-mgladbach',
  'eintracht-frankfurt',
  'elversberg',
  'fc-cologne',
  'freiburg',
  'hamburg',
  'hoffenheim',
  'mainz',
  'paderborn',
  'rb-leipzig',
  'schalke-04',
  'stuttgart',
  'union-berlin',
  'werder-bremen'
)
ON CONFLICT DO NOTHING;

-- Season 13 = Serie A 2026-2027
INSERT INTO "ClubSeason" ("ClubId", "SeasonId")
SELECT club."Id", 13
FROM "Club" club
WHERE club."Slug" IN (
  'ac-milan',
  'atalanta',
  'bologna',
  'cagliari',
  'como',
  'fiorentina',
  'frosinone',
  'genoa',
  'inter-milan',
  'juventus',
  'lazio',
  'lecce',
  'monza',
  'napoli',
  'parma',
  'roma',
  'sassuolo',
  'torino',
  'udinese',
  'venezia'
)
ON CONFLICT DO NOTHING;

-- Season 14 = Ligue 1 2026-2027
-- Out: Metz, Nantes. In: Le Mans, Troyes.
INSERT INTO "ClubSeason" ("ClubId", "SeasonId")
SELECT club."Id", 14
FROM "Club" club
WHERE club."Slug" IN (
  'angers',
  'auxerre',
  'brest',
  'le-havre',
  'le-mans',
  'lens',
  'lille',
  'lorient',
  'lyon',
  'marseille',
  'monaco',
  'nice',
  'paris-fc',
  'psg',
  'rennes',
  'strasbourg',
  'toulouse',
  'troyes'
)
ON CONFLICT DO NOTHING;
