CREATE TABLE "BetRiskLevel" (
	"Id" int4 NOT NULL,
	"Name" varchar(50) NOT NULL,
	CONSTRAINT "BetRiskLevel_pkey" PRIMARY KEY ("Id")
);

INSERT INTO "BetRiskLevel" ("Id", "Name") VALUES
    (1, 'Low'),
    (2, 'Medium'),
    (3, 'High')
ON CONFLICT ("Id") DO NOTHING;

CREATE TABLE "DailyPick" (
	"BetSlipId" int4 NOT NULL,
	"RiskLevelId" int4 NOT NULL,
	"SlipDate" date NOT NULL,
	CONSTRAINT "DailyPick_pkey" PRIMARY KEY ("BetSlipId"),
	CONSTRAINT fk_dailypick_betslip FOREIGN KEY ("BetSlipId") REFERENCES "BetSlip"("Id") ON DELETE CASCADE,
	CONSTRAINT fk_dailypick_risklevel FOREIGN KEY ("RiskLevelId") REFERENCES "BetRiskLevel"("Id") ON DELETE RESTRICT,
	CONSTRAINT uq_dailypick_date_risk UNIQUE ("SlipDate", "RiskLevelId")
);
