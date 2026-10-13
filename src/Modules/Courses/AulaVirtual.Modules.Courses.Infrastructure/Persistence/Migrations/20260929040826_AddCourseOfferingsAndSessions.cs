using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaVirtual.Modules.Courses.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseOfferingsAndSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "course_offerings",
                schema: "courses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "draft"),
                    maximum_capacity = table.Column<int>(type: "integer", nullable: false),
                    base_price = table.Column<decimal>(type: "numeric", nullable: false),
                    planned_session_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_course_offerings", x => x.id);
                    table.CheckConstraint("ck_course_offerings_status", "status IN ('draft', 'published')");
                    table.ForeignKey(
                        name: "fk_course_offerings_course_id",
                        column: x => x.course_id,
                        principalSchema: "courses",
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "offering_sessions",
                schema: "courses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_offering_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    virtual_link = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offering_sessions", x => x.id);
                    table.CheckConstraint("ck_offering_sessions_time", "starts_at < ends_at");
                    table.ForeignKey(
                        name: "fk_offering_sessions_course_offering_id",
                        column: x => x.course_offering_id,
                        principalSchema: "courses",
                        principalTable: "course_offerings",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_course_offerings_course_id",
                schema: "courses",
                table: "course_offerings",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "ix_offering_sessions_course_offering_id",
                schema: "courses",
                table: "offering_sessions",
                column: "course_offering_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "offering_sessions",
                schema: "courses");

            migrationBuilder.DropTable(
                name: "course_offerings",
                schema: "courses");
        }
    }
}
