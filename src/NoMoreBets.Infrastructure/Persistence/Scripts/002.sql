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
    (6, 'FirstTeamToScore'),
    (7, 'PlayerOrSubToScore'),
    (8, 'Goalscorer'),
    (9, 'PlayerGoalOrAssist'),
    (10, 'AnyPlayerToScore'),
    (11, 'Handicap'),
    (12, 'ExactScore')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "BetStatus" ("Id", "Name") VALUES
    (1, 'Pending'),
    (2, 'Won'),
    (3, 'Lost'),
    (4, 'CashedOut')
ON CONFLICT ("Id") DO NOTHING;


INSERT INTO "League" ("Id", "Name", "SoccerdataId") OVERRIDING SYSTEM VALUE VALUES
  (1, 'Premier League', 228)
ON CONFLICT ("SoccerdataId") DO NOTHING;

INSERT INTO "Season" ("Id", "LeagueId", "Year") OVERRIDING SYSTEM VALUE VALUES
  (1, 1, '2025-2026')
ON CONFLICT ("LeagueId", "Year") DO NOTHING;

INSERT INTO "Stage" ("Id", "SeasonId", "Name", "SoccerdataId") OVERRIDING SYSTEM VALUE VALUES
  (1, 1, 'Premier League', 13908)
ON CONFLICT ("SoccerdataId") DO NOTHING;

INSERT INTO "Club" ("Name", "LeagueId", "SoccerdataId") 
VALUES
    ('Liverpool', 1, 4138),
    ('AFC Bournemouth', 1, 3072),
    ('Aston Villa', 1, 2912),
    ('Newcastle United', 1, 3071),
    ('Tottenham Hotspur', 1, 2909),
    ('Burnley', 1, 3104),
    ('Nottingham Forest', 1, 4149),
    ('Brentford', 1, 4148),
    ('Sunderland', 1, 3073),
    ('West Ham United', 1, 3059),
    ('Brighton & Hove Albion', 1, 3200),
    ('Fulham', 1, 4145),
    ('Wolverhampton Wanderers', 1, 3074),
    ('Manchester City', 1, 4136),
    ('Chelsea', 1, 2916),
    ('Crystal Palace', 1, 4140),
    ('Manchester United', 1, 4137),
    ('Arsenal', 1, 3068),
    ('Leeds United', 1, 4147),
    ('Everton', 1, 4139)
ON CONFLICT ("SoccerdataId") DO NOTHING;