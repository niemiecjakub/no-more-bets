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
