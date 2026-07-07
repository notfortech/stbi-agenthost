#nullable disable
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioTechBI.AgentHostInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "agenthost");

            migrationBuilder.CreateTable(
                name: "Plans",
                schema: "agenthost",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlanType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreditsPerCycle = table.Column<int>(type: "int", nullable: false),
                    IsUnlimited = table.Column<bool>(type: "bit", nullable: false),
                    ResetFrequency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaximumRequestsPerMinute = table.Column<int>(type: "int", nullable: false),
                    MaximumConcurrentRequests = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantSubscriptions",
                schema: "agenthost",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TenantName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreditsRemaining = table.Column<int>(type: "int", nullable: false),
                    CreditsUsedThisCycle = table.Column<int>(type: "int", nullable: false),
                    CurrentCycleStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NextResetDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantSubscriptions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "agenthost",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsageRecords",
                schema: "agenthost",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlueprintId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExecutionTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    TokensUsed = table.Column<int>(type: "int", nullable: false),
                    CreditsConsumed = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageRecords_TenantSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalSchema: "agenthost",
                        principalTable: "TenantSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditTransactions",
                schema: "agenthost",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BalanceBefore = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditTransactions_TenantSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalSchema: "agenthost",
                        principalTable: "TenantSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionHistories",
                schema: "agenthost",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NewPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionHistories_Plans_NewPlanId",
                        column: x => x.NewPlanId,
                        principalSchema: "agenthost",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionHistories_Plans_PreviousPlanId",
                        column: x => x.PreviousPlanId,
                        principalSchema: "agenthost",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionHistories_TenantSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalSchema: "agenthost",
                        principalTable: "TenantSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Indexes
            migrationBuilder.CreateIndex(
                name: "IX_Plans_Name",
                schema: "agenthost",
                table: "Plans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_TenantId",
                schema: "agenthost",
                table: "TenantSubscriptions",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_PlanId",
                schema: "agenthost",
                table: "TenantSubscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageRecords_TenantId",
                schema: "agenthost",
                table: "UsageRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageRecords_Timestamp",
                schema: "agenthost",
                table: "UsageRecords",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_UsageRecords_SubscriptionId",
                schema: "agenthost",
                table: "UsageRecords",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_TenantId",
                schema: "agenthost",
                table: "CreditTransactions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_SubscriptionId",
                schema: "agenthost",
                table: "CreditTransactions",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionHistories_TenantId",
                schema: "agenthost",
                table: "SubscriptionHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionHistories_SubscriptionId",
                schema: "agenthost",
                table: "SubscriptionHistories",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionHistories_NewPlanId",
                schema: "agenthost",
                table: "SubscriptionHistories",
                column: "NewPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionHistories_PreviousPlanId",
                schema: "agenthost",
                table: "SubscriptionHistories",
                column: "PreviousPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SubscriptionHistories", schema: "agenthost");
            migrationBuilder.DropTable(name: "CreditTransactions",    schema: "agenthost");
            migrationBuilder.DropTable(name: "UsageRecords",          schema: "agenthost");
            migrationBuilder.DropTable(name: "TenantSubscriptions",   schema: "agenthost");
            migrationBuilder.DropTable(name: "Plans",                 schema: "agenthost");
        }
    }
}
