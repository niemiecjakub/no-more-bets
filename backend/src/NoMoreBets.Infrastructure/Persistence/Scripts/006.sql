CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE "DocumentChunk" (
	"Id" int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	"SourceType" varchar(50) NOT NULL,
	"SourceId" int4 NOT NULL,
	"ChunkIndex" int4 NOT NULL,
	"Content" text NOT NULL,
	"MetadataJson" text NULL,
	"Embedding" vector(1536) NOT NULL,
	"EmbeddingModel" varchar(100) NOT NULL,
	"UpdatedAt" timestamptz NOT NULL,
	CONSTRAINT "DocumentChunk_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT uq_documentchunk_source_chunk_model UNIQUE ("SourceType", "SourceId", "ChunkIndex", "EmbeddingModel")
);

CREATE INDEX idx_documentchunk_source ON public."DocumentChunk" USING btree ("SourceType", "SourceId");
CREATE INDEX idx_documentchunk_embedding ON public."DocumentChunk" USING hnsw ("Embedding" vector_cosine_ops);
