-- Lock agent reasoning and forecast on the slip at placement time (feedback loop / calibration).
ALTER TABLE "BetSlip"
	ADD COLUMN "Rationale" text NULL,
	ADD COLUMN "EstimatedWinProbability" numeric(5, 4) NULL;
