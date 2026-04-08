using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint2StripeDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "stripe_webhook_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stripe_webhook_events_TenantId",
                table: "stripe_webhook_events",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stripe_webhook_events_TenantId",
                table: "stripe_webhook_events");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "stripe_webhook_events");
        }
    }
}
