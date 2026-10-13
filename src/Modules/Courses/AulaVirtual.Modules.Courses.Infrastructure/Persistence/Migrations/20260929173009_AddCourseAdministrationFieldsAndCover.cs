using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaVirtual.Modules.Courses.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseAdministrationFieldsAndCover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cover_content_type",
                schema: "courses",
                table: "courses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cover_object_key",
                schema: "courses",
                table: "courses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cover_original_file_name",
                schema: "courses",
                table: "courses",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "cover_size",
                schema: "courses",
                table: "courses",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "full_description",
                schema: "courses",
                table: "courses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "objectives",
                schema: "courses",
                table: "courses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prerequisites",
                schema: "courses",
                table: "courses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "short_description",
                schema: "courses",
                table: "courses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "syllabus",
                schema: "courses",
                table: "courses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_audience",
                schema: "courses",
                table: "courses",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cover_content_type",
                schema: "courses",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "cover_object_key",
                schema: "courses",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "cover_original_file_name",
                schema: "courses",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "cover_size",
                schema: "courses",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "full_description",
                schema: "courses",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "objectives",
                schema: "courses",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "prerequisites",
                schema: "courses",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "short_description",
                schema: "courses",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "syllabus",
                schema: "courses",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "target_audience",
                schema: "courses",
                table: "courses");
        }
    }
}
