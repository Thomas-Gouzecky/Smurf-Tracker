using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace auth.Migrations
{
    /// <inheritdoc />
    public partial class AddOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EventType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Payload = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                ALTER TABLE public.outbox RENAME COLUMN "Id" TO id;
                ALTER TABLE public.outbox RENAME COLUMN "AggregateId" TO aggregate_id;
                ALTER TABLE public.outbox RENAME COLUMN "AggregateType" TO aggregate_type;
                ALTER TABLE public.outbox RENAME COLUMN "EventType" TO event_type;
                ALTER TABLE public.outbox RENAME COLUMN "Payload" TO payload;
                ALTER TABLE public.outbox RENAME COLUMN "CreatedAt" TO created_at;

                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'debezium') THEN
                        CREATE ROLE debezium WITH LOGIN REPLICATION PASSWORD 'postgres';
                    ELSE
                        ALTER ROLE debezium WITH LOGIN REPLICATION PASSWORD 'postgres';
                    END IF;
                END
                $$;

                GRANT CONNECT ON DATABASE auth TO debezium;
                GRANT CREATE ON DATABASE auth TO debezium;
                GRANT USAGE ON SCHEMA public TO debezium;
                GRANT SELECT ON TABLE public.outbox TO debezium;

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT FROM pg_publication WHERE pubname = 'outbox_publication'
                    ) THEN
                        CREATE PUBLICATION outbox_publication FOR TABLE public.outbox;
                    END IF;
                END
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox");
        }
    }
}
