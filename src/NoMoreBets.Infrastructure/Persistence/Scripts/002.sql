INSERT INTO "BettingEventOption" ("Id", "Name") VALUES
    (1, 'DoubleChance_HomeOrAway'),
    (2, 'DoubleChance_HomeOrDraw'),
    (3, 'DoubleChance_AwayOrDraw'),
    (4, 'MatchResult_Home'),
    (5, 'MatchResult_Away'),
    (6, 'MatchResult_Draw'),
    (7, 'BothTeamsToScore_Yes'),
    (8, 'BothTeamsToScore_No'),
    (9, 'TotalGoals_Over_0_5'),
    (10, 'TotalGoals_Under_0_5'),
    (11, 'TotalGoals_Over_1_5'),
    (12, 'TotalGoals_Under_1_5'),
    (13, 'TotalGoals_Over_2_5'),
    (14, 'TotalGoals_Under_2_5'),
    (15, 'TotalGoals_Over_3_5'),
    (16, 'TotalGoals_Under_3_5'),
    (17, 'TotalGoals_Over_4_5'),
    (18, 'TotalGoals_Under_4_5'),
    (19, 'TotalGoals_Over_5_5'),
    (20, 'TotalGoals_Under_5_5'),
    (31, 'Handicap_Home_Minus_4'),
    (32, 'Handicap_Draw_Minus_4'),
    (33, 'Handicap_Away_Plus_4'),
    (34, 'Handicap_Home_Minus_3'),
    (35, 'Handicap_Draw_Minus_3'),
    (36, 'Handicap_Away_Plus_3'),
    (37, 'Handicap_Home_Minus_2'),
    (38, 'Handicap_Draw_Minus_2'),
    (39, 'Handicap_Away_Plus_2'),
    (40, 'Handicap_Home_Minus_1'),
    (41, 'Handicap_Draw_Minus_1'),
    (42, 'Handicap_Away_Plus_1'),
    (43, 'Handicap_Home_Plus_1'),
    (44, 'Handicap_Draw_Plus_1'),
    (45, 'Handicap_Away_Minus_1'),
    (46, 'Handicap_Home_Plus_2'),
    (47, 'Handicap_Draw_Plus_2'),
    (48, 'Handicap_Away_Minus_2'),
    (49, 'Handicap_Home_Plus_3'),
    (50, 'Handicap_Draw_Plus_3'),
    (51, 'Handicap_Away_Minus_3'),
    (52, 'CorrectScore_0_0'),
    (53, 'CorrectScore_0_1'),
    (54, 'CorrectScore_0_2'),
    (55, 'CorrectScore_0_3'),
    (56, 'CorrectScore_0_4'),
    (57, 'CorrectScore_1_0'),
    (58, 'CorrectScore_1_1'),
    (59, 'CorrectScore_1_2'),
    (60, 'CorrectScore_1_3'),
    (61, 'CorrectScore_1_4'),
    (62, 'CorrectScore_2_0'),
    (63, 'CorrectScore_2_1'),
    (64, 'CorrectScore_2_2'),
    (65, 'CorrectScore_2_3'),
    (66, 'CorrectScore_2_4'),
    (67, 'CorrectScore_3_0'),
    (68, 'CorrectScore_3_1'),
    (69, 'CorrectScore_3_2'),
    (70, 'CorrectScore_3_3'),
    (71, 'CorrectScore_4_0'),
    (72, 'CorrectScore_4_1'),
    (73, 'CorrectScore_4_2'),
    (74, 'CorrectScore_4_3'),
    (75, 'CorrectScore_4_4'),
    (76, 'CorrectScore_Other')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "MatchStatus" ("Id", "Name") VALUES
    (1, 'Upcomming'),
    (2, 'Finished')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "BettingEventType" ("Id", "Name") VALUES
    (1, 'OverUnderGoals'),
    (3, 'DoubleChance'),
    (4, 'BothTeamsToScore'),
    (5, 'MatchResult'),
    (11, 'Handicap'),
    (12, 'ExactScore')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "BetStatus" ("Id", "Name") VALUES
    (1, 'Pending'),
    (2, 'Won'),
    (3, 'Lost')
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

INSERT INTO "Bankroll" ("Name", "Amount", "Flow", "BetId", "CreatedAt")
SELECT 'Salary', 8000, 'IN', NULL, timezone('utc', now())
WHERE NOT EXISTS (
  SELECT 1 FROM "Bankroll" WHERE "Name" = 'Salary' AND "Flow" = 'IN' LIMIT 1
);

INSERT INTO "Memory" ("Name", "Description", "Content", "CreatedAt", "UpdatedAt") VALUES
    ('STRATEGY', 'The core betting strategy and decision-making rules.', '', timezone('utc', now()), timezone('utc', now())),
    ('BANKROLL_MANAGEMENT', 'Rules and limits for managing capital.', '', timezone('utc', now()), timezone('utc', now())),
    ('GENERAL_KNOWLEDGE', 'General facts, patterns, and insights.', '', timezone('utc', now()), timezone('utc', now())),
    ('REFLECTIONS', 'Stores lessons and observations from past bets and outcomes.', '', timezone('utc', now()), timezone('utc', now())),
    ('THOUGHTS', 'Your thoughts.', '', timezone('utc', now()), timezone('utc', now()))
ON CONFLICT ("Name") DO NOTHING;
