CREATE TABLE "Feedback" (
	"Id" int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"Message" text NOT NULL,
	"Name" varchar(200) NULL,
	"Email" varchar(320) NULL,
	"CreatedAt" timestamp NOT NULL,
	CONSTRAINT "Feedback_pkey" PRIMARY KEY ("Id")
);
CREATE INDEX idx_feedback_createdat ON public."Feedback" USING btree ("CreatedAt" DESC);
