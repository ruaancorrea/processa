using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Processa.Modules.Identidade.Infrastructure.Migracoes
{
    /// <inheritdoc />
    public partial class AdicionarEquipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "equipes",
                schema: "identidade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "membros_equipe",
                schema: "identidade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Papel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membros_equipe", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_equipes_TenantId",
                schema: "identidade",
                table: "equipes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_membros_equipe_EquipeId_UsuarioId",
                schema: "identidade",
                table: "membros_equipe",
                columns: new[] { "EquipeId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_membros_equipe_UsuarioId",
                schema: "identidade",
                table: "membros_equipe",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "equipes",
                schema: "identidade");

            migrationBuilder.DropTable(
                name: "membros_equipe",
                schema: "identidade");
        }
    }
}
