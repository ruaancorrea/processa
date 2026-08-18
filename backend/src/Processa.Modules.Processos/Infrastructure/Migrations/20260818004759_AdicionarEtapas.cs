using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Processa.Modules.Processos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarEtapas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "etapas",
                schema: "processos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FluxoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    configuracao = table.Column<string>(type: "json", nullable: true),
                    configuracao_acesso = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_etapas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_etapas_TenantId_FluxoId",
                schema: "processos",
                table: "etapas",
                columns: new[] { "TenantId", "FluxoId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "etapas",
                schema: "processos");
        }
    }
}
