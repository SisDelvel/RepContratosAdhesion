using System.Data;
using System.Data.SqlClient;
using Contratos_Adhesion.Models;
using Dapper;

namespace Contratos_Adhesion.Services
{
    public interface IRepositorioContratoServicio
    {
        Task<ContratoServicioDto?> ObtenerDatosOrdenServicioAsync(string ventaId, int negocio);
        Task GuardarContratoAsync(GuardarContratoServicioDto dto, int negocio);
        Task<VentaMovDto?> ObtenerMovVentaAsync(string ventaId, int negocio);
    }

    public class RepositorioContratoServicio : IRepositorioContratoServicio
    {
        private readonly IDbConnectionFactory _factory;

        public RepositorioContratoServicio(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<ContratoServicioDto?> ObtenerDatosOrdenServicioAsync(string ventaId, int negocio)
        {
            if (!int.TryParse(ventaId, out int idVenta))
                return null;

            const string sqlPrincipal = @"
SELECT
    -- Venta / Orden
    v.Id                                             AS NumOrden,
    CONVERT(varchar(10), v.FechaEmision, 23)         AS FechaOrden,
    CONVERT(varchar(10), v.FechaRequerida, 23)       AS FechaRecepcion,
    v.HoraRequerida                                  AS HoraEntrega,
    CONVERT(varchar(10), v.FechaOriginal, 23)        AS FechaEntrega,
    CAST(v.Importe     AS DECIMAL(18,2))             AS Importe,
    CAST(v.Impuestos   AS DECIMAL(18,2))             AS Impuestos,
    CAST(v.PrecioTotal AS DECIMAL(18,2))             AS PrecioTotal,
    v.ServicioSerie                                  AS Niv,
    v.Comentarios,

    -- Asesor / Pirámide
    ag.Nombre                                        AS Asesor,
    ag.Telefonos                                     AS TelAsesor,
    ag.eMail                                         AS EmailAsesor,
    v.ServicioIdentificador + ' ' +
        CONVERT(varchar(10), v.ServicioNumero)       AS Piramide,

    -- Póliza de seguro
    (SELECT Propiedad FROM Prop
     WHERE Rama = 'Emp'
       AND Tipo = 'PolizaSeguros' + CONVERT(varchar(1), v.Sucursal)) AS PolizaNumero,

    -- Cliente
    c.Nombre                        AS NombreCliente,
    c.Direccion                     AS Calle,
    c.DireccionNumero               AS NumExt,
    c.PersonalDireccionNumInt       AS NumInt,
    c.Delegacion                    AS Delegacion,
    c.Colonia                       AS Colonia,
    c.Estado                        AS Estado,
    c.Telefonos                     AS Telefonos,
    c.eMail1                        AS Correo,
    c.RFC                           AS RfcCliente,
    c.PersonalCodigoPostal          AS CodigoPostal,

    -- Vehículo
    a.Fabricante                    AS Marca,
    a.Rama                          AS Submarca,
    a.Descripcion1                  AS TipoVersion,
    a.PedimentoClave                AS Capacidad,

    -- Color
    vc.Descripcion                  AS Color,

    -- Vin
    vi.Modelo                       AS AnioModelo,
    vi.Km                           AS KmRecorridos,
    vi.Placas,

    -- Distribuidora
    e.EmpresaNombreUIF              AS Denominacion,
    e.RFC                           AS Rfc,
    ISNULL(e.Direccion,'') + ' No. ' + ISNULL(e.DireccionNumero,'')
        + ', COLONIA ' + ISNULL(e.Colonia,'')
        + ', C.P.: '   + ISNULL(e.CodigoPostal,'')
        + ', '         + ISNULL(e.Poblacion,'')
        + ', '         + ISNULL(e.Estado,'')
        + ', '         + ISNULL(e.Pais,'')           AS Domicilio,
    e.Telefonos                     AS Telefono,

    -- Localidad (sucursal)
    s.Direccion + ' ' + s.DireccionNumero + ', ' + s.Colonia + ', '
        + s.Delegacion + ' ' + s.CodigoPostal + ', ' + s.Estado AS Localidad

FROM Venta v
LEFT JOIN Agente   ag ON ag.Agente      = v.Agente
LEFT JOIN Cte      c  ON c.Cliente      = v.Cliente
LEFT JOIN Vin      vi ON vi.Vin         = v.ServicioSerie
LEFT JOIN Art      a  ON a.Articulo     = vi.Articulo
LEFT JOIN VinColor vc ON vc.Color       = vi.ColorExterior
LEFT JOIN Sucursal s  ON s.Sucursal     = v.Sucursal
LEFT JOIN Empresa  e  ON e.Empresa      = v.Empresa
WHERE v.Id = @VentaId";

            const string sqlCostos = @"
SELECT
    CAST(
        ISNULL(
            (SELECT SUM(vd.Precio * vd.Cantidad)
             FROM VentaD vd
             WHERE vd.ID = @VentaId
               AND vd.Paquete = 0
               AND ISNULL(vd.CantidadCancelada, 0) = 0),
        0)
    AS DECIMAL(18,2)) AS TotalRefacciones,
    CAST(v.Impuestos AS DECIMAL(18,2)) AS Iva
FROM Venta v
WHERE v.Id = @VentaId";

            const string sqlOperaciones = @"
SELECT
    vd.Articulo,
    a.Descripcion1                                                AS Descripcion,
    CAST((vd.Cantidad - ISNULL(vd.CantidadCancelada, 0))
         AS DECIMAL(18,2))                                        AS Cantidad,
    CAST(vd.Precio AS DECIMAL(18,2))                              AS PrecioUnit,
    CAST(ROUND(vd.Precio * (vd.Cantidad - ISNULL(vd.CantidadCancelada, 0)), 2)
         AS DECIMAL(18,2))                                        AS Total
FROM Venta v
JOIN VentaD vd ON vd.ID      = v.ID
JOIN Art    a  ON a.Articulo = vd.Articulo
WHERE v.Id = @VentaId
  AND (vd.Cantidad - ISNULL(vd.CantidadCancelada, 0)) > 0";

            const string sqlFirmas = @"
            SELECT
                InvTapetes, InvCeniceros, InvBocinas, InvInstrumentos, InvEncendedores,
                InvRadio, InvClaxon, InvAC, InvRetrovisor, InvManijas, InvVestiduras,
                InvCinturones, InvManualProp, InvCarnetServicio,
                InvTarjetaCirculacion, InvPolizaSeguro, InvVerificacion, InvAlfombradoCaj,
                InvLlantaRefaccion, InvTriangulos, InvExtintor, InvCablesBateria,
                InvGato, InvHerramientas, InvBotiquin, InvRedProtectora, InvBirloSeguridad,
                ExtCristales, ExtLimpiadores, ExtTapones, ExtFarosNiebla, ExtAntena,
                ExtTaponGas, ExtMolduras, ExtEspejos, ExtFarosDelanteros, ExtLucesTraseras, ExtGolpes,
                TestLlantas, TestCheckEngine, TestVscrack, TestPresionAceite,
                TestControlEstabilidad, TestBolsasAire, TestBateria, TestTemperatura,
                NivelGasolina,
                C5ExtLimpiaparabrisas, C5ExtLuces, C5ExtAntena, C5ExtEspejos, C5ExtCristales,
                C5ExtTapones, C5ExtMolduras, C5ExtTaponGas, C5ExtClaxon,
                C5IntInstrumentos, C5IntCalefaccion, C5IntAire, C5IntRadio, C5IntBocinas,
                C5IntEncendedor, C5IntEspejoRet, C5IntCeniceros, C5IntCinturones, C5IntTapetes,
                C5IntManijas, C5IntEquipoAd, C5IntAccesorios, C5IntAditamentos, C5IntOtros,
                SinGarantia, ConGarantia,
                FirmaDistribuidor, FirmaCliente, FirmaExtra,
                PartesEntregaSi, PartesEntregaNo, PartesGarantia, PartesResiduos,
                ServicioDomicilioSi, ServicioDomicilioNo, PolizaSeguros, PolizaNoSeguros,
                DeseaContacto,
                Anticipo, Conductor, MedioContacto, TelContacto, FechaDofu, Katashiki
            FROM ContratoServicioFirma
            WHERE IdVenta = @VentaId";
            const string sqlDanos = @"
SELECT IdTipoDano, CoordX, CoordY
FROM ContratoServicio_Dano
WHERE IdVenta = @VentaId";

            using var conn = _factory.Create(negocio);

            var dto = await conn.QueryFirstOrDefaultAsync<ContratoServicioDto>(sqlPrincipal, new { VentaId = idVenta });
            if (dto is null) return null;

            var costos = await conn.QueryFirstOrDefaultAsync<CostosServicioDto>(sqlCostos, new { VentaId = idVenta });
            if (costos is not null)
            {
                dto.TotalRefacciones = costos.TotalRefacciones;
                dto.Iva = costos.Iva;
            }

            var operaciones = await conn.QueryAsync<LineaOperacionDto>(sqlOperaciones, new { VentaId = idVenta });
            dto.Operaciones = operaciones.ToList();

            var firmas = await conn.QueryFirstOrDefaultAsync<GuardarContratoServicioDto>(sqlFirmas, new { VentaId = idVenta });
            if (firmas is not null)
            {
                dto.InvTapetes = firmas.InvTapetes;
                dto.InvCeniceros = firmas.InvCeniceros;
                dto.InvBocinas = firmas.InvBocinas;
                dto.InvInstrumentos = firmas.InvInstrumentos;
                dto.InvEncendedores = firmas.InvEncendedores;
                dto.InvRadio = firmas.InvRadio;
                dto.InvClaxon = firmas.InvClaxon;
                dto.InvAC = firmas.InvAC;
                dto.InvRetrovisor = firmas.InvRetrovisor;
                dto.InvManijas = firmas.InvManijas;
                dto.InvVestiduras = firmas.InvVestiduras;
                dto.InvCinturones = firmas.InvCinturones;
                dto.InvManualProp = firmas.InvManualProp;
                dto.InvCarnetServicio = firmas.InvCarnetServicio;
                dto.InvTarjetaCirculacion = firmas.InvTarjetaCirculacion;
                dto.InvPolizaSeguro = firmas.InvPolizaSeguro;
                dto.InvVerificacion = firmas.InvVerificacion;
                dto.InvAlfombradoCaj = firmas.InvAlfombradoCaj;
                dto.InvLlantaRefaccion = firmas.InvLlantaRefaccion;
                dto.InvTriangulos = firmas.InvTriangulos;
                dto.InvExtintor = firmas.InvExtintor;
                dto.InvCablesBateria = firmas.InvCablesBateria;
                dto.InvGato = firmas.InvGato;
                dto.InvHerramientas = firmas.InvHerramientas;
                dto.InvBotiquin = firmas.InvBotiquin;
                dto.InvRedProtectora = firmas.InvRedProtectora;
                dto.InvBirloSeguridad = firmas.InvBirloSeguridad;
                dto.ExtCristales = firmas.ExtCristales;
                dto.ExtLimpiadores = firmas.ExtLimpiadores;
                dto.ExtTapones = firmas.ExtTapones;
                dto.ExtFarosNiebla = firmas.ExtFarosNiebla;
                dto.ExtAntena = firmas.ExtAntena;
                dto.ExtTaponGas = firmas.ExtTaponGas;
                dto.ExtMolduras = firmas.ExtMolduras;
                dto.ExtEspejos = firmas.ExtEspejos;
                dto.ExtFarosDelanteros = firmas.ExtFarosDelanteros;
                dto.ExtLucesTraseras = firmas.ExtLucesTraseras;
                dto.ExtGolpes = firmas.ExtGolpes;
                dto.TestLlantas = firmas.TestLlantas;
                dto.TestCheckEngine = firmas.TestCheckEngine;
                dto.TestVscrack = firmas.TestVscrack;
                dto.TestPresionAceite = firmas.TestPresionAceite;
                dto.TestControlEstabilidad = firmas.TestControlEstabilidad;
                dto.TestBolsasAire = firmas.TestBolsasAire;
                dto.TestBateria = firmas.TestBateria;
                dto.TestTemperatura = firmas.TestTemperatura;
                dto.NivelGasolina = firmas.NivelGasolina;
                dto.C5ExtLimpiaparabrisas = firmas.C5ExtLimpiaparabrisas;
                dto.C5ExtLuces = firmas.C5ExtLuces;
                dto.C5ExtAntena = firmas.C5ExtAntena;
                dto.C5ExtEspejos = firmas.C5ExtEspejos;
                dto.C5ExtCristales = firmas.C5ExtCristales;
                dto.C5ExtTapones = firmas.C5ExtTapones;
                dto.C5ExtMolduras = firmas.C5ExtMolduras;
                dto.C5ExtTaponGas = firmas.C5ExtTaponGas;
                dto.C5ExtClaxon = firmas.C5ExtClaxon;
                dto.C5IntInstrumentos = firmas.C5IntInstrumentos;
                dto.C5IntCalefaccion = firmas.C5IntCalefaccion;
                dto.C5IntAire = firmas.C5IntAire;
                dto.C5IntRadio = firmas.C5IntRadio;
                dto.C5IntBocinas = firmas.C5IntBocinas;
                dto.C5IntEncendedor = firmas.C5IntEncendedor;
                dto.C5IntEspejoRet = firmas.C5IntEspejoRet;
                dto.C5IntCeniceros = firmas.C5IntCeniceros;
                dto.C5IntCinturones = firmas.C5IntCinturones;
                dto.C5IntTapetes = firmas.C5IntTapetes;
                dto.C5IntManijas = firmas.C5IntManijas;
                dto.C5IntEquipoAd = firmas.C5IntEquipoAd;
                dto.C5IntAccesorios = firmas.C5IntAccesorios;
                dto.C5IntAditamentos = firmas.C5IntAditamentos;
                dto.C5IntOtros = firmas.C5IntOtros;
                dto.SinGarantia = firmas.SinGarantia;
                dto.ConGarantia = firmas.ConGarantia;
                dto.FirmaDistribuidor = firmas.FirmaDistribuidor;
                dto.FirmaCliente = firmas.FirmaCliente;
                dto.FirmaExtra = firmas.FirmaExtra;
                dto.PartesEntregaSi = firmas.PartesEntregaSi;
                dto.PartesEntregaNo = firmas.PartesEntregaNo;
                dto.PartesGarantia = firmas.PartesGarantia;
                dto.PartesResiduos = firmas.PartesResiduos;
                dto.ServicioDomicilioSi = firmas.ServicioDomicilioSi;
                dto.ServicioDomicilioNo = firmas.ServicioDomicilioNo;
                dto.PolizaSeguros = firmas.PolizaSeguros;
                dto.PolizaNoSeguros = firmas.PolizaNoSeguros;
                dto.DeseaContacto = firmas.DeseaContacto;
                dto.Anticipo = firmas.Anticipo;
                dto.SharePointUniqueId = firmas.SharePointUniqueId;

                if (!string.IsNullOrEmpty(firmas.Conductor)) dto.Conductor = firmas.Conductor;
                if (!string.IsNullOrEmpty(firmas.MedioContacto)) dto.MedioContacto = firmas.MedioContacto;
                if (!string.IsNullOrEmpty(firmas.TelContacto)) dto.TelContacto = firmas.TelContacto;
                if (!string.IsNullOrEmpty(firmas.FechaDofu)) dto.FechaDofu = firmas.FechaDofu;
                if (!string.IsNullOrEmpty(firmas.Katashiki)) dto.Katashiki = firmas.Katashiki;
            }

            var danos = await conn.QueryAsync<DanoServicioDto>(sqlDanos, new { VentaId = idVenta });
            dto.Danos = danos.ToList();

            return dto;
        }

        public async Task GuardarContratoAsync(GuardarContratoServicioDto dto, int negocio)
        {
            const string sqlFirma = @"
            IF EXISTS (SELECT 1 FROM ContratoServicioFirma WHERE IdVenta = @IdVenta)
                UPDATE ContratoServicioFirma SET
                    InvTapetes = @InvTapetes, InvCeniceros = @InvCeniceros, InvBocinas = @InvBocinas,
                    InvInstrumentos = @InvInstrumentos, InvEncendedores = @InvEncendedores, InvRadio = @InvRadio,
                    InvClaxon = @InvClaxon, InvAC = @InvAC, InvRetrovisor = @InvRetrovisor,
                    InvManijas = @InvManijas, InvVestiduras = @InvVestiduras, InvCinturones = @InvCinturones,
                    InvManualProp = @InvManualProp, InvCarnetServicio = @InvCarnetServicio,
                    InvTarjetaCirculacion = @InvTarjetaCirculacion, InvPolizaSeguro = @InvPolizaSeguro,
                    InvVerificacion = @InvVerificacion, InvAlfombradoCaj = @InvAlfombradoCaj,
                    InvLlantaRefaccion = @InvLlantaRefaccion, InvTriangulos = @InvTriangulos,
                    InvExtintor = @InvExtintor, InvCablesBateria = @InvCablesBateria,
                    InvGato = @InvGato, InvHerramientas = @InvHerramientas, InvBotiquin = @InvBotiquin,
                    InvRedProtectora = @InvRedProtectora, InvBirloSeguridad = @InvBirloSeguridad,
                    ExtCristales = @ExtCristales, ExtLimpiadores = @ExtLimpiadores, ExtTapones = @ExtTapones,
                    ExtFarosNiebla = @ExtFarosNiebla, ExtAntena = @ExtAntena, ExtTaponGas = @ExtTaponGas,
                    ExtMolduras = @ExtMolduras, ExtEspejos = @ExtEspejos, ExtFarosDelanteros = @ExtFarosDelanteros,
                    ExtLucesTraseras = @ExtLucesTraseras, ExtGolpes = @ExtGolpes,
                    TestLlantas = @TestLlantas, TestCheckEngine = @TestCheckEngine, TestVscrack = @TestVscrack,
                    TestPresionAceite = @TestPresionAceite, TestControlEstabilidad = @TestControlEstabilidad,
                    TestBolsasAire = @TestBolsasAire, TestBateria = @TestBateria, TestTemperatura = @TestTemperatura,
                    NivelGasolina = @NivelGasolina,
                    C5ExtLimpiaparabrisas = @C5ExtLimpiaparabrisas, C5ExtLuces = @C5ExtLuces, C5ExtAntena = @C5ExtAntena,
                    C5ExtEspejos = @C5ExtEspejos, C5ExtCristales = @C5ExtCristales, C5ExtTapones = @C5ExtTapones,
                    C5ExtMolduras = @C5ExtMolduras, C5ExtTaponGas = @C5ExtTaponGas, C5ExtClaxon = @C5ExtClaxon,
                    C5IntInstrumentos = @C5IntInstrumentos, C5IntCalefaccion = @C5IntCalefaccion, C5IntAire = @C5IntAire,
                    C5IntRadio = @C5IntRadio, C5IntBocinas = @C5IntBocinas, C5IntEncendedor = @C5IntEncendedor,
                    C5IntEspejoRet = @C5IntEspejoRet, C5IntCeniceros = @C5IntCeniceros, C5IntCinturones = @C5IntCinturones,
                    C5IntTapetes = @C5IntTapetes, C5IntManijas = @C5IntManijas, C5IntEquipoAd = @C5IntEquipoAd,
                    C5IntAccesorios = @C5IntAccesorios, C5IntAditamentos = @C5IntAditamentos, C5IntOtros = @C5IntOtros,
                    SinGarantia = @SinGarantia, ConGarantia = @ConGarantia,
                    FirmaDistribuidor = @FirmaDistribuidor, FirmaCliente = @FirmaCliente, FirmaExtra = @FirmaExtra,
                    PartesEntregaSi = @PartesEntregaSi, PartesEntregaNo = @PartesEntregaNo,
                    PartesGarantia = @PartesGarantia, PartesResiduos = @PartesResiduos,
                    ServicioDomicilioSi = @ServicioDomicilioSi, ServicioDomicilioNo = @ServicioDomicilioNo,
                    PolizaSeguros = @PolizaSeguros, PolizaNoSeguros = @PolizaNoSeguros,
                    DeseaContacto = @DeseaContacto,
                    Anticipo = @Anticipo, Conductor = @Conductor, MedioContacto = @MedioContacto,
                    TelContacto = @TelContacto, FechaDofu = @FechaDofu, Katashiki = @Katashiki
                WHERE IdVenta = @IdVenta
            ELSE
                INSERT INTO ContratoServicioFirma (
                    IdVenta,
                    InvTapetes, InvCeniceros, InvBocinas, InvInstrumentos, InvEncendedores, InvRadio,
                    InvClaxon, InvAC, InvRetrovisor, InvManijas, InvVestiduras, InvCinturones,
                    InvManualProp, InvCarnetServicio,
                    InvTarjetaCirculacion, InvPolizaSeguro, InvVerificacion, InvAlfombradoCaj,
                    InvLlantaRefaccion, InvTriangulos, InvExtintor, InvCablesBateria,
                    InvGato, InvHerramientas, InvBotiquin, InvRedProtectora, InvBirloSeguridad,
                    ExtCristales, ExtLimpiadores, ExtTapones, ExtFarosNiebla, ExtAntena, ExtTaponGas,
                    ExtMolduras, ExtEspejos, ExtFarosDelanteros, ExtLucesTraseras, ExtGolpes,
                    TestLlantas, TestCheckEngine, TestVscrack, TestPresionAceite, TestControlEstabilidad,
                    TestBolsasAire, TestBateria, TestTemperatura,
                    NivelGasolina,
                    C5ExtLimpiaparabrisas, C5ExtLuces, C5ExtAntena, C5ExtEspejos, C5ExtCristales,
                    C5ExtTapones, C5ExtMolduras, C5ExtTaponGas, C5ExtClaxon,
                    C5IntInstrumentos, C5IntCalefaccion, C5IntAire, C5IntRadio, C5IntBocinas,
                    C5IntEncendedor, C5IntEspejoRet, C5IntCeniceros, C5IntCinturones, C5IntTapetes,
                    C5IntManijas, C5IntEquipoAd, C5IntAccesorios, C5IntAditamentos, C5IntOtros,
                    SinGarantia, ConGarantia,
                    FirmaDistribuidor, FirmaCliente, FirmaExtra,
                    PartesEntregaSi, PartesEntregaNo, PartesGarantia, PartesResiduos,
                    ServicioDomicilioSi, ServicioDomicilioNo, PolizaSeguros, PolizaNoSeguros,
                    DeseaContacto,
                    Anticipo, Conductor, MedioContacto, TelContacto, FechaDofu, Katashiki
                ) VALUES (
                    @IdVenta,
                    @InvTapetes, @InvCeniceros, @InvBocinas, @InvInstrumentos, @InvEncendedores, @InvRadio,
                    @InvClaxon, @InvAC, @InvRetrovisor, @InvManijas, @InvVestiduras, @InvCinturones,
                    @InvManualProp, @InvCarnetServicio,
                    @InvTarjetaCirculacion, @InvPolizaSeguro, @InvVerificacion, @InvAlfombradoCaj,
                    @InvLlantaRefaccion, @InvTriangulos, @InvExtintor, @InvCablesBateria,
                    @InvGato, @InvHerramientas, @InvBotiquin, @InvRedProtectora, @InvBirloSeguridad,
                    @ExtCristales, @ExtLimpiadores, @ExtTapones, @ExtFarosNiebla, @ExtAntena, @ExtTaponGas,
                    @ExtMolduras, @ExtEspejos, @ExtFarosDelanteros, @ExtLucesTraseras, @ExtGolpes,
                    @TestLlantas, @TestCheckEngine, @TestVscrack, @TestPresionAceite, @TestControlEstabilidad,
                    @TestBolsasAire, @TestBateria, @TestTemperatura,
                    @NivelGasolina,
                    @C5ExtLimpiaparabrisas, @C5ExtLuces, @C5ExtAntena, @C5ExtEspejos, @C5ExtCristales,
                    @C5ExtTapones, @C5ExtMolduras, @C5ExtTaponGas, @C5ExtClaxon,
                    @C5IntInstrumentos, @C5IntCalefaccion, @C5IntAire, @C5IntRadio, @C5IntBocinas,
                    @C5IntEncendedor, @C5IntEspejoRet, @C5IntCeniceros, @C5IntCinturones, @C5IntTapetes,
                    @C5IntManijas, @C5IntEquipoAd, @C5IntAccesorios, @C5IntAditamentos, @C5IntOtros,
                    @SinGarantia, @ConGarantia,
                    @FirmaDistribuidor, @FirmaCliente, @FirmaExtra,
                    @PartesEntregaSi, @PartesEntregaNo, @PartesGarantia, @PartesResiduos,
                    @ServicioDomicilioSi, @ServicioDomicilioNo, @PolizaSeguros, @PolizaNoSeguros,
                    @DeseaContacto,
                    @Anticipo, @Conductor, @MedioContacto, @TelContacto, @FechaDofu, @Katashiki
                )";
            const string sqlDeleteDanos = @"
DELETE FROM ContratoServicio_Dano WHERE IdVenta = @IdVenta";

            const string sqlInsertDano = @"
INSERT INTO ContratoServicio_Dano (IdVenta, IdTipoDano, CoordX, CoordY)
VALUES (@IdVenta, @IdTipoDano, @CoordX, @CoordY)";

            using var conn = _factory.Create(negocio);
            conn.Open();
            using var tx = ((SqlConnection)conn).BeginTransaction();

            try
            {
                await conn.ExecuteAsync(sqlFirma, dto, tx);
                await conn.ExecuteAsync(sqlDeleteDanos, new { dto.IdVenta }, tx);

                if (dto.Danos?.Count > 0)
                    foreach (var dano in dto.Danos)
                        await conn.ExecuteAsync(sqlInsertDano, new
                        {
                            dto.IdVenta,
                            dano.IdTipoDano,
                            dano.CoordX,
                            dano.CoordY
                        }, tx);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
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