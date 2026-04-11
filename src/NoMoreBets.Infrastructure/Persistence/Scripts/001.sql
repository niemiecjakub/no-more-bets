CREATE TABLE "League" (
	"Id" int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"Name" varchar(200) NOT NULL,
	"Slug" varchar(200) NOT NULL,
	"SoccerdataId" int4 NOT NULL,
	CONSTRAINT "League_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT uq_league_soccerdata UNIQUE ("SoccerdataId"),
	CONSTRAINT uq_league_slug UNIQUE ("Slug")
);
CREATE INDEX idx_league_name ON public."League" USING btree ("Name");



CREATE TABLE "MatchStatus" (
	"Id" int4 NOT NULL,
	"Name" varchar(50) NOT NULL,
	CONSTRAINT "MatchStatus_pkey" PRIMARY KEY ("Id")
);


CREATE TABLE "Club" (
	"Id" int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"Name" varchar(200) NOT NULL,
	"Slug" varchar(200) NOT NULL,
	"LeagueId" int4 NOT NULL,
	"SoccerdataId" int4 NOT NULL,
	CONSTRAINT "Club_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT uq_club_soccerdata UNIQUE ("SoccerdataId"),
	CONSTRAINT uq_club_slug UNIQUE ("Slug"),
	CONSTRAINT fk_club_league FOREIGN KEY ("LeagueId") REFERENCES "League"("Id") ON DELETE CASCADE
);
CREATE INDEX idx_club_league ON public."Club" USING btree ("LeagueId");
CREATE INDEX idx_club_name ON public."Club" USING btree ("Name");


CREATE TABLE "ClubDailySummary" (
	"Id" int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"ClubId" int4 NOT NULL,
	"Date" date NOT NULL,
	"Summary" text NOT NULL,
	CONSTRAINT "ClubDailySummary_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT fk_clubdailysummary_club FOREIGN KEY ("ClubId") REFERENCES "Club"("Id") ON DELETE CASCADE
);
CREATE INDEX idx_clubdailysummary_club ON public."ClubDailySummary" USING btree ("ClubId");
CREATE INDEX idx_clubdailysummary_club_date ON public."ClubDailySummary" USING btree ("ClubId", "Date");


CREATE TABLE "Season" (
	"Id" int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"LeagueId" int4 NOT NULL,
	"Year" varchar(20) NOT NULL,
	CONSTRAINT "Season_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT uq_season UNIQUE ("LeagueId", "Year"),
	CONSTRAINT fk_season_league FOREIGN KEY ("LeagueId") REFERENCES "League"("Id") ON DELETE CASCADE
);
CREATE INDEX idx_season_league ON public."Season" USING btree ("LeagueId");


CREATE TABLE "Stage" (
	"Id" int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"SeasonId" int4 NOT NULL,
	"Name" varchar(200) NOT NULL,
	"SoccerdataId" int4 NOT NULL,
	CONSTRAINT "Stage_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT uq_stage UNIQUE ("SeasonId", "Name"),
	CONSTRAINT uq_stage_soccerdata UNIQUE ("SoccerdataId"),
	CONSTRAINT fk_stage_season FOREIGN KEY ("SeasonId") REFERENCES "Season"("Id") ON DELETE CASCADE
);
CREATE INDEX idx_stage_season ON public."Stage" USING btree ("SeasonId");


CREATE TABLE "Match" (
	"Id" int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"StageId" int4 NOT NULL,
	"MatchDate" timestamp NOT NULL,
	"HomeClubId" int4 NOT NULL,
	"AwayClubId" int4 NOT NULL,
	"MatchStatusId" int4 NOT NULL,
	"HomeGoals" int4 NULL,
	"AwayGoals" int4 NULL,
	"SoccerdataId" int4,
	"BetclicUrl" text NULL,
	"FotmobUrl" text NULL,
	CONSTRAINT "Game_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT chk_game_not_same_club CHECK (("HomeClubId" <> "AwayClubId")),
	CONSTRAINT fk_game_awayclub FOREIGN KEY ("AwayClubId") REFERENCES "Club"("Id"),
	CONSTRAINT fk_game_homeclub FOREIGN KEY ("HomeClubId") REFERENCES "Club"("Id"),
	CONSTRAINT fk_game_matchstatus FOREIGN KEY ("MatchStatusId") REFERENCES "MatchStatus"("Id") ON DELETE RESTRICT,
	CONSTRAINT fk_game_stage FOREIGN KEY ("StageId") REFERENCES "Stage"("Id") ON DELETE CASCADE
);
CREATE INDEX idx_game_awayclub ON public."Match" USING btree ("AwayClubId");
CREATE INDEX idx_game_awayclub_date ON public."Match" USING btree ("AwayClubId", "MatchDate");
CREATE INDEX idx_game_homeclub ON public."Match" USING btree ("HomeClubId");
CREATE INDEX idx_game_homeclub_date ON public."Match" USING btree ("HomeClubId", "MatchDate");
CREATE INDEX idx_game_matchdate ON public."Match" USING btree ("MatchDate");
CREATE INDEX idx_game_matchstatus_date ON public."Match" USING btree ("MatchStatusId", "MatchDate");
CREATE INDEX idx_game_stage ON public."Match" USING btree ("StageId");


CREATE TABLE "Lineup" (
	"MatchId" int4 NOT NULL,
	"HomeTeamJson" jsonb NOT NULL,
	"AwayTeamJson" jsonb NOT NULL,
	"UpdatedAt" timestamp NOT NULL,
	CONSTRAINT "Lineup_pkey" PRIMARY KEY ("MatchId"),
	CONSTRAINT fk_lineup_match FOREIGN KEY ("MatchId") REFERENCES "Match"("Id") ON DELETE CASCADE
);


CREATE TABLE "MatchPreview" (
	"MatchId" int4 NOT NULL,
	"PreviewContentJson" jsonb NOT NULL,
	CONSTRAINT "MatchPreview_pkey" PRIMARY KEY ("MatchId"),
	CONSTRAINT fk_matchpreview_match FOREIGN KEY ("MatchId") REFERENCES "Match"("Id") ON DELETE CASCADE
);


CREATE TABLE "Head2Head" (
	"Team1Id" int4 NOT NULL,
	"Team2Id" int4 NOT NULL,
	"Head2HeadJson" jsonb NOT NULL,
	"UpdatedAt" timestamp NOT NULL,
	CONSTRAINT "Head2Head_pkey" PRIMARY KEY ("Team1Id", "Team2Id"),
	CONSTRAINT fk_head2head_team1 FOREIGN KEY ("Team1Id") REFERENCES "Club"("Id") ON DELETE CASCADE,
	CONSTRAINT fk_head2head_team2 FOREIGN KEY ("Team2Id") REFERENCES "Club"("Id") ON DELETE CASCADE
);
CREATE INDEX idx_head2head_team1 ON public."Head2Head" USING btree ("Team1Id");
CREATE INDEX idx_head2head_team2 ON public."Head2Head" USING btree ("Team2Id");


CREATE TABLE "LeagueTableSnapshot" (
    "Id"           bigserial PRIMARY KEY,
    "LeagueId"     int4 NOT NULL,
    "SeasonId"     int4 NOT NULL,
    "SnapshotDate" date NOT NULL,

    CONSTRAINT fk_snapshot_league
        FOREIGN KEY ("LeagueId")
        REFERENCES "League"("Id")
        ON DELETE CASCADE,

    CONSTRAINT fk_snapshot_season
        FOREIGN KEY ("SeasonId")
        REFERENCES "Season"("Id")
        ON DELETE CASCADE,

    CONSTRAINT uq_snapshot UNIQUE ("SeasonId", "SnapshotDate")
);

CREATE INDEX idx_snapshot_league_date
ON "LeagueTableSnapshot" ("LeagueId", "SnapshotDate");

CREATE INDEX idx_snapshot_season_date
ON "LeagueTableSnapshot" ("SeasonId", "SnapshotDate");


CREATE TABLE "LeagueTableSnapshotRow" (
    "SnapshotId"      bigint NOT NULL,
    "ClubId"          int4 NOT NULL,

    "Position"        int4 NOT NULL,
    "MatchesPlayed"   int4 NOT NULL,
    "Wins"            int4 NOT NULL,
    "Draws"           int4 NOT NULL,
    "Losses"          int4 NOT NULL,
    "GoalsFor"        int4 NOT NULL,
    "GoalsAgainst"    int4 NOT NULL,
    "GoalDifference"  int4 NOT NULL,
    "Points"          int4 NOT NULL,

    "Xg"              numeric(6,2) NOT NULL,
    "XgDiff"          numeric(6,2) NOT NULL,
    "Xga"             numeric(6,2) NOT NULL,
    "XgaDiff"         numeric(6,2) NOT NULL,
    "Xpts"            numeric(6,2) NOT NULL,
    "XptsDiff"        numeric(6,2) NOT NULL,

    PRIMARY KEY ("SnapshotId", "ClubId"),

    CONSTRAINT fk_row_snapshot
        FOREIGN KEY ("SnapshotId")
        REFERENCES "LeagueTableSnapshot"("Id")
        ON DELETE CASCADE,

    CONSTRAINT fk_row_club
        FOREIGN KEY ("ClubId")
        REFERENCES "Club"("Id")
        ON DELETE CASCADE
);

