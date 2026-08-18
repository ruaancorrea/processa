using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Processa.Modules.Processos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CriarProcessos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "processos");

            migrationBuilder.CreateTable(
                name: "campos_personalizados",
                schema: "processos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoProcessoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    opcoes = table.Column<string>(type: "jsonb", nullable: false),
                    Obrigatorio = table.Column<bool>(type: "boolean", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campos_personalizados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fluxos",
                schema: "processos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoProcessoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FluxoPadrao = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fluxos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tipos_processo",
                schema: "processos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResponsavelObrigatorio = table.Column<bool>(type: "boolean", nullable: false),
                    ModoAtribuicao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ResponsavelFixoId = table.Column<Guid>(type: "uuid", nullable: true),
                    permissoes_inicio = table.Column<string>(type: "jsonb", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_processo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_campos_personalizados_TenantId_TipoProcessoId",
                schema: "processos",
                table: "campos_personalizados",
                columns: new[] { "TenantId", "TipoProcessoId" });

            migrationBuilder.CreateIndex(
                name: "IX_fluxos_TenantId_TipoProcessoId",
                schema: "processos",
                table: "fluxos",
                columns: new[] { "TenantId", "TipoProcessoId" });

            migrationBuilder.CreateIndex(
                name: "IX_fluxos_TipoProcessoId",
                schema: "processos",
                table: "fluxos",
                column: "TipoProcessoId",
                unique: true,
                filter: "\"FluxoPadrao\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_tipos_processo_TenantId_EquipeId",
                schema: "processos",
                table: "tipos_processo",
                columns: new[] { "TenantId", "EquipeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "campos_personalizados",
                schema: "processos");

            migrationBuilder.DropTable(
                name: "fluxos",
                schema: "processos");

            migrationBuilder.DropTable(
                name: "tipos_processo",
                schema: "processos");
        }
    }
}
