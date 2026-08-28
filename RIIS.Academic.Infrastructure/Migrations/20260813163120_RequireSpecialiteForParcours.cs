using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RIIS.Academic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RequireSpecialiteForParcours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CyclesFormationsFilieres_CycleFormationId_FiliereId_SpecialiteId",
                table: "CyclesFormationsFilieres");

            migrationBuilder.AlterColumn<long>(
                name: "SpecialiteId",
                table: "CyclesFormationsFilieres",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CyclesFormationsFilieres_CycleFormationId_FiliereId_SpecialiteId",
                table: "CyclesFormationsFilieres",
                columns: new[] { "CycleFormationId", "FiliereId", "SpecialiteId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CyclesFormationsFilieres_CycleFormationId_FiliereId_SpecialiteId",
                table: "CyclesFormationsFilieres");

            migrationBuilder.AlterColumn<long>(
                name: "SpecialiteId",
                table: "CyclesFormationsFilieres",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateIndex(
                name: "IX_CyclesFormationsFilieres_CycleFormationId_FiliereId_SpecialiteId",
                table: "CyclesFormationsFilieres",
                columns: new[] { "CycleFormationId", "FiliereId", "SpecialiteId" },
                unique: true,
                filter: "[SpecialiteId] IS NOT NULL");
        }
    }
}
