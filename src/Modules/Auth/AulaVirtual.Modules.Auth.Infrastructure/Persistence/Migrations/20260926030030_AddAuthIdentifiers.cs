using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaVirtual.Modules.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthIdentifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_auth_accounts_login_type_normalized_login_identifier",
                schema: "auth",
                table: "auth_accounts");

            migrationBuilder.DropCheckConstraint(
                name: "auth_accounts_login_type_check",
                schema: "auth",
                table: "auth_accounts");

            migrationBuilder.CreateTable(
                name: "auth_identifiers",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auth_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    login_identifier = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    normalized_login_identifier = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auth_identifiers", x => x.id);
                    table.CheckConstraint("auth_identifiers_login_type_check", "login_type IN ('username', 'email')");
                    table.ForeignKey(
                        name: "fk_auth_identifiers_auth_accounts_auth_account_id",
                        column: x => x.auth_account_id,
                        principalSchema: "auth",
                        principalTable: "auth_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_auth_accounts_user_id",
                schema: "auth",
                table: "auth_accounts",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_auth_identifiers_auth_account_id_login_type",
                schema: "auth",
                table: "auth_identifiers",
                columns: new[] { "auth_account_id", "login_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_auth_identifiers_login_type_normalized_login_identifier",
                schema: "auth",
                table: "auth_identifiers",
                columns: new[] { "login_type", "normalized_login_identifier" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO auth.auth_identifiers
                    (id, auth_account_id, login_type, login_identifier,
                     normalized_login_identifier, created_at, updated_at)
                SELECT
                    gen_random_uuid(), id, login_type, login_identifier,
                    normalized_login_identifier, created_at, updated_at
                FROM auth.auth_accounts;
                """);

            migrationBuilder.Sql(
                """
                DO $migration$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM auth.auth_accounts AS account
                        LEFT JOIN auth.auth_identifiers AS identifier
                            ON identifier.auth_account_id = account.id
                        GROUP BY account.id
                        HAVING count(identifier.id) <> 1
                    ) THEN
                        RAISE EXCEPTION
                            'Auth identifier backfill did not create exactly one identifier per account.';
                    END IF;
                END
                $migration$;
                """);

            migrationBuilder.DropColumn(
                name: "login_identifier",
                schema: "auth",
                table: "auth_accounts");

            migrationBuilder.DropColumn(
                name: "login_type",
                schema: "auth",
                table: "auth_accounts");

            migrationBuilder.DropColumn(
                name: "normalized_login_identifier",
                schema: "auth",
                table: "auth_accounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $migration$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM auth.auth_accounts AS account
                        LEFT JOIN auth.auth_identifiers AS identifier
                            ON identifier.auth_account_id = account.id
                        GROUP BY account.id
                        HAVING count(identifier.id) <> 1
                    ) THEN
                        RAISE EXCEPTION
                            'Rollback requires exactly one identifier per auth account.';
                    END IF;
                END
                $migration$;
                """);

            migrationBuilder.AddColumn<string>(
                name: "login_identifier",
                schema: "auth",
                table: "auth_accounts",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "login_type",
                schema: "auth",
                table: "auth_accounts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_login_identifier",
                schema: "auth",
                table: "auth_accounts",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE auth.auth_accounts AS account
                SET
                    login_type = identifier.login_type,
                    login_identifier = identifier.login_identifier,
                    normalized_login_identifier =
                        identifier.normalized_login_identifier
                FROM auth.auth_identifiers AS identifier
                WHERE identifier.auth_account_id = account.id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "login_identifier",
                schema: "auth",
                table: "auth_accounts",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "login_type",
                schema: "auth",
                table: "auth_accounts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_login_identifier",
                schema: "auth",
                table: "auth_accounts",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_auth_accounts_login_type_normalized_login_identifier",
                schema: "auth",
                table: "auth_accounts",
                columns: new[] { "login_type", "normalized_login_identifier" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "auth_accounts_login_type_check",
                schema: "auth",
                table: "auth_accounts",
                sql: "login_type IN ('username', 'email')");

            migrationBuilder.DropTable(
                name: "auth_identifiers",
                schema: "auth");

            migrationBuilder.DropIndex(
                name: "ix_auth_accounts_user_id",
                schema: "auth",
                table: "auth_accounts");
        }
    }
}
