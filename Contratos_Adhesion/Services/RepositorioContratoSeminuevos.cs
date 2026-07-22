using System.Data;
using System.Data.SqlClient;
using Contratos_Adhesion.Models;
using Dapper;

namespace Contratos_Adhesion.Services
{
    public interface IRepositorioContratoSeminuevos
    {
        Task<ContratoSeminuevoDto?> ObtenerDatosContratoSeminuevoAsync(string ventaId, int negocio);
        Task GuardarContratoAsync(GuardarContratoSeminuevoDto dto, int negocio);
        Task<VentaMovDto?> ObtenerMovVentaAsync(string ventaId, int negocio);
    }

    public class RepositorioContratoSeminuevos : IRepositorioContratoSeminuevos
    {
        private readonly IDbConnectionFactory _factory; // ← reemplaza string conexionNegocio

        public RepositorioContratoSeminuevos(IDbConnectionFactory factory) // ← reemplaza IConfiguration
        {
            _factory = factory;
        }

        public async Task<ContratoSeminuevoDto?> ObtenerDatosContratoSeminuevoAsync(string ventaId, int negocio)
        {
            if (!int.TryParse(ventaId, out int idVenta))
                return null;

            const string sqlPrincipal = @"
SELECT
    -- Venta
    v.Condicion,
    v.MovID                                          AS Folio,
    CONVERT(varchar(10), v.FechaEmision, 23)         AS Fecha,
    v.ServicioSerie                                  AS Niv,
    CONVERT(varchar(10), v.FechaEntrega, 23)         AS FechaEntrega,

    -- Cliente
    c.Nombre                         AS NombreCliente,
    c.PersonalDireccion                      AS Calle,
    c.PersonalDireccionNumero        AS NumExt,
    c.PersonalDireccionNumInt        AS NumInt,
    c.PersonalDelegacion             AS Delegacion,
    c.PersonalColonia                AS Colonia,
    c.PersonalEstado                 AS Estado,
    c.PersonalTelefonos              AS Telefonos,
    c.eMail1                         AS Correo,
    c.RFC                            AS RfcCliente,
    c.PersonalCodigoPostal           AS CodigoPostal,

    -- Vehículo
    vi.Descripcion1              AS Marca,
    vi.Descripcion2              AS Submarca,
    vi.Descripcion3              AS TipoVersion,
    a.PedimentoClave             AS Capacidad,
    vi.Articulo                  AS Catalogo,
    vi.Modelo                    AS AnioModelo,
    vi.Km                        AS KmRecorridos,
    vi.Placas,

    -- Color
    vc.Descripcion               AS Color,

    -- Lugar de entrega / Localidad
    s.Direccion + ' ' + s.DireccionNumero + ', ' + s.Colonia + ', '
        + s.Delegacion + ' ' + s.CodigoPostal + ', ' + s.Estado AS LugarEntrega,

    -- Distribuidora — datos generales
    e.EmpresaNombreUIF           AS Denominacion,
    e.RFC                        AS Rfc,
    ISNULL(e.Direccion,'') + ' No. ' + ISNULL(e.DireccionNumero,'')
        + ', COLONIA ' + ISNULL(e.Colonia,'')
        + ', C.P.: '   + ISNULL(e.CodigoPostal,'')
        + ', '         + ISNULL(e.Poblacion,'')
        + ', '         + ISNULL(e.Estado,'')
        + ', '         + ISNULL(e.Pais,'')          AS Domicilio,
    e.Telefonos                  AS Telefono,
    s.Direccion + ' ' + s.DireccionNumero + ', ' + s.Colonia + ', '
        + s.Delegacion + ' ' + s.CodigoPostal + ', ' + s.Estado AS Localidad

FROM Venta v
LEFT JOIN Cte      c  ON c.Cliente       = v.Cliente
LEFT JOIN Vin      vi ON vi.Vin          = v.ServicioSerie
LEFT JOIN Art      a  ON a.Articulo      = vi.Articulo
LEFT JOIN VinColor vc ON vc.Color        = vi.ColorExterior
LEFT JOIN Sucursal s  ON s.Sucursal      = v.Sucursal
LEFT JOIN Empresa  e  ON e.Empresa       = v.Empresa
WHERE v.Id = @VentaId";

            const string sqlCostos = @"
SELECT
    CAST(
        CASE WHEN Cte.RFC NOT IN ('XAXX010101000','XEXX010101000')
            THEN (SELECT D.Precio + (D.Precio * (ISNULL(D.Impuesto2, 0) / 100))
                  FROM VentaD D
                  WHERE D.ID = Venta.ID AND D.Articulo = VentaD.Articulo)
            ELSE (SELECT (D.Precio + (D.Precio * (ISNULL(D.Impuesto2, 0) / 100))) * (1 + D.Impuesto1 / 100)
                  FROM VentaD D
                  WHERE D.ID = Venta.ID AND D.Articulo = VentaD.Articulo)
        END
    AS DECIMAL(18,2))                    AS PrecioVehiculo,
    CAST(
        CASE WHEN Cte.RFC NOT IN ('XAXX010101000','XEXX010101000')
            THEN (
                SELECT SUM(D.Precio + (D.Precio * (ISNULL(D.Impuesto2, 0) / 100)))
                FROM VentaD D
                JOIN Art ON Art.Articulo = D.Articulo
                WHERE D.ID = Venta.ID AND D.CantidadCancelada IS NULL AND Art.Tipo <> 'VIN'
            )
            ELSE (
                SELECT SUM((D.Precio + (D.Precio * (ISNULL(D.Impuesto2, 0) / 100))) * (1 + D.Impuesto1 / 100))
                FROM VentaD D
                JOIN Art ON Art.Articulo = D.Articulo
                WHERE D.ID = Venta.ID AND D.CantidadCancelada IS NULL AND Art.Tipo <> 'VIN'
            )
        END
    AS DECIMAL(18,2))                    AS Equipo,
    CAST(
        ISNULL(
            (SELECT SUM(D.Precio * (ISNULL(D.Impuesto2, 0) / 100))
             FROM VentaD D
             JOIN Art ON Art.Articulo = D.Articulo
             WHERE D.ID = Venta.ID AND D.CantidadCancelada IS NULL AND Art.Tipo <> 'VIN'),
        0)
    AS DECIMAL(18,2))                    AS OtrosCargos,
    CAST(
        CASE WHEN Cte.RFC NOT IN ('XAXX010101000','XEXX010101000')
            THEN (SELECT ROUND(SUM(Impuesto1Total), 2) FROM VentaTCalc WHERE ID = Venta.ID)
            ELSE 0
        END
    AS DECIMAL(18,2))                    AS Iva,
    CAST(ROUND(Venta.Importe + Venta.Impuestos, 2) AS DECIMAL(18,2)) AS MontoTotal,
    CAST(ROUND(Venta.Importe + Venta.Impuestos, 2) AS DECIMAL(18,2)) AS PagoContado,
    CAST(
        ISNULL(
            (SELECT SUM(CambioVINCosto) FROM VinEnajenados WHERE VIN = Venta.ServicioSerie),
        0)
    AS DECIMAL(18,2))                    AS Enganche
FROM Venta
JOIN Cte    ON Cte.Cliente = Venta.Cliente
JOIN VentaD ON VentaD.ID   = Venta.ID AND VentaD.CantidadCancelada IS NULL
WHERE Venta.ID = @VentaId";

            const string sqlUsada = @"
SELECT
    ve.CambioVin                             AS UsadaNiv,
    vi.Descripcion1                          AS UsadaMarca,
    vi.Descripcion2                          AS UsadaSubmarca,
    vi.Descripcion3                          AS UsadaVersion,
    vi.ColorExterior                         AS UsadaColor,
    vi.Modelo                                AS UsadaAnio,
    CAST(ve.CambioVINCosto AS DECIMAL(18,2)) AS UsadaValor
FROM VinEnajenados ve
INNER JOIN Vin vi ON vi.Vin = ve.CambioVin
WHERE ve.Vin = (SELECT ServicioSerie FROM Venta WHERE Id = @VentaId)";

            const string sqlFirmas = @"
            SELECT
                CONVERT(varchar(10), FechaEntrega, 23) AS FechaEntrega,
                CorreoDistribuidora,
                ExtLimpiaparabrisas, ExtLuces, ExtAntena, ExtEspejosLat, ExtCristales,
                ExtTapones, ExtMolduras, ExtTaponGas, ExtClaxon,
                IntInstrumentos, IntCalefaccion, IntAire, IntRadio, IntBocinas,
                IntEncendedor, IntEspejoRet, IntCeniceros, IntCinturones, IntTapetes,
                IntManijas, IntEquipoAd, IntAccesorios, IntOtros,
                MecLlantas, MecRuedas, MecRines, MecEscape, MecDireccion,
                MecSuspension, MecFrenos, MecParabrisas, MecCarroceria,
                Doc7Factura, Doc7Tarjeta, Doc7DocsOficiales, Doc7Manual,
                Doc7Tenencias, Doc7Verificacion, Doc7Multas,
                SinGarantia, ConGarantia,
                Doc10Factura, Doc10DocsOficiales, Doc10Constancia, Doc10Tenencias,
                Doc10Verificacion, Doc10Multas, Doc10Manual,
                FirmaDistribuidor, FirmaCliente
            FROM ContratoSeminuevoFirma
            WHERE IdVenta = @VentaId";
            using var conn = _factory.Create(negocio); // ← único cambio aquí

            var dto = await conn.QueryFirstOrDefaultAsync<ContratoSeminuevoDto>(sqlPrincipal, new { VentaId = idVenta });
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

            var firmas = await conn.QueryFirstOrDefaultAsync<GuardarContratoSeminuevoDto>(sqlFirmas, new { VentaId = idVenta });
            if (firmas is not null)
            {
                if (!string.IsNullOrEmpty(firmas.FechaEntrega))
                    dto.FechaEntrega = firmas.FechaEntrega;
                if (!string.IsNullOrEmpty(firmas.CorreoDistribuidora))
                    dto.CorreoDistribuidora = firmas.CorreoDistribuidora;

                dto.FirmaDistribuidor = firmas.FirmaDistribuidor;
                dto.FirmaCliente = firmas.FirmaCliente;
                dto.ExtLimpiaparabrisas = firmas.ExtLimpiaparabrisas;
                dto.ExtLuces = firmas.ExtLuces;
                dto.ExtAntena = firmas.ExtAntena;
                dto.ExtEspejosLat = firmas.ExtEspejosLat;
                dto.ExtCristales = firmas.ExtCristales;
                dto.ExtTapones = firmas.ExtTapones;
                dto.ExtMolduras = firmas.ExtMolduras;
                dto.ExtTaponGas = firmas.ExtTaponGas;
                dto.ExtClaxon = firmas.ExtClaxon;
                dto.IntInstrumentos = firmas.IntInstrumentos;
                dto.IntCalefaccion = firmas.IntCalefaccion;
                dto.IntAire = firmas.IntAire;
                dto.IntRadio = firmas.IntRadio;
                dto.IntBocinas = firmas.IntBocinas;
                dto.IntEncendedor = firmas.IntEncendedor;
                dto.IntEspejoRet = firmas.IntEspejoRet;
                dto.IntCeniceros = firmas.IntCeniceros;
                dto.IntCinturones = firmas.IntCinturones;
                dto.IntTapetes = firmas.IntTapetes;
                dto.IntManijas = firmas.IntManijas;
                dto.IntEquipoAd = firmas.IntEquipoAd;
                dto.IntAccesorios = firmas.IntAccesorios;
                dto.IntOtros = firmas.IntOtros;
                dto.MecLlantas = firmas.MecLlantas;
                dto.MecRuedas = firmas.MecRuedas;
                dto.MecRines = firmas.MecRines;
                dto.MecEscape = firmas.MecEscape;
                dto.MecDireccion = firmas.MecDireccion;
                dto.MecSuspension = firmas.MecSuspension;
                dto.MecFrenos = firmas.MecFrenos;
                dto.MecParabrisas = firmas.MecParabrisas;
                dto.MecCarroceria = firmas.MecCarroceria;
                dto.Doc7Factura = firmas.Doc7Factura;
                dto.Doc7Tarjeta = firmas.Doc7Tarjeta;
                dto.Doc7DocsOficiales = firmas.Doc7DocsOficiales;
                dto.Doc7Manual = firmas.Doc7Manual;
                dto.Doc7Tenencias = firmas.Doc7Tenencias;
                dto.Doc7Verificacion = firmas.Doc7Verificacion;
                dto.Doc7Multas = firmas.Doc7Multas;
                dto.SinGarantia = firmas.SinGarantia;
                dto.ConGarantia = firmas.ConGarantia;
                dto.Doc10Factura = firmas.Doc10Factura;
                dto.Doc10DocsOficiales = firmas.Doc10DocsOficiales;
                dto.Doc10Constancia = firmas.Doc10Constancia;
                dto.Doc10Tenencias = firmas.Doc10Tenencias;
                dto.Doc10Verificacion = firmas.Doc10Verificacion;
                dto.Doc10Multas = firmas.Doc10Multas;
                dto.Doc10Manual = firmas.Doc10Manual;
                dto.SharePointUniqueId = firmas.SharePointUniqueId;
            }

            return dto;
        }

