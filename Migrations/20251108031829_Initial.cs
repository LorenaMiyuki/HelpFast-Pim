using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpFast_Pim.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Cargos",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cargos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Faqs",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Pergunta = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Resposta = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faqs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Senha = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CargoId = table.Column<int>(type: "int", nullable: false),
                    Telefone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    UltimoLogin = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Cargos_CargoId",
                        column: x => x.CargoId,
                        principalSchema: "dbo",
                        principalTable: "Cargos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Chamados",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    TecnicoId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DataAbertura = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataFechamento = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chamados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chamados_Usuarios_ClienteId",
                        column: x => x.ClienteId,
                        principalSchema: "dbo",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Chamados_Usuarios_TecnicoId",
                        column: x => x.TecnicoId,
                        principalSchema: "dbo",
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Chats",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChamadoId = table.Column<int>(type: "int", nullable: true),
                    RemetenteId = table.Column<int>(type: "int", nullable: false),
                    DestinatarioId = table.Column<int>(type: "int", nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(max)", maxLength: 4000, nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chats_Chamados_ChamadoId",
                        column: x => x.ChamadoId,
                        principalSchema: "dbo",
                        principalTable: "Chamados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Chats_Usuarios_DestinatarioId",
                        column: x => x.DestinatarioId,
                        principalSchema: "dbo",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Chats_Usuarios_RemetenteId",
                        column: x => x.RemetenteId,
                        principalSchema: "dbo",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistoricoChamados",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChamadoId = table.Column<int>(type: "int", nullable: false),
                    Acao = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoChamados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoChamados_Chamados_ChamadoId",
                        column: x => x.ChamadoId,
                        principalSchema: "dbo",
                        principalTable: "Chamados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatIaResults",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChatId = table.Column<int>(type: "int", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatIaResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatIaResults_Chats_ChatId",
                        column: x => x.ChatId,
                        principalSchema: "dbo",
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cargos_Nome",
                schema: "dbo",
                table: "Cargos",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chamados_Cliente_Status_Data",
                schema: "dbo",
                table: "Chamados",
                columns: new[] { "ClienteId", "Status", "DataAbertura" });

            migrationBuilder.CreateIndex(
                name: "IX_Chamados_ClienteId",
                schema: "dbo",
                table: "Chamados",
                column: "ClienteId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Chamados_TecnicoId",
                schema: "dbo",
                table: "Chamados",
                column: "TecnicoId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatIaResults_ChatId",
                schema: "dbo",
                table: "ChatIaResults",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_Chamado_DataEnvio",
                schema: "dbo",
                table: "Chats",
                columns: new[] { "ChamadoId", "DataEnvio" });

            migrationBuilder.CreateIndex(
                name: "IX_Chats_DestinatarioId",
                schema: "dbo",
                table: "Chats",
                column: "DestinatarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_RemetenteId",
                schema: "dbo",
                table: "Chats",
                column: "RemetenteId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoChamados_ChamadoId",
                schema: "dbo",
                table: "HistoricoChamados",
                column: "ChamadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CargoId",
                schema: "dbo",
                table: "Usuarios",
                column: "CargoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                schema: "dbo",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatIaResults",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Faqs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "HistoricoChamados",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Chats",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Chamados",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Usuarios",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Cargos",
                schema: "dbo");
        }
    }
}
