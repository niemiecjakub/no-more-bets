INSERT INTO "League" ("Id", "Name", "Slug", "SoccerdataId") OVERRIDING SYSTEM VALUE VALUES
  (7, 'FIFA World Cup', 'fifa-world-cup', 313)
ON CONFLICT ("SoccerdataId") DO NOTHING;

INSERT INTO "Season" ("Id", "LeagueId", "Year") OVERRIDING SYSTEM VALUE VALUES
  (7, 7, '2026')
ON CONFLICT ("LeagueId", "Year") DO NOTHING;

INSERT INTO "Stage" ("Id", "SeasonId", "Name", "SoccerdataId") OVERRIDING SYSTEM VALUE VALUES
  (7, 7, 'World Championship', 14750)
ON CONFLICT ("SoccerdataId") DO NOTHING;

INSERT INTO "Club" ("Name", "Slug", "LeagueId", "SoccerdataId")
VALUES
    ('Algeria', 'algeria', 7, 5472),
    ('Argentina', 'argentina', 7, 5094),
    ('Australia', 'australia', 7, 5294),
    ('Austria', 'austria', 7, 5618),
    ('Belgium', 'belgium', 7, 5269),
    ('Bosnia-Herzegovina', 'bosnia-herzegovina', 7, 5629),
    ('Brazil', 'brazil', 7, 5093),
    ('Cabo Verde', 'cabo-verde', 7, 5460),
    ('Canada', 'canada', 7, 5342),
    ('Colombia', 'colombia', 7, 5098),
    ('Congo DR', 'congo-dr', 7, 5442),
    ('Cote d''Ivoire', 'cote-d-ivoire', 7, 5448),
    ('Croatia', 'croatia', 7, 5260),
    ('Curacao', 'curacao', 7, 5539),
    ('Czechia', 'czechia', 7, 5617),
    ('Ecuador', 'ecuador', 7, 5096),
    ('Egypt', 'egypt', 7, 5284),
    ('England', 'england', 7, 5279),
    ('France', 'france', 7, 5250),
    ('Germany', 'germany', 7, 5310),
    ('Ghana', 'ghana', 7, 5346),
    ('Haiti', 'haiti', 7, 5528),
    ('IR Iran', 'ir-iran', 7, 5290),
    ('Iraq', 'iraq', 7, 5490),
    ('Japan', 'japan', 7, 5274),
    ('Jordan', 'jordan', 7, 5569),
    ('Korea Republic', 'korea-republic', 7, 5312),
    ('Mexico', 'mexico', 7, 5268),
    ('Morocco', 'morocco', 7, 5286),
    ('Netherlands', 'netherlands', 7, 5328),
    ('New Zealand', 'new-zealand', 7, 5599),
    ('Norway', 'norway', 7, 5620),
    ('Panama', 'panama', 7, 5314),
    ('Paraguay', 'paraguay', 7, 5100),
    ('Portugal', 'portugal', 7, 5251),
    ('Qatar', 'qatar', 7, 5335),
    ('Saudi Arabia', 'saudi-arabia', 7, 5282),
    ('Scotland', 'scotland', 7, 5624),
    ('Senegal', 'senegal', 7, 5321),
    ('South Africa', 'south-africa', 7, 5457),
    ('Spain', 'spain', 7, 5256),
    ('Sweden', 'sweden', 7, 5275),
    ('Switzerland', 'switzerland', 7, 5276),
    ('Tunisia', 'tunisia', 7, 5317),
    ('Turkiye', 'turkiye', 7, 5625),
    ('United States', 'united-states', 7, 5332),
    ('Uruguay', 'uruguay', 7, 5095),
    ('Uzbekistan', 'uzbekistan', 7, 5497)
ON CONFLICT ("SoccerdataId") DO NOTHING;