        private static string FormatearMoneda(decimal? valor) =>
            valor.HasValue ? valor.Value.ToString("C", new System.Globalization.CultureInfo("es-MX")) : "$0.00";

        public async Task GuardarContratoAsync(GuardarContratoSeminuevoDto dto, int negocio)
        {
            const string sql = @"
            IF EXISTS (SELECT 1 FROM ContratoSeminuevoFirma WHERE IdVenta = @IdVenta)
                UPDATE ContratoSeminuevoFirma
                SET
                    FechaEntrega        = @FechaEntrega,
                    CorreoDistribuidora = @CorreoDistribuidora,
                    ExtLimpiaparabrisas = @ExtLimpiaparabrisas,
                    ExtLuces            = @ExtLuces,
                    ExtAntena           = @ExtAntena,
                    ExtEspejosLat       = @ExtEspejosLat,
                    ExtCristales        = @ExtCristales,
                    ExtTapones          = @ExtTapones,
                    ExtMolduras         = @ExtMolduras,
                    ExtTaponGas         = @ExtTaponGas,
                    ExtClaxon           = @ExtClaxon,
                    IntInstrumentos     = @IntInstrumentos,
                    IntCalefaccion      = @IntCalefaccion,
                    IntAire             = @IntAire,
                    IntRadio            = @IntRadio,
                    IntBocinas          = @IntBocinas,
                    IntEncendedor       = @IntEncendedor,
                    IntEspejoRet        = @IntEspejoRet,
                    IntCeniceros        = @IntCeniceros,
                    IntCinturones       = @IntCinturones,
                    IntTapetes          = @IntTapetes,
                    IntManijas          = @IntManijas,
                    IntEquipoAd         = @IntEquipoAd,
                    IntAccesorios       = @IntAccesorios,
                    IntOtros            = @IntOtros,
                    MecLlantas          = @MecLlantas,
                    MecRuedas           = @MecRuedas,
                    MecRines            = @MecRines,
                    MecEscape           = @MecEscape,
                    MecDireccion        = @MecDireccion,
                    MecSuspension       = @MecSuspension,
                    MecFrenos           = @MecFrenos,
                    MecParabrisas       = @MecParabrisas,
                    MecCarroceria       = @MecCarroceria,
                    Doc7Factura         = @Doc7Factura,
                    Doc7Tarjeta         = @Doc7Tarjeta,
                    Doc7DocsOficiales   = @Doc7DocsOficiales,
                    Doc7Manual          = @Doc7Manual,
                    Doc7Tenencias       = @Doc7Tenencias,
                    Doc7Verificacion    = @Doc7Verificacion,
                    Doc7Multas          = @Doc7Multas,
                    SinGarantia         = @SinGarantia,
                    ConGarantia         = @ConGarantia,
                    Doc10Factura        = @Doc10Factura,
                    Doc10DocsOficiales  = @Doc10DocsOficiales,
                    Doc10Constancia     = @Doc10Constancia,
                    Doc10Tenencias      = @Doc10Tenencias,
                    Doc10Verificacion   = @Doc10Verificacion,
                    Doc10Multas         = @Doc10Multas,
                    Doc10Manual         = @Doc10Manual,
                    FirmaDistribuidor   = @FirmaDistribuidor,
                    FirmaCliente        = @FirmaCliente
                WHERE IdVenta = @IdVenta
            ELSE
                INSERT INTO ContratoSeminuevoFirma
                (
                    IdVenta, FechaEntrega, CorreoDistribuidora,
                    ExtLimpiaparabrisas, ExtLuces, ExtAntena, ExtEspejosLat, ExtCristales,
                    ExtTapones, ExtMolduras, ExtTaponGas, ExtClaxon,
                    IntInstrumentos, IntCalefaccion, IntAire, IntRadio, IntBocinas,
                    IntEncendedor, IntEspejoRet, IntCeniceros, IntCinturones, IntTapetes,
                    IntManijas, IntEquipoAd, IntAccesorios, IntOtros,
                    MecLlantas, MecRuedas, MecRines, MecEscape, MecDireccion,
                    MecSuspension, MecFrenos, MecParabrisas, MecCarroceria,
                    Doc7Factura, Doc7Tarjeta, Doc7DocsOficiales, Doc7Manual,
                    Doc7Tenencias, Doc7Verificacion, Doc7Multas,
                    SinGarantia, ConGarantia,
                    Doc10Factura, Doc10DocsOficiales, Doc10Constancia, Doc10Tenencias,
                    Doc10Verificacion, Doc10Multas, Doc10Manual,
                    FirmaDistribuidor, FirmaCliente
                )
                VALUES
                (
                    @IdVenta, @FechaEntrega, @CorreoDistribuidora,
                    @ExtLimpiaparabrisas, @ExtLuces, @ExtAntena, @ExtEspejosLat, @ExtCristales,
                    @ExtTapones, @ExtMolduras, @ExtTaponGas, @ExtClaxon,
                    @IntInstrumentos, @IntCalefaccion, @IntAire, @IntRadio, @IntBocinas,
                    @IntEncendedor, @IntEspejoRet, @IntCeniceros, @IntCinturones, @IntTapetes,
                    @IntManijas, @IntEquipoAd, @IntAccesorios, @IntOtros,
                    @MecLlantas, @MecRuedas, @MecRines, @MecEscape, @MecDireccion,
                    @MecSuspension, @MecFrenos, @MecParabrisas, @MecCarroceria,
                    @Doc7Factura, @Doc7Tarjeta, @Doc7DocsOficiales, @Doc7Manual,
                    @Doc7Tenencias, @Doc7Verificacion, @Doc7Multas,
                    @SinGarantia, @ConGarantia,
                    @Doc10Factura, @Doc10DocsOficiales, @Doc10Constancia, @Doc10Tenencias,
                    @Doc10Verificacion, @Doc10Multas, @Doc10Manual,
                    @FirmaDistribuidor, @FirmaCliente
                )";

            using var conn = _factory.Create(negocio); // ← único cambio aquí
            await conn.ExecuteAsync(sql, dto);
        }

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