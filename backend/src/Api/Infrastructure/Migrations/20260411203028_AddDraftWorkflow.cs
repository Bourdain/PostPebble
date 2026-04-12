using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE memberships SET \"Role\" = 'Reviewer' WHERE \"Role\" = 'Editor';");
            migrationBuilder.Sql("UPDATE memberships SET \"Role\" = 'Drafter' WHERE \"Role\" = 'Viewer';");

            migrationBuilder.DropForeignKey(
                name: "FK_scheduled_posts_credit_reservations_ReservationId",
                table: "scheduled_posts");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "memberships",
                newName: "JoinedAtUtc");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReservationId",
                table: "scheduled_posts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_scheduled_posts_credit_reservations_ReservationId",
                table: "scheduled_posts",
                column: "ReservationId",
                principalTable: "credit_reservations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_scheduled_posts_credit_reservations_ReservationId",
                table: "scheduled_posts");

            migrationBuilder.RenameColumn(
                name: "JoinedAtUtc",
                table: "memberships",
                newName: "CreatedAtUtc");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReservationId",
                table: "scheduled_posts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_scheduled_posts_credit_reservations_ReservationId",
                table: "scheduled_posts",
                column: "ReservationId",
                principalTable: "credit_reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql("UPDATE memberships SET \"Role\" = 'Editor' WHERE \"Role\" = 'Reviewer';");
            migrationBuilder.Sql("UPDATE memberships SET \"Role\" = 'Viewer' WHERE \"Role\" = 'Drafter';");
        }
    }
}
