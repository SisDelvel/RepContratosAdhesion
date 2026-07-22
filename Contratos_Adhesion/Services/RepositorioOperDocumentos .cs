using System.Data;
using System.Data.SqlClient;
using Contratos_Adhesion.Models;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Contratos_Adhesion.Services
{
    public interface IRepositorioOperDocumentos
    {
        Task GuardarDocumentoAsync(GuardarDocumentoOperDto dto, string mov, string movId, string tipoDocumento);
    }

    public class RepositorioOperDocumentos : IRepositorioOperDocumentos
    {
        private readonly IDbConnectionFactory _factory;
        private readonly ILogger<RepositorioOperDocumentos> _logger;

        public RepositorioOperDocumentos(IDbConnectionFactory factory, ILogger<RepositorioOperDocumentos> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        private class RelacionDocDto
        {
            public int Id { get; set; }
            public int IdOperDocumentosXTipo { get; set; }
        }

        public async Task GuardarDocumentoAsync(GuardarDocumentoOperDto dto, string mov, string movId, string tipoDocumento)
        {
            _logger.LogInformation(
                "GuardarDocumentoAsync iniciado. IdVenta={IdVenta}, Mov='{Mov}', MovId='{MovId}', TipoDocumento='{TipoDocumento}'",
                dto.IdVenta, mov, movId, tipoDocumento);

            using var conn = (SqlConnection)_factory.CreateGrupo();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // ── 1) OperTempVenta — upsert por IdVenta ──────────────────
                const string sqlBuscarTemp = "SELECT Id FROM OperTempVenta WHERE IdVenta = @IdVenta";
                var existingTempId = await conn.QueryFirstOrDefaultAsync<int?>(
                    sqlBuscarTemp, new { dto.IdVenta }, tx);

                int idOperTempVenta;

                if (existingTempId.HasValue)
                {
                    idOperTempVenta = existingTempId.Value;
                    _logger.LogInformation("OperTempVenta existente encontrado. Id={Id}", idOperTempVenta);

                    const string sqlUpdateTemp = @"
                        UPDATE OperTempVenta
                        SET MovId = @MovId, Mov = @Mov, Estatus = 1, FechaUltModif = GETDATE()
                        WHERE Id = @Id";
                    await conn.ExecuteAsync(sqlUpdateTemp,
                        new { MovId = movId, Mov = mov, Id = idOperTempVenta }, tx);
                }
                else
                {
                    const string sqlInsertTemp = @"
                        INSERT INTO OperTempVenta (IdVenta, MovId, Mov, Estatus, FechaAlta)
                        OUTPUT INSERTED.Id
                        VALUES (@IdVenta, @MovId, @Mov, 1, GETDATE())";
                    idOperTempVenta = await conn.QuerySingleAsync<int>(sqlInsertTemp,
                        new { dto.IdVenta, MovId = movId, Mov = mov }, tx);

                    _logger.LogInformation("OperTempVenta insertado. Id={Id}", idOperTempVenta);
                }

                // ── 2) Catálogos — con validación explícita ─────────────────
                const string sqlTipoDoc = @"
                    SELECT Id FROM ComuCatTiposDocumentoOper
                    WHERE LTRIM(RTRIM(Tipo)) = LTRIM(RTRIM(@Tipo))";
                var idTiposDocumentoOper = await conn.QueryFirstOrDefaultAsync<int?>(
                    sqlTipoDoc, new { Tipo = tipoDocumento }, tx);

                if (idTiposDocumentoOper is null)
                {
                    _logger.LogError(
                        "No se encontró catálogo ComuCatTiposDocumentoOper con Tipo='{TipoDocumento}'.",
                        tipoDocumento);
                    throw new InvalidOperationException(
                        $"No existe un registro en ComuCatTiposDocumentoOper con Tipo = '{tipoDocumento}'. " +
                        "Verifica el valor exacto en la tabla (mayúsculas/espacios).");
                }

                const string sqlTipoOper = @"
                    SELECT Id FROM ComuCatTiposOper
                    WHERE LTRIM(RTRIM(Tipo)) = LTRIM(RTRIM(@Mov))";
                var idTiposOper = await conn.QueryFirstOrDefaultAsync<int?>(
                    sqlTipoOper, new { Mov = mov }, tx);

                if (idTiposOper is null)
                {
                    _logger.LogError(
                        "No se encontró catálogo ComuCatTiposOper con Tipo='{Mov}' (derivado de Venta.Mov).",
                        mov);
                    throw new InvalidOperationException(
                        $"No existe un registro en ComuCatTiposOper con Tipo = '{mov}' (tomado de Venta.Mov). " +
                        "Verifica el valor exacto en la tabla (mayúsculas/espacios).");
                }

                _logger.LogInformation(
                    "Catálogos resueltos. IdTiposDocumentoOper={IdTiposDocumentoOper}, IdTiposOper={IdTiposOper}",
                    idTiposDocumentoOper, idTiposOper);

                // ── 2.1) Asegurar combinación válida en la tabla puente ─────
                const string sqlBuscarCombinacion = @"
                    SELECT 1
                    FROM ComuCatTiposDocumentoOperTiposOper
                    WHERE IdTiposDocumentoOper = @IdTiposDocumentoOper
                      AND IdTiposOper = @IdTiposOper";
                var combinacionExiste = await conn.QueryFirstOrDefaultAsync<int?>(
                    sqlBuscarCombinacion,
                    new { IdTiposDocumentoOper = idTiposDocumentoOper, IdTiposOper = idTiposOper },
                    tx);

                if (combinacionExiste is null)
                {
                    const string sqlInsertCombinacion = @"
                        INSERT INTO ComuCatTiposDocumentoOperTiposOper (IdTiposDocumentoOper, IdTiposOper)
                        VALUES (@IdTiposDocumentoOper, @IdTiposOper)";
                    await conn.ExecuteAsync(sqlInsertCombinacion,
                        new { IdTiposDocumentoOper = idTiposDocumentoOper, IdTiposOper = idTiposOper },
                        tx);

                    _logger.LogInformation(
                        "Combinación nueva registrada en ComuCatTiposDocumentoOperTiposOper: ({IdTiposDocumentoOper}, {IdTiposOper})",
                        idTiposDocumentoOper, idTiposOper);
                }

                // ── 3) OperDocumentosXVenta — ¿ya existe relación? ──────────
                const string sqlBuscarRelacion = @"
                    SELECT Id, IdOperDocumentosXTipo
                    FROM OperDocumentosXVenta
                    WHERE IdOperTempVenta = @IdOperTempVenta";
                var relacion = await conn.QueryFirstOrDefaultAsync<RelacionDocDto>(
                    sqlBuscarRelacion, new { IdOperTempVenta = idOperTempVenta }, tx);

                if (relacion is not null)
                {
                    _logger.LogInformation(
                        "Relación existente encontrada. IdOperDocumentosXVenta={IdVenta}, IdOperDocumentosXTipo={IdTipo}",
                        relacion.Id, relacion.IdOperDocumentosXTipo);

                    const string sqlUpdateDoc = @"
                        UPDATE OperDocumentosXTipo
                        SET IdTiposDocumentoOper = @IdTiposDocumentoOper,
                            IdTiposOper = @IdTiposOper,
                            Archivo = @Archivo,
                            Url = @Url,
                            IdSharePoint = @IdSharePoint,
                            MimeType = @MimeType,
                            Tmano = @Tamano,
                            IdEstadoXDocumento = 2,
                            Estatus = 1,
                            FechaUltModif = GETDATE()
                        WHERE Id = @Id";
                    await conn.ExecuteAsync(sqlUpdateDoc, new
                    {
                        IdTiposDocumentoOper = idTiposDocumentoOper,
                        IdTiposOper = idTiposOper,
                        dto.Archivo,
                        dto.Url,
                        dto.IdSharePoint,
                        dto.MimeType,
                        dto.Tamano,
                        Id = relacion.IdOperDocumentosXTipo
                    }, tx);

                    const string sqlUpdateVenta = @"
                        UPDATE OperDocumentosXVenta
                        SET Fecha = GETDATE(), Estatus = 1, FechaUltModif = GETDATE()
                        WHERE Id = @Id";
                    await conn.ExecuteAsync(sqlUpdateVenta, new { Id = relacion.Id }, tx);

                    _logger.LogInformation(
                        "OperDocumentosXTipo y OperDocumentosXVenta actualizados. IdOperDocumentosXTipo={IdTipo}, IdOperDocumentosXVenta={IdVentaRel}",
                        relacion.IdOperDocumentosXTipo, relacion.Id);
                }
                else
                {
                    const string sqlInsertDoc = @"
                        INSERT INTO OperDocumentosXTipo
                            (IdTiposDocumentoOper, IdTiposOper, Archivo, Url, IdSharePoint, MimeType, Tmano, IdEstadoXDocumento, Estatus, FechaAlta)
                        OUTPUT INSERTED.Id
                        VALUES
                            (@IdTiposDocumentoOper, @IdTiposOper, @Archivo, @Url, @IdSharePoint, @MimeType, @Tamano, 2, 1, GETDATE())";
                    var idDoc = await conn.QuerySingleAsync<int>(sqlInsertDoc, new
                    {
                        IdTiposDocumentoOper = idTiposDocumentoOper,
                        IdTiposOper = idTiposOper,
                        dto.Archivo,
                        dto.Url,
                        dto.IdSharePoint,
                        dto.MimeType,
                        dto.Tamano
                    }, tx);

                    const string sqlInsertVenta = @"
                        INSERT INTO OperDocumentosXVenta
                            (IdOperTempVenta, IdOperDocumentosXTipo, Fecha, Observaciones, Estatus, FechaAlta)
                        VALUES
                            (@IdOperTempVenta, @IdOperDocumentosXTipo, GETDATE(), '', 1, GETDATE())";
                    await conn.ExecuteAsync(sqlInsertVenta, new
                    {
                        IdOperTempVenta = idOperTempVenta,
                        IdOperDocumentosXTipo = idDoc
                    }, tx);

                    _logger.LogInformation(
                        "Nueva relación creada. IdOperDocumentosXTipo={IdDoc}, IdOperTempVenta={IdTemp}",
                        idDoc, idOperTempVenta);
                }

                tx.Commit();
                _logger.LogInformation("GuardarDocumentoAsync completado exitosamente para IdVenta={IdVenta}", dto.IdVenta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GuardarDocumentoAsync para IdVenta={IdVenta}", dto.IdVenta);
                tx.Rollback();
                throw;
            }
        }
    }
}