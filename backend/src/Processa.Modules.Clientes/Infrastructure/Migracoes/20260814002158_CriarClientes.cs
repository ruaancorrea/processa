using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Processa.Modules.Clientes.Infrastructure.Migracoes
{
    /// <inheritdoc />
    public partial class CriarClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "clientes");

            migrationBuilder.CreateTable(
                name: "clientes",
                schema: "clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RazaoSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    CodigoExterno = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GrupoClienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    RegimeTributario = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DataEntrada = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "contatos_cliente",
                schema: "clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Celular = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contatos_cliente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "grupos_cliente",
                schema: "clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupos_cliente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "responsaveis_cliente",
                schema: "clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RemovidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_responsaveis_cliente", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_clientes_TenantId_cnpj",
                schema: "clientes",
                table: "clientes",
                columns: new[] { "TenantId", "cnpj" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contatos_cliente_ClienteId",
                schema: "clientes",
                table: "contatos_cliente",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_grupos_cliente_TenantId",
                schema: "clientes",
                table: "grupos_cliente",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_responsaveis_cliente_ClienteId",
                schema: "clientes",
                table: "responsaveis_cliente",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_responsaveis_cliente_TenantId_ClienteId_UsuarioId_EquipeId",
                schema: "clientes",
                table: "responsaveis_cliente",
                columns: new[] { "TenantId", "ClienteId", "UsuarioId", "EquipeId" },
                unique: true,
                filter: "\"RemovidoEm\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clientes",
                schema: "clientes");

            migrationBuilder.DropTable(
                name: "contatos_cliente",
                schema: "clientes");

            migrationBuilder.DropTable(
                name: "grupos_cliente",
                schema: "clientes");

            migrationBuilder.DropTable(
                name: "responsaveis_cliente",
                schema: "clientes");
        }
    }
}
