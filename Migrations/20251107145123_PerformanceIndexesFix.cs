using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpFast_Pim.Migrations
{
    /// <inheritdoc />
    public partial class PerformanceIndexesFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Chamados_ChamadoId1",
                schema: "dbo",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Chat_ChamadoId",
                schema: "dbo",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Chats_ChamadoId1",
                schema: "dbo",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "ChamadoId1",
                schema: "dbo",
                table: "Chats");

            migrationBuilder.RenameIndex(
                name: "IX_Chat_RemetenteId",
                schema: "dbo",
                table: "Chats",
                newName: "IX_Chats_RemetenteId");

            migrationBuilder.RenameIndex(
                name: "IX_Chat_DestinatarioId",
                schema: "dbo",
                table: "Chats",
                newName: "IX_Chats_DestinatarioId");

            migrationBuilder.RenameIndex(
                name: "IX_Chat_ChamadoId_DataEnvio",
                schema: "dbo",
                table: "Chats",
                newName: "IX_Chats_Chamado_DataEnvio");

            migrationBuilder.CreateIndex(
                name: "IX_Chamados_Cliente_Status_Data",
                schema: "dbo",
                table: "Chamados",
                columns: new[] { "ClienteId", "Status", "DataAbertura" });

            migrationBuilder.CreateIndex(
                name: "IX_Chamados_Status",
                schema: "dbo",
                table: "Chamados",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Chamados_Tecnico_Status_Data",
                schema: "dbo",
                table: "Chamados",
                columns: new[] { "TecnicoId", "Status", "DataAbertura" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Chamados_Cliente_Status_Data",
                schema: "dbo",
                table: "Chamados");

            migrationBuilder.DropIndex(
                name: "IX_Chamados_Status",
                schema: "dbo",
                table: "Chamados");

            migrationBuilder.DropIndex(
                name: "IX_Chamados_Tecnico_Status_Data",
                schema: "dbo",
                table: "Chamados");

            migrationBuilder.RenameIndex(
                name: "IX_Chats_RemetenteId",
                schema: "dbo",
                table: "Chats",
                newName: "IX_Chat_RemetenteId");

            migrationBuilder.RenameIndex(
                name: "IX_Chats_DestinatarioId",
                schema: "dbo",
                table: "Chats",
                newName: "IX_Chat_DestinatarioId");

            migrationBuilder.RenameIndex(
                name: "IX_Chats_Chamado_DataEnvio",
                schema: "dbo",
                table: "Chats",
                newName: "IX_Chat_ChamadoId_DataEnvio");

            migrationBuilder.AddColumn<int>(
                name: "ChamadoId1",
                schema: "dbo",
                table: "Chats",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chat_ChamadoId",
                schema: "dbo",
                table: "Chats",
                column: "ChamadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_ChamadoId1",
                schema: "dbo",
                table: "Chats",
                column: "ChamadoId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Chamados_ChamadoId1",
                schema: "dbo",
                table: "Chats",
                column: "ChamadoId1",
                principalSchema: "dbo",
                principalTable: "Chamados",
                principalColumn: "Id");
        }
    }
}
