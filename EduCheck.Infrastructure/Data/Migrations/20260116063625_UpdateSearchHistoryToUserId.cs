using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCheck.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSearchHistoryToUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_institute_search_history_institutes_InstituteId",
                table: "institute_search_history");

            migrationBuilder.DropForeignKey(
                name: "FK_institute_search_history_students_StudentId",
                table: "institute_search_history");

            migrationBuilder.DropIndex(
                name: "IX_institute_search_history_SearchedAt",
                table: "institute_search_history");

            migrationBuilder.DropIndex(
                name: "IX_institute_search_history_StudentId_InstituteId",
                table: "institute_search_history");

            migrationBuilder.DropIndex(
                name: "IX_institute_search_history_StudentId_SearchedAt",
                table: "institute_search_history");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "institute_search_history",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SearchedAt",
                table: "institute_search_history",
                newName: "searched_at");

            migrationBuilder.RenameColumn(
                name: "InstituteId",
                table: "institute_search_history",
                newName: "institute_id");

            migrationBuilder.RenameIndex(
                name: "IX_institute_search_history_InstituteId",
                table: "institute_search_history",
                newName: "IX_institute_search_history_institute_id");

            migrationBuilder.AlterColumn<Guid>(
                name: "StudentId",
                table: "institute_search_history",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "searched_at",
                table: "institute_search_history",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "institute_search_history",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "institute_search_history",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_institute_search_history_StudentId",
                table: "institute_search_history",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "ix_institute_search_history_user_id",
                table: "institute_search_history",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_institute_search_history_user_institute",
                table: "institute_search_history",
                columns: new[] { "user_id", "institute_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_institute_search_history_institutes_institute_id",
                table: "institute_search_history",
                column: "institute_id",
                principalTable: "institutes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_institute_search_history_students_StudentId",
                table: "institute_search_history",
                column: "StudentId",
                principalTable: "students",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_institute_search_history_users_user_id",
                table: "institute_search_history",
                column: "user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_institute_search_history_institutes_institute_id",
                table: "institute_search_history");

            migrationBuilder.DropForeignKey(
                name: "FK_institute_search_history_students_StudentId",
                table: "institute_search_history");

            migrationBuilder.DropForeignKey(
                name: "FK_institute_search_history_users_user_id",
                table: "institute_search_history");

            migrationBuilder.DropIndex(
                name: "IX_institute_search_history_StudentId",
                table: "institute_search_history");

            migrationBuilder.DropIndex(
                name: "ix_institute_search_history_user_id",
                table: "institute_search_history");

            migrationBuilder.DropIndex(
                name: "ix_institute_search_history_user_institute",
                table: "institute_search_history");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "institute_search_history");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "institute_search_history");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "institute_search_history",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "searched_at",
                table: "institute_search_history",
                newName: "SearchedAt");

            migrationBuilder.RenameColumn(
                name: "institute_id",
                table: "institute_search_history",
                newName: "InstituteId");

            migrationBuilder.RenameIndex(
                name: "IX_institute_search_history_institute_id",
                table: "institute_search_history",
                newName: "IX_institute_search_history_InstituteId");

            migrationBuilder.AlterColumn<Guid>(
                name: "StudentId",
                table: "institute_search_history",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SearchedAt",
                table: "institute_search_history",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateIndex(
                name: "IX_institute_search_history_SearchedAt",
                table: "institute_search_history",
                column: "SearchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_institute_search_history_StudentId_InstituteId",
                table: "institute_search_history",
                columns: new[] { "StudentId", "InstituteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_institute_search_history_StudentId_SearchedAt",
                table: "institute_search_history",
                columns: new[] { "StudentId", "SearchedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_institute_search_history_institutes_InstituteId",
                table: "institute_search_history",
                column: "InstituteId",
                principalTable: "institutes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_institute_search_history_students_StudentId",
                table: "institute_search_history",
                column: "StudentId",
                principalTable: "students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
