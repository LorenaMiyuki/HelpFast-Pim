using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpFast_Pim.Migrations
{
    /// <inheritdoc />
    public partial class AumentarTamanhoMensagem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Mensagem",
                schema: "dbo",
                table: "Chats",
                type: "nvarchar(max)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Mensagem",
                schema: "dbo",
                table: "Chats",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 4000);
        }
    }
}
