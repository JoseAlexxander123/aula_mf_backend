using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaVirtual.Modules.Courses.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseOfferingCommercialAndInstructors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                schema: "courses",
                table: "course_offerings",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "course_offering_promotions",
                schema: "courses",
                columns: table => new
                {
                    course_offering_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    value = table.Column<decimal>(type: "numeric", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_course_offering_promotions", x => x.course_offering_id);
                    table.CheckConstraint("ck_course_offering_promotions_type", "type IN ('percentage', 'fixed_amount')");
                    table.CheckConstraint("ck_course_offering_promotions_value", "value > 0 AND (type <> 'percentage' OR value <= 100)");
                    table.CheckConstraint("ck_course_offering_promotions_window", "starts_at < ends_at");
                    table.ForeignKey(
                        name: "fk_course_offering_promotions_offering_id",
                        column: x => x.course_offering_id,
                        principalSchema: "courses",
                        principalTable: "course_offerings",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "instructors",
                schema: "courses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instructors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "course_offering_instructors",
                schema: "courses",
                columns: table => new
                {
                    course_offering_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instructor_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_course_offering_instructors", x => new { x.course_offering_id, x.instructor_id });
                    table.ForeignKey(
                        name: "fk_course_offering_instructors_instructor_id",
                        column: x => x.instructor_id,
                        principalSchema: "courses",
                        principalTable: "instructors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_course_offering_instructors_offering_id",
                        column: x => x.course_offering_id,
                        principalSchema: "courses",
                        principalTable: "course_offerings",
                        principalColumn: "id");
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_course_offerings_currency_code",
                schema: "courses",
                table: "course_offerings",
                sql: "currency_code IS NULL OR currency_code ~ '^[A-Z]{3}$'");

            migrationBuilder.CreateIndex(
                name: "ix_course_offering_instructors_instructor_id",
                schema: "courses",
                table: "course_offering_instructors",
                column: "instructor_id");

            migrationBuilder.CreateIndex(
                name: "ux_instructors_user_id",
                schema: "courses",
                table: "instructors",
                column: "user_id",
                unique: true,
                filter: "user_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_offering_instructors",
                schema: "courses");

            migrationBuilder.DropTable(
                name: "course_offering_promotions",
                schema: "courses");

            migrationBuilder.DropTable(
                name: "instructors",
                schema: "courses");

            migrationBuilder.DropCheckConstraint(
                name: "ck_course_offerings_currency_code",
                schema: "courses",
                table: "course_offerings");

            migrationBuilder.DropColumn(
                name: "currency_code",
                schema: "courses",
                table: "course_offerings");
        }
    }
}