CREATE INDEX idx_row_snapshot_position
ON "LeagueTableSnapshotRow" ("SnapshotId", "Position");

CREATE INDEX idx_row_club
ON "LeagueTableSnapshotRow" ("ClubId");


CREATE TABLE "BettingOddsSnapshot" (
    "Id"           bigserial PRIMARY KEY,
    "MatchId"      int4 NOT NULL,
    "SnapshotTime" timestamp NOT NULL,

    CONSTRAINT fk_bettingoddssnapshot_match
        FOREIGN KEY ("MatchId")
        REFERENCES "Match"("Id")
        ON DELETE CASCADE
);

CREATE INDEX idx_bettingoddssnapshot_match_time
ON "BettingOddsSnapshot" ("MatchId", "SnapshotTime");


CREATE TABLE "BettingEventType" (
    "Id"   int4 NOT NULL,
    "Name" varchar(50) NOT NULL,
    CONSTRAINT "BettingEventType_pkey" PRIMARY KEY ("Id")
);

CREATE TABLE "BettingEventOption" (
    "Id"   int4 NOT NULL,
    "Name" varchar(80) NOT NULL,
    CONSTRAINT "BettingEventOption_pkey" PRIMARY KEY ("Id")
);

CREATE TABLE "BettingOddsSnapshotRow" (
    "Id"          bigserial PRIMARY KEY,
    "SnapshotId"   bigint NOT NULL,
    "EventTypeId" int4 NOT NULL,
    "EventOptionId" int4 NULL,
    "Odds" numeric(18, 4) NULL,

    CONSTRAINT fk_bettingoddssnapshotrow_snapshot
        FOREIGN KEY ("SnapshotId")
        REFERENCES "BettingOddsSnapshot"("Id")
        ON DELETE CASCADE,

    CONSTRAINT fk_bettingoddssnapshotrow_eventtype
        FOREIGN KEY ("EventTypeId")
        REFERENCES "BettingEventType"("Id")
        ON DELETE RESTRICT,

    CONSTRAINT fk_bettingoddssnapshotrow_eventoption
        FOREIGN KEY ("EventOptionId")
        REFERENCES "BettingEventOption"("Id")
        ON DELETE RESTRICT
);

CREATE INDEX idx_bettingoddssnapshotrow_snapshot_eventtype
ON "BettingOddsSnapshotRow" ("SnapshotId", "EventTypeId");


CREATE TABLE "MatchDetails" (
	"Id" int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"FotmobUrl" text NULL,
	"MatchId" int4 NULL,
	"FotmobDetailsJson" jsonb NULL,
	"FotmobReview" text NULL,
	CONSTRAINT "MatchDetails_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT uq_matchdetails_match UNIQUE ("MatchId"),
	CONSTRAINT fk_matchdetails_match FOREIGN KEY ("MatchId") REFERENCES "Match"("Id") ON DELETE SET NULL
);
CREATE INDEX idx_matchdetails_match ON public."MatchDetails" USING btree ("MatchId");
CREATE UNIQUE INDEX uq_matchdetails_fotmoburl ON public."MatchDetails" ("FotmobUrl") WHERE "FotmobUrl" IS NOT NULL;

