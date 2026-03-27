INSERT INTO "MatchStatus" ("Id", "Name") VALUES
    (1, 'Upcomming'),
    (2, 'Finished')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "BettingEventType" ("Id", "Name") VALUES
    (1, 'OverUnderGoals'),
    (2, 'TeamGoals'),
    (3, 'DoubleChance'),
    (4, 'BothTeamsToScore'),
    (5, 'MatchResult'),
    (11, 'Handicap'),
    (12, 'ExactScore')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "BetStatus" ("Id", "Name") VALUES
    (1, 'Pending'),
    (2, 'Won'),
    (3, 'Lost'),
    (4, 'CashedOut')
ON CONFLICT ("Id") DO NOTHING;


INSERT INTO "League" ("Id", "Name", "Slug", "SoccerdataId") OVERRIDING SYSTEM VALUE VALUES
  (1, 'Premier League', 'premier-league', 228)
ON CONFLICT ("SoccerdataId") DO NOTHING;

INSERT INTO "Season" ("Id", "LeagueId", "Year") OVERRIDING SYSTEM VALUE VALUES
  (1, 1, '2025-2026')
ON CONFLICT ("LeagueId", "Year") DO NOTHING;

INSERT INTO "Stage" ("Id", "SeasonId", "Name", "SoccerdataId") OVERRIDING SYSTEM VALUE VALUES
  (1, 1, 'Premier League', 13908)
ON CONFLICT ("SoccerdataId") DO NOTHING;

INSERT INTO "Club" ("Name", "Slug", "LeagueId", "SoccerdataId")
VALUES
    ('Liverpool', 'liverpool', 1, 4138),
    ('AFC Bournemouth', 'afc-bournemouth', 1, 3072),
    ('Aston Villa', 'aston-villa', 1, 2912),
    ('Newcastle United', 'newcastle-united', 1, 3071),
    ('Tottenham Hotspur', 'tottenham-hotspur', 1, 2909),
    ('Burnley', 'burnley', 1, 3104),
    ('Nottingham Forest', 'nottingham-forest', 1, 4149),
    ('Brentford', 'brentford', 1, 4148),
    ('Sunderland', 'sunderland', 1, 3073),
    ('West Ham United', 'west-ham-united', 1, 3059),
    ('Brighton & Hove Albion', 'brighton-hove-albion', 1, 3200),
    ('Fulham', 'fulham', 1, 4145),
    ('Wolverhampton Wanderers', 'wolverhampton-wanderers', 1, 3074),
    ('Manchester City', 'manchester-city', 1, 4136),
    ('Chelsea', 'chelsea', 1, 2916),
    ('Crystal Palace', 'crystal-palace', 1, 4140),
    ('Manchester United', 'manchester-united', 1, 4137),
    ('Arsenal', 'arsenal', 1, 3068),
    ('Leeds United', 'leeds-united', 1, 4147),
    ('Everton', 'everton', 1, 4139)
ON CONFLICT ("SoccerdataId") DO NOTHING;