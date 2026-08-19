using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Processa.Modules.Processos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarDemandas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "anexos_execucao",
                schema: "processos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecucaoEtapaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeOriginal = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NomeArmazenado = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CaminhoStorage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anexos_execucao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "comentarios_execucao",
                schema: "processos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecucaoEtapaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Texto = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comentarios_execucao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "demandas",
                schema: "processos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoProcessoId = table.Column<Guid>(type: "uuid", nullable: false),
                    FluxoAtivoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponsavelId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Prioridade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PercentualConclusao = table.Column<int>(type: "integer", nullable: false),
                    DataInicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DataFimPrevista = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DataFimReal = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EtapaAtualId = table.Column<Guid>(type: "uuid", nullable: true),
                    DemandaPaiId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_demandas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "desdobramentos_aguardados",
                schema: "processos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecucaoEtapaUniaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecucaoEtapaCondicionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Concluido = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_desdobramentos_aguardados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "execucao_etapas",
                schema: "processos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DemandaId = table.Column<Guid>(type: "uuid", nullable: false),
                    EtapaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponsavelId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IniciadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dados_execucao = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_execucao_etapas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "historico_execucao_etapa",
                schema: "processos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecucaoEtapaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoEvento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Dados = table.Column<string>(type: "jsonb", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historico_execucao_etapa", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_anexos_execucao_ExecucaoEtapaId",
                schema: "processos",
                table: "anexos_execucao",
                column: "ExecucaoEtapaId");

            migrationBuilder.CreateIndex(
                name: "IX_comentarios_execucao_ExecucaoEtapaId",
                schema: "processos",
                table: "comentarios_execucao",
                column: "ExecucaoEtapaId");

            migrationBuilder.CreateIndex(
                name: "IX_demandas_TenantId_ClienteId",
                schema: "processos",
                table: "demandas",
                columns: new[] { "TenantId", "ClienteId" });

            migrationBuilder.CreateIndex(
                name: "IX_demandas_TenantId_ResponsavelId",
                schema: "processos",
                table: "demandas",
                columns: new[] { "TenantId", "ResponsavelId" });

            migrationBuilder.CreateIndex(
                name: "IX_demandas_TenantId_Status",
                schema: "processos",
                table: "demandas",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_demandas_TenantId_TipoProcessoId",
                schema: "processos",
                table: "demandas",
                columns: new[] { "TenantId", "TipoProcessoId" });

            migrationBuilder.CreateIndex(
                name: "IX_desdobramentos_aguardados_ExecucaoEtapaCondicionalId",
                schema: "processos",
                table: "desdobramentos_aguardados",
                column: "ExecucaoEtapaCondicionalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_desdobramentos_aguardados_ExecucaoEtapaUniaoId",
                schema: "processos",
                table: "desdobramentos_aguardados",
                column: "ExecucaoEtapaUniaoId");

            migrationBuilder.CreateIndex(
                name: "IX_execucao_etapas_TenantId_DemandaId",
                schema: "processos",
                table: "execucao_etapas",
                columns: new[] { "TenantId", "DemandaId" });

            migrationBuilder.CreateIndex(
                name: "IX_execucao_etapas_TenantId_DemandaId_EtapaId",
                schema: "processos",
                table: "execucao_etapas",
                columns: new[] { "TenantId", "DemandaId", "EtapaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_historico_execucao_etapa_ExecucaoEtapaId",
                schema: "processos",
                table: "historico_execucao_etapa",
                column: "ExecucaoEtapaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anexos_execucao",
                schema: "processos");

            migrationBuilder.DropTable(
                name: "comentarios_execucao",
                schema: "processos");

            migrationBuilder.DropTable(
                name: "demandas",
                schema: "processos");

            migrationBuilder.DropTable(
                name: "desdobramentos_aguardados",
                schema: "processos");

            migrationBuilder.DropTable(
                name: "execucao_etapas",
                schema: "processos");

            migrationBuilder.DropTable(
                name: "historico_execucao_etapa",
                schema: "processos");
        }
    }
}
