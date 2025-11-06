using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpFast_Pim.Migrations
{
    /// <inheritdoc />
    public partial class AddChatTipoAndChamadoChats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_Chats_ChamadoId",
                schema: "dbo",
                table: "Chats",
                newName: "IX_Chat_ChamadoId");

            migrationBuilder.AddColumn<int>(
                name: "ChamadoId1",
                schema: "dbo",
                table: "Chats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                schema: "dbo",
                table: "Chats",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chat_ChamadoId_DataEnvio",
                schema: "dbo",
                table: "Chats",
                columns: new[] { "ChamadoId", "DataEnvio" });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Chamados_ChamadoId1",
                schema: "dbo",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Chat_ChamadoId_DataEnvio",
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

            migrationBuilder.DropColumn(
                name: "Tipo",
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
                name: "IX_Chat_ChamadoId",
                schema: "dbo",
                table: "Chats",
                newName: "IX_Chats_ChamadoId");
        }
    }
}