-- AgentSessionPhase: 1=Research, 2=Betting, 3=Reflection (matches NoMoreBets.Domain.AgentSessions.AgentSessionPhase)
CREATE TABLE "AgentSession" (
	"Id" int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"Phase" int4 NOT NULL,
	"StartedAt" timestamp NOT NULL,
	CONSTRAINT "AgentSession_pkey" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_AgentSession_Phase_StartedAt" ON public."AgentSession" USING btree ("Phase", "StartedAt");

CREATE TABLE "MatchAnalysis" (
	"Id" int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"MatchId" int4 NOT NULL,
	"AgentSessionId" int4 NULL,
	"Code" varchar(255) NOT NULL,
	"Content" jsonb NOT NULL,
	CONSTRAINT "MatchAnalysis_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT fk_matchanalysis_match FOREIGN KEY ("MatchId") REFERENCES "Match"("Id") ON DELETE CASCADE,
	CONSTRAINT fk_matchanalysis_agentsession FOREIGN KEY ("AgentSessionId") REFERENCES "AgentSession"("Id") ON DELETE SET NULL
);

CREATE INDEX idx_matchanalysis_match ON public."MatchAnalysis" USING btree ("MatchId");
CREATE INDEX "IX_MatchAnalysis_AgentSessionId" ON public."MatchAnalysis" USING btree ("AgentSessionId");


CREATE TABLE "BetStatus" (
	"Id" int4 NOT NULL,
	"Name" varchar(50) NOT NULL,
	CONSTRAINT "BetStatus_pkey" PRIMARY KEY ("Id")
);

CREATE TABLE "BetSlip" (
	"Id" int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"AgentSessionId" int4 NULL,
	"StakeAmount" numeric(18, 4) NOT NULL,
	"TotalOdds" numeric(18, 4) NOT NULL,
	"PotentialPayout" numeric(18, 4) NOT NULL,
	"StatusId" int4 NOT NULL,
	"CreatedAt" timestamp NOT NULL,
	"UpdatedAt" timestamp NULL,
	CONSTRAINT "BetSlip_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT fk_betslip_status FOREIGN KEY ("StatusId") REFERENCES "BetStatus"("Id") ON DELETE RESTRICT,
	CONSTRAINT fk_betslip_agentsession FOREIGN KEY ("AgentSessionId") REFERENCES "AgentSession"("Id") ON DELETE SET NULL
);
CREATE INDEX idx_betslip_statusid ON public."BetSlip" USING btree ("StatusId");
CREATE INDEX "IX_BetSlip_AgentSessionId" ON public."BetSlip" USING btree ("AgentSessionId");

-- AgentSessionMessageKind: 1=Message, 2=Reasoning, 3=FunctionCall (matches NoMoreBets.Domain.AgentSessions.AgentSessionMessageKind)
CREATE TABLE "AgentSessionMessage" (
	"Id" int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"SessionId" int4 NOT NULL,
	"Ordinal" int4 NOT NULL,
	"Kind" int4 NOT NULL,
	"Text" text NOT NULL,
	CONSTRAINT "AgentSessionMessage_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT fk_agentsessionmessage_session FOREIGN KEY ("SessionId") REFERENCES "AgentSession"("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_AgentSessionMessage_SessionId" ON public."AgentSessionMessage" USING btree ("SessionId");

CREATE TABLE "BetSelection" (
	"Id" int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"BetSlipId" int4 NOT NULL,
	"MatchId" int4 NOT NULL,
	"EventTypeId" int4 NOT NULL,
	"EventOptionId" int4 NOT NULL,
	"OddsAtPlacement" numeric(18, 4) NOT NULL,
	"StatusId" int4 NOT NULL,
	"UpdatedAt" timestamp NULL,
	CONSTRAINT "BetSelection_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT fk_betselection_betslip FOREIGN KEY ("BetSlipId") REFERENCES "BetSlip"("Id") ON DELETE CASCADE,
	CONSTRAINT fk_betselection_match FOREIGN KEY ("MatchId") REFERENCES "Match"("Id") ON DELETE RESTRICT,
	CONSTRAINT fk_betselection_eventtype FOREIGN KEY ("EventTypeId") REFERENCES "BettingEventType"("Id") ON DELETE RESTRICT,
	CONSTRAINT fk_betselection_eventoption FOREIGN KEY ("EventOptionId") REFERENCES "BettingEventOption"("Id") ON DELETE RESTRICT,
	CONSTRAINT fk_betselection_status FOREIGN KEY ("StatusId") REFERENCES "BetStatus"("Id") ON DELETE RESTRICT
);
CREATE INDEX idx_betselection_betslipid ON public."BetSelection" USING btree ("BetSlipId");
CREATE INDEX idx_betselection_matchid ON public."BetSelection" USING btree ("MatchId");
CREATE INDEX idx_betselection_eventoptionid ON public."BetSelection" USING btree ("EventOptionId");
CREATE INDEX idx_betselection_statusid ON public."BetSelection" USING btree ("StatusId");

CREATE TABLE "Memory" (
	"Id" int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"Name" varchar(200) NOT NULL,
	"Description" text NULL,
	"Content" text NOT NULL,
	"CreatedAt" timestamp NOT NULL,
	"UpdatedAt" timestamp NOT NULL,
	CONSTRAINT "Memory_pkey" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX idx_memory_name ON public."Memory" USING btree ("Name");

CREATE TABLE "Bankroll" (
	"Id" int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"Name" varchar(200) NOT NULL,
	"Amount" numeric(18, 4) NOT NULL,
	"Flow" varchar(3) NOT NULL,
	"BetId" int4 NULL,
	"CreatedAt" timestamp NOT NULL,
	CONSTRAINT "Bankroll_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT chk_bankroll_flow CHECK ("Flow" IN ('IN', 'OUT')),
	CONSTRAINT fk_bankroll_betslip FOREIGN KEY ("BetId") REFERENCES "BetSlip"("Id") ON DELETE RESTRICT
);
CREATE INDEX idx_bankroll_betid ON public."Bankroll" USING btree ("BetId");