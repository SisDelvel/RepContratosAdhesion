using System.Data;
using System.Data.SqlClient;
using Contratos_Adhesion.Models;
using Dapper;

namespace Contratos_Adhesion.Services
{
    public interface IRepositorioContratoNuevos
    {
        Task<ContratoDto?> ObtenerDatosContratoAsync(string ventaId, int negocio);
        Task GuardarContratoAsync(GuardarContratoNuevoDto dto, int negocio);
        Task<string?> ObtenerTipoContratoAsync(string idVenta, int negocio);
        Task<VentaMovDto?> ObtenerMovVentaAsync(string ventaId, int negocio); // ← nuevo
    }

    public class RepositorioContratoNuevos : IRepositorioContratoNuevos
    {
        private readonly IDbConnectionFactory _factory;

        public RepositorioContratoNuevos(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<ContratoDto?> ObtenerDatosContratoAsync(string ventaId, int negocio)
        {
            if (!int.TryParse(ventaId, out int idVenta))
                return null;

            const string sqlPrincipal = @"
        SELECT
            v.Condicion,
            v.MovID              AS Folio,
            CONVERT(varchar(10), v.FechaEmision, 23) AS Fecha,
            v.ServicioSerie      AS Niv,
            CONVERT(varchar(10), v.FechaEntrega, 23) AS FechaEntrega,

            -- Cliente
            c.Nombre             AS NombreCliente,
            c.PersonalDireccion          AS Calle,
            c.PersonalDireccionNumero    AS NumExt,
            c.PersonalDireccionNumInt    AS NumInt,
            c.PersonalDelegacion         AS Delegacion,
            c.PersonalColonia            AS Colonia,
            c.PersonalEstado             AS Estado,
            c.PersonalTelefonos          AS Telefonos,
            c.eMail1             AS Correo,
            c.RFC                AS RfcCliente,
            c.PersonalCodigoPostal       AS CodigoPostal,

            -- Vehículo
            a.Fabricante         AS Marca,
            a.NombreCorto        AS Submarca,
            a.Descripcion1       AS TipoVersion,
            a.PedimentoClave     AS Capacidad,
            vi.Modelo            AS AnioModelo,
            vi.Articulo          AS Catalogo,

            -- Color
            vc.Descripcion       AS Color,

            -- Lugar de entrega / Localidad
            s.Direccion + ' ' + s.DireccionNumero + ', ' + s.Colonia + ', ' + s.Delegacion + ' ' + s.CodigoPostal + ', ' + s.Estado AS LugarEntrega,

            -- Distribuidora — datos generales
            e.EmpresaNombreUIF   AS Denominacion,
            e.RFC                AS Rfc,
            ISNULL(e.Direccion,'') + ' No. ' + ISNULL(e.DireccionNumero,'')
                + ', COLONIA ' + ISNULL(e.Colonia,'')
                + ', C.P.: '   + ISNULL(e.CodigoPostal,'')
                + ', '         + ISNULL(e.Poblacion,'')
                + ', '         + ISNULL(e.Estado,'')
                + ', '         + ISNULL(e.Pais,'') AS Domicilio,
            e.Telefonos          AS Telefono,
            s.Direccion + ' ' + s.DireccionNumero + ', ' + s.Colonia + ', ' + s.Delegacion + ' ' + s.CodigoPostal + ', ' + s.Estado AS Localidad,

            -- Distribuidora — Persona Moral (Declaraciones del Vendedor)
            e.RepresentanteContrato                     AS DistribuidoraRepresentante,
            e.EscrituraPublica                          AS DistribuidoraEscrituraNumero,
            CONVERT(varchar(10), e.FechaEscritura, 103) AS DistribuidoraEscrituraFecha,
            e.NomNotario                                AS DistribuidoraNotarioNombre,
            e.NumNotaria                                AS DistribuidoraNotariaNumero,
            e.UbicacionNotaria                          AS DistribuidoraNotariaEstado,
            e.RegPubCom                                 AS DistribuidoraRegistroPublico,
            e.NumeroRPC                                 AS DistribuidoraRegistroNumero,
            CONVERT(varchar(10), e.FechaRPC, 103)       AS DistribuidoraRegistroFecha,
            e.NomNotarioRPC                             AS DistribuidoraNotariaRLNombre,
            e.NumNotariaRPC                             AS DistribuidoraNotariaRLNumero,
            e.UbicacionNotariaRPC                       AS DistribuidoraNotariaRLEstado,
            e.NumNotarial                               AS DistribuidoraTestimonio,
            e.INE                                       AS DistribuidoraIne,

            -- Persona Moral del Comprador — con soporte EndosarA
            CASE WHEN v.EndosarA IS NULL THEN c.NumeroActa
                 ELSE (SELECT NumeroActa        FROM Cte WHERE Cliente = v.EndosarA) END AS EscrituraNumero,

            CASE WHEN v.EndosarA IS NULL THEN CONVERT(varchar(10), c.FechaConstitucion, 103)
                 ELSE (SELECT CONVERT(varchar(10), FechaConstitucion, 103) FROM Cte WHERE Cliente = v.EndosarA) END AS EscrituraFecha,

            CASE WHEN v.EndosarA IS NULL THEN c.NomNotario
                 ELSE (SELECT NomNotario        FROM Cte WHERE Cliente = v.EndosarA) END AS NotarioNombre,

            CASE WHEN v.EndosarA IS NULL THEN c.NumeroNotaria
                 ELSE (SELECT NumeroNotaria     FROM Cte WHERE Cliente = v.EndosarA) END AS NotariaNumero,

            CASE WHEN v.EndosarA IS NULL THEN c.UbicacionNotaria
                 ELSE (SELECT UbicacionNotaria  FROM Cte WHERE Cliente = v.EndosarA) END AS NotariaEstado,

            CASE WHEN v.EndosarA IS NULL THEN c.RegPubCom
                 ELSE (SELECT RegPubCom         FROM Cte WHERE Cliente = v.EndosarA) END AS RegistroPublico,

            CASE WHEN v.EndosarA IS NULL THEN c.NumeroRPC
                 ELSE (SELECT NumeroRPC         FROM Cte WHERE Cliente = v.EndosarA) END AS RegistroNumero,

            CASE WHEN v.EndosarA IS NULL THEN CAST(DAY(c.FechaRPC)   AS varchar(2))
                 ELSE (SELECT CAST(DAY(FechaRPC)   AS varchar(2)) FROM Cte WHERE Cliente = v.EndosarA) END AS RegistroDia,

            CASE WHEN v.EndosarA IS NULL THEN CAST(MONTH(c.FechaRPC) AS varchar(2))
                 ELSE (SELECT CAST(MONTH(FechaRPC) AS varchar(2)) FROM Cte WHERE Cliente = v.EndosarA) END AS RegistroMes,

            CASE WHEN v.EndosarA IS NULL THEN CAST(YEAR(c.FechaRPC)  AS varchar(4))
                 ELSE (SELECT CAST(YEAR(FechaRPC)  AS varchar(4)) FROM Cte WHERE Cliente = v.EndosarA) END AS RegistroAnio,

            CASE WHEN v.EndosarA IS NULL THEN c.NumNotarial
                 ELSE (SELECT NumNotarial       FROM Cte WHERE Cliente = v.EndosarA) END AS TestimonioNotarial,

            CASE WHEN v.EndosarA IS NULL THEN c.NumNotariaRPC
                 ELSE (SELECT NumNotariaRPC     FROM Cte WHERE Cliente = v.EndosarA) END AS NotariaRLNumero,

            CASE WHEN v.EndosarA IS NULL THEN c.NomNotarioRPC
                 ELSE (SELECT NomNotarioRPC     FROM Cte WHERE Cliente = v.EndosarA) END AS NotariaRLNombre,

            CASE WHEN v.EndosarA IS NULL THEN c.UbicacionNotariaRPC
                 ELSE (SELECT UbicacionNotariaRPC FROM Cte WHERE Cliente = v.EndosarA) END AS NotariaRLEstado

        FROM Venta v
        LEFT JOIN Cte      c  ON c.Cliente   = v.Cliente
        LEFT JOIN Vin      vi ON vi.Vin       = v.ServicioSerie
        LEFT JOIN Art      a  ON a.Articulo   = vi.Articulo
        LEFT JOIN VinColor vc ON vc.Color     = vi.ColorExterior
        LEFT JOIN Sucursal s  ON s.Sucursal   = v.Sucursal
        LEFT JOIN Empresa  e  ON e.Empresa    = v.Empresa
        WHERE v.Id = @VentaId";

            const string sqlCostos = @"
        SELECT
            CAST(VentaD.Precio AS DECIMAL(18,2)) AS PrecioVehiculo,
            CAST(
                CASE WHEN Cte.RFC NOT IN ('XAXX010101000','XEXX010101000')
                    THEN (SELECT ROUND(SUM(V.Precio) + SUM(V.Impuesto2total), 2) FROM VentaTCalc V JOIN Art ON Art.Articulo = V.Articulo WHERE V.ID = Venta.ID AND Art.Tipo <> 'VIN')
                    ELSE (SELECT ROUND(SUM(V.Precio) + SUM(V.Impuesto1total) + SUM(V.Impuesto2total), 2) FROM VentaTCalc V JOIN Art ON Art.Articulo = V.Articulo WHERE V.ID = Venta.ID AND Art.Tipo <> 'VIN')
                END
            AS DECIMAL(18,2)) AS Equipo,
            CAST((SELECT ISNULL(SUM(impuesto2total), 0) FROM VentaTCalc WHERE ID = Venta.ID) AS DECIMAL(18,2)) AS OtrosCargos,
            CAST(CASE WHEN Cte.RFC NOT IN ('XAXX010101000','XEXX010101000') THEN (SELECT ROUND(SUM(Impuesto1Total), 2) FROM VentaTCalc WHERE ID = Venta.ID) ELSE 0 END AS DECIMAL(18,2)) AS Iva,
            CAST(ROUND(Venta.Importe + Venta.Impuestos, 2) AS DECIMAL(18,2)) AS MontoTotal,
            CAST(ROUND(Venta.Importe + Venta.Impuestos, 2) AS DECIMAL(18,2)) AS PagoContado,
            CAST(ISNULL((SELECT SUM(CambioVINCosto) FROM VinEnajenados WHERE VIN = Venta.ServicioSerie), 0) AS DECIMAL(18,2)) AS Enganche
        FROM Venta
        JOIN Cte    ON Cte.Cliente = Venta.Cliente
        JOIN VentaD ON VentaD.ID   = Venta.ID
        WHERE Venta.ID = @VentaId";

            const string sqlUsada = @"
        SELECT
            ve.CambioVin         AS UsadaNiv,
            vi.Descripcion1      AS UsadaMarca,
            vi.Descripcion2      AS UsadaSubmarca,
            vi.Descripcion3      AS UsadaVersion,
            vi.ColorExterior     AS UsadaColor,
            vi.Modelo            AS UsadaAnio,
            CAST(ve.CambioVINCosto AS DECIMAL(18,2)) AS UsadaValor
        FROM VinEnajenados ve
        INNER JOIN Vin vi ON vi.Vin = ve.CambioVin
        WHERE ve.Vin = (SELECT ServicioSerie FROM Venta WHERE Id = @VentaId)";

            const string sqlFirmas = @"
        SELECT
            CONVERT(varchar(10), FechaEntrega, 23) AS FechaEntrega,
            CesionDatos,
            PublicidadDatos,
            FirmaAutorizacion,
            FirmaVendedor,
            FirmaComprador,
            CorreoDistribuidora,
            SharePointUniqueId
        FROM ContratoNuevoFirma
        WHERE IdVenta = @VentaId";

            using var conn = _factory.Create(negocio); // ← único cambio aquí

            var dto = await conn.QueryFirstOrDefaultAsync<ContratoDto>(sqlPrincipal, new { VentaId = idVenta });
            if (dto is null) return null;

            var costos = await conn.QueryFirstOrDefaultAsync<CostosContratoDto>(sqlCostos, new { VentaId = idVenta });
            if (costos is not null)
            {
                dto.PrecioVehiculo = FormatearMoneda(costos.PrecioVehiculo);
                dto.Equipo = FormatearMoneda(costos.Equipo);
                dto.OtrosCargos = FormatearMoneda(costos.OtrosCargos);
                dto.Iva = FormatearMoneda(costos.Iva);
                dto.MontoTotal = FormatearMoneda(costos.MontoTotal);
                dto.PagoContado = FormatearMoneda(costos.PagoContado);
                dto.Enganche = FormatearMoneda(costos.Enganche);
            }

            var usada = await conn.QueryFirstOrDefaultAsync<UnidadUsadaDto>(sqlUsada, new { VentaId = idVenta });
            if (usada is not null)
            {
                dto.UsadaNiv = usada.UsadaNiv;
                dto.UsadaMarca = usada.UsadaMarca;
                dto.UsadaSubmarca = usada.UsadaSubmarca;
                dto.UsadaVersion = usada.UsadaVersion;
                dto.UsadaColor = usada.UsadaColor;
                dto.UsadaAnio = usada.UsadaAnio;
                dto.UsadaValor = FormatearMoneda(usada.UsadaValor);
            }

            var firmas = await conn.QueryFirstOrDefaultAsync<GuardarContratoNuevoDto>(sqlFirmas, new { VentaId = idVenta });
            if (firmas is not null)
            {
                if (!string.IsNullOrEmpty(firmas.FechaEntrega))
                    dto.FechaEntrega = firmas.FechaEntrega;
                if (!string.IsNullOrEmpty(firmas.CorreoDistribuidora))
                    dto.CorreoDistribuidora = firmas.CorreoDistribuidora;

                dto.CesionDatos = firmas.CesionDatos;
                dto.PublicidadDatos = firmas.PublicidadDatos;
                dto.FirmaAutorizacion = firmas.FirmaAutorizacion;
                dto.FirmaVendedor = firmas.FirmaVendedor;
                dto.FirmaComprador = firmas.FirmaComprador;
                dto.SharePointUniqueId = firmas.SharePointUniqueId;
            }

            return dto;
        }

        public async Task GuardarContratoAsync(GuardarContratoNuevoDto dto, int negocio)
        {
            const string sql = @"
            IF EXISTS (SELECT 1 FROM ContratoNuevoFirma WHERE IdVenta = @IdVenta)
                UPDATE ContratoNuevoFirma
                SET FechaEntrega        = @FechaEntrega,
                    CesionDatos         = @CesionDatos,
                    PublicidadDatos     = @PublicidadDatos,
                    FirmaAutorizacion   = @FirmaAutorizacion,
                    FirmaVendedor       = @FirmaVendedor,
                    FirmaComprador      = @FirmaComprador,
                    CorreoDistribuidora = @CorreoDistribuidora,
                    SharePointUniqueId  = @SharePointUniqueId
                WHERE IdVenta = @IdVenta
            ELSE
                INSERT INTO ContratoNuevoFirma
                    (IdVenta, FechaEntrega, CesionDatos, PublicidadDatos,
                     FirmaAutorizacion, FirmaVendedor, FirmaComprador,
                     CorreoDistribuidora, SharePointUniqueId)
                VALUES
                    (@IdVenta, @FechaEntrega, @CesionDatos, @PublicidadDatos,
                     @FirmaAutorizacion, @FirmaVendedor, @FirmaComprador,
                     @CorreoDistribuidora, @SharePointUniqueId)";

            using var conn = _factory.Create(negocio); // ← único cambio aquí
            await conn.ExecuteAsync(sql, dto);
        }

        public async Task<string?> ObtenerTipoContratoAsync(string idVenta, int negocio)
        {
            if (!int.TryParse(idVenta, out int id))
                return null;

            using var conn = _factory.Create(negocio); // ← único cambio aquí
            const string sql = "SELECT TOP 1 Mov FROM Venta WHERE Id = @Id";
            return await conn.QueryFirstOrDefaultAsync<string>(sql, new { Id = id });
        }

        private static string FormatearMoneda(decimal? valor) =>
            valor.HasValue ? valor.Value.ToString("C", new System.Globalization.CultureInfo("es-MX")) : "$0.00";

        public async Task<VentaMovDto?> ObtenerMovVentaAsync(string ventaId, int negocio)
        {
            if (!int.TryParse(ventaId, out int idVenta))
                return null;

            const string sql = "SELECT TOP 1 Mov, MovID AS MovId FROM Venta WHERE Id = @Id";

            using var conn = _factory.Create(negocio);
            return await conn.QueryFirstOrDefaultAsync<VentaMovDto>(sql, new { Id = idVenta });
        }
    }
}