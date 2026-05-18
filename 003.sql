INSERT INTO "League" ("Id", "Name", "Slug", "SoccerdataId") OVERRIDING SYSTEM VALUE VALUES
  (7, 'FIFA World Cup', 'fifa-world-cup', 313)
ON CONFLICT ("SoccerdataId") DO NOTHING;

INSERT INTO "Season" ("Id", "LeagueId", "Year") OVERRIDING SYSTEM VALUE VALUES
  (7, 7, '2026')
ON CONFLICT ("LeagueId", "Year") DO NOTHING;

INSERT INTO "Stage" ("Id", "SeasonId", "Name", "SoccerdataId") OVERRIDING SYSTEM VALUE VALUES
  (7, 7, 'Group Stage', 14403)
ON CONFLICT ("SoccerdataId") DO NOTHING;

-- Club.SoccerdataId placeholders (-1) for teams not yet found in SoccerData API responses.
INSERT INTO "Club" ("Name", "Slug", "LeagueId", "SoccerdataId")
VALUES
    ('Canada', 'canada', 7, 5342),
    ('Mexico', 'mexico', 7, 5268),
    ('United States', 'united-states', 7, 5332),
    ('Australia', 'australia', 7, 5294),
    ('Iraq', 'iraq', 7, -1),
    ('Iran', 'iran', 7, 5290),
    ('Japan', 'japan', 7, 5274),
    ('Jordan', 'jordan', 7, -1),
    ('South Korea', 'south-korea', 7, 5312),
    ('Qatar', 'qatar', 7, 5335),
    ('Saudi Arabia', 'saudi-arabia', 7, 5282),
    ('Uzbekistan', 'uzbekistan', 7, -1),
    ('Algeria', 'algeria', 7, -1),
    ('Cape Verde', 'cape-verde', 7, -1),
    ('DR Congo', 'dr-congo', 7, -1),
    ('Ivory Coast', 'ivory-coast', 7, -1),
    ('Egypt', 'egypt', 7, 5284),
    ('Ghana', 'ghana', 7, 5346),
    ('Morocco', 'morocco', 7, 5286),
    ('Senegal', 'senegal', 7, 5321),
    ('South Africa', 'south-africa', 7, -1),
    ('Tunisia', 'tunisia', 7, 5317),
    ('Curacao', 'curacao', 7, -1),
    ('Haiti', 'haiti', 7, -1),
    ('Panama', 'panama', 7, 5314),
    ('Argentina', 'argentina', 7, 5094),
    ('Brazil', 'brazil', 7, 5093),
    ('Colombia', 'colombia', 7, 5098),
    ('Ecuador', 'ecuador', 7, 5096),
    ('Paraguay', 'paraguay', 7, -1),
    ('Uruguay', 'uruguay', 7, 5095),
    ('New Zealand', 'new-zealand', 7, 5599),
    ('Austria', 'austria', 7, 5618),
    ('Belgium', 'belgium', 7, 5269),
    ('Bosnia and Herzegovina', 'bosnia-and-herzegovina', 7, 5629),
    ('Croatia', 'croatia', 7, 5260),
    ('Czech Republic', 'czech-republic', 7, 5617),
    ('England', 'england', 7, 5279),
    ('France', 'france', 7, 5250),
    ('Germany', 'germany', 7, 5310),
    ('Netherlands', 'netherlands', 7, 5328),
    ('Norway', 'norway', 7, 5620),
    ('Portugal', 'portugal', 7, 5251),
    ('Scotland', 'scotland', 7, 5624),
    ('Spain', 'spain', 7, 5256),
    ('Sweden', 'sweden', 7, 5275),
    ('Switzerland', 'switzerland', 7, 5276),
    ('Turkey', 'turkey', 7, 5625)
ON CONFLICT ("SoccerdataId") DO NOTHING;
