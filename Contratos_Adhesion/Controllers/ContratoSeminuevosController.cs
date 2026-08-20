using Contratos_Adhesion.Models;
using Contratos_Adhesion.Services;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;


namespace Contratos_Adhesion.Controllers
{

    public class ContratoSeminuevosController : Controller
    {
        private readonly IRepositorioContratoSeminuevos _repositorio;
        private readonly IRepositorioOperDocumentos _operDocumentosRepositorio;

        public ContratoSeminuevosController(
            IRepositorioContratoSeminuevos repositorio,
            IRepositorioOperDocumentos operDocumentosRepositorio)
        {
            _repositorio = repositorio;
            _operDocumentosRepositorio = operDocumentosRepositorio;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> ObtenerDatosContrato(string ventaId)
        {
            if (string.IsNullOrWhiteSpace(ventaId))
                return BadRequest(new { mensaje = "El ID de venta es requerido." });
            try
            {
                var negocio = HttpContext.Session.GetInt32("Negocio") ?? 1; // ← nuevo
                var datos = await _repositorio.ObtenerDatosContratoSeminuevoAsync(ventaId, negocio); // ← nuevo
                if (datos == null)
                    return NotFound(new { mensaje = $"La venta con ID {ventaId} no existe en el sistema." });
                return Ok(datos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno.", detalle = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GuardarContrato([FromBody] GuardarContratoSeminuevoDto dto)
        {
            try
            {
                var negocio = HttpContext.Session.GetInt32("Negocio") ?? 1; // ← nuevo
                await _repositorio.GuardarContratoAsync(dto, negocio); // ← nuevo
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        public async Task<IActionResult> GenerarPDFContrato(string ventaId)
        {

            try
            {
                var negocio = HttpContext.Session.GetInt32("Negocio") ?? 1; // ← nuevo
                var dto = await _repositorio.ObtenerDatosContratoSeminuevoAsync(ventaId, negocio); // ← nuevo

                if (dto is null)
                    return NotFound(new { mensaje = $"La venta con ID {ventaId} no existe." });

                QuestPDF.Settings.License = LicenseType.Community;

                // ← nuevo: resolución del logo por negocio
                var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var logoPath = Path.Combine(wwwroot, "imagenes", ObtenerNombreLogo(negocio));
                var logoBytes = System.IO.File.Exists(logoPath) ? System.IO.File.ReadAllBytes(logoPath) : null;

                // Helper local para convertir base64 a bytes de imagen
                static byte[]? Base64ToBytes(string? b64)
                {
                    if (string.IsNullOrEmpty(b64)) return null;
                    var comma = b64.IndexOf(',');
                    var data = comma >= 0 ? b64[(comma + 1)..] : b64;
                    try { return Convert.FromBase64String(data); } catch { return null; }
                }

                var bytesDistribuidor = Base64ToBytes(dto.FirmaDistribuidor);
                var bytesCliente = Base64ToBytes(dto.FirmaCliente);

                // Helper local para el símbolo de checkbox
                static string Chk(bool val) => val ? "(✓)" : "( )";

                var pdf = QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(7.5f).FontFamily("Arial"));

                        page.Content().Column(col =>
                        {
                            // ── Header ───────────────────────────────────────────
                            col.Item().Row(row =>
                            {
                                row.ConstantItem(80).Column(c => // ← nuevo
                                {
                                    if (logoBytes != null) c.Item().Image(logoBytes).FitArea();
                                });
                                row.ConstantItem(8); // ← nuevo

                                row.RelativeItem(3).Column(c =>
                                {
                                    c.Item().Text($"Denominacion: {dto.Denominacion}").Bold();
                                    c.Item().Text($"RFC: {dto.Rfc}");
                                    c.Item().Text($"Domicilio: {dto.Domicilio}");
                                    c.Item().Height(4);
                                    c.Item().Text($"Teléfono(s)          {dto.Telefono}");
                                    c.Item().Text("Horario de atención:");
                                    c.Item().Text(dto.HorarioAtencion);
                                    c.Item().Text("Fax:");
                                    c.Item().Text($"Email: {dto.CorreoDistribuidora}");
                                });
                                row.ConstantItem(10);
                                row.RelativeItem(2).Column(c =>
                                {
                                    c.Item().Text($"Folio: {dto.Folio}").Bold().FontSize(9);
                                    c.Item().Text($"Fecha y hora: {dto.Fecha}").Bold();
                                });
                                row.ConstantItem(10);
                                row.RelativeItem(2).Column(c =>
                                {
                                    c.Item().Text("Localidad:").Bold();
                                    c.Item().Text(dto.Localidad ?? "");
                                });
                            });
                            col.Item().Height(6);

                            // ── Datos del Cliente ────────────────────────────────
                            col.Item().Border(0.5f).Column(sec =>
                            {
                                sec.Item().BorderBottom(0.5f).Padding(2)
                                   .Text("DATOS DE CLIENTE (CONSUMIDOR)")
                                   .Bold().FontSize(7.5f);

                                sec.Item().Padding(3).Column(inner =>
                                {
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem(1).Column(c =>
                                        {
                                            c.Item().BorderBottom(0.5f).Text(dto.NombreCliente);
                                            c.Item().Text("Nombre:").FontSize(6.5f);
                                        });
                                        r.ConstantItem(8);
                                        r.RelativeItem(1).Column(rr =>
                                        {
                                            rr.Item().BorderBottom(0.5f).Text(dto.RfcCliente);
                                            rr.Item().Text("R.F.C.:").FontSize(6.5f);
                                        });
                                    });

                                    inner.Item().Height(4);
                                    inner.Item().Text("Domicilio:");

                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem(3).Column(c =>
                                        {
                                            c.Item().BorderBottom(0.5f).Text(dto.Calle);
                                            c.Item().Text("(Calle)").FontSize(6.5f);
                                        });
                                        r.ConstantItem(6);
                                        r.RelativeItem(2).Column(c =>
                                        {
                                            c.Item().BorderBottom(0.5f).Text(dto.NumExt);
                                            c.Item().Text("(Número Exterior e Interior)").FontSize(6.5f);
                                        });
                                        r.ConstantItem(6);
                                        r.RelativeItem(2).Column(c =>
                                        {
                                            c.Item().BorderBottom(0.5f).Text(dto.Colonia);
                                            c.Item().Text("(Colonia)").FontSize(6.5f);
                                        });
                                    });

                                    inner.Item().Height(3);

                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem(1).Column(c =>
                                        {
                                            c.Item().BorderBottom(0.5f).Text(dto.CodigoPostal);
                                            c.Item().Text("(Código Postal)").FontSize(6.5f);
                                        });
                                        r.ConstantItem(6);
                                        r.RelativeItem(2).Column(c =>
                                        {
                                            c.Item().BorderBottom(0.5f).Text(dto.Delegacion);
                                            c.Item().Text("(Delegación o Municipio)").FontSize(6.5f);
                                        });
                                        r.ConstantItem(6);
                                        r.RelativeItem(2).Column(c =>
                                        {
                                            c.Item().BorderBottom(0.5f).Text(dto.Estado);
                                            c.Item().Text("(Estado)").FontSize(6.5f);
                                        });
                                        r.ConstantItem(6);
                                        r.RelativeItem(2).Column(c =>
                                        {
                                            c.Item().BorderBottom(0.5f).Text(dto.Telefonos);
                                            c.Item().Text("(Teléfonos)").FontSize(6.5f);
                                        });
                                    });
                                });
                            });

                            col.Item().Height(5);

                            // ── Características del Vehículo Usado ───────────────
                            col.Item().Border(0.5f).Column(sec =>
                            {
                                sec.Item().BorderBottom(0.5f).Padding(2)
                                   .Text("CARACTERÍSTICAS DEL VEHÍCULO USADO")
                                   .Bold().FontSize(7.5f);

                                sec.Item().Padding(3).Row(row =>
                                {
                                    row.RelativeItem().Column(c =>
                                    {
                                        LabelValue(c, "Marca:", dto.Marca);
                                        LabelValue(c, "Submarca:", dto.Submarca);
                                        LabelValue(c, "Tipo o versión:", dto.TipoVersion);
                                        LabelValue(c, "Color:", dto.Color);
                                        LabelValue(c, "Año-Modelo:", dto.AnioModelo);
                                    });
                                    row.ConstantItem(10);
                                    row.RelativeItem().Column(c =>
                                    {
                                        LabelValue(c, "Número de kilómetros recorridos:", dto.KmRecorridos);
                                        LabelValue(c, "Número Identificación Vehicular:", dto.Niv);
                                        LabelValue(c, "Capacidad:", dto.Capacidad);
                                        LabelValue(c, "Número de Placas:", dto.Placas);
                                        LabelValue(c, "Fecha y hora de entrega del vehículo:", dto.FechaEntrega);
                                        c.Item().Height(3);
                                        c.Item().Text("Lugar de entrega del Vehículo:").Bold();
                                        c.Item().Text(dto.LugarEntrega ?? "");
                                    });
                                });
                            });

                            col.Item().Height(5);

                            // ── Monto / Forma de Pago ────────────────────────────
                            col.Item().Border(0.5f).Column(sec =>
                            {
                                sec.Item().BorderBottom(0.5f).Padding(2)
                                   .Text("MONTO DE LA OPERACIÓN")
                                   .Bold().FontSize(7.5f);

                                sec.Item().Padding(3).Row(row =>
                                {
                                    row.RelativeItem(3).Column(c =>
                                    {
                                        MontoRow(c, "Precio del vehículo usado:", dto.PrecioVehiculo);
                                        MontoRow(c, "Equipo y accesorios adicionales (ver análisis):", dto.Equipo);
                                        MontoRow(c, "Otros cargos:", dto.OtrosCargos);
                                        c.Item().Height(3);
                                        MontoRow(c, "Impuestos al valor agregado:", dto.Iva);
                                        c.Item().BorderTop(0.5f).Height(2);
                                        MontoRow(c, "Monto total de la operación:", dto.MontoTotal, bold: true);
                                    });

                                    row.ConstantItem(8);

                                    row.RelativeItem(2).Border(0.5f).Column(c =>
                                    {
                                        c.Item().BorderBottom(0.5f).Padding(2)
                                         .Text("FORMA DE PAGO")
                                         .Bold().FontSize(7.5f);

                                        c.Item().Padding(3).Column(inner =>
                                        {
                                            MontoRow(inner, "Contado:", dto.PagoContado, bold: true);
                                            inner.Item().Height(3);
                                            MontoRow(inner, "Enganche o unidad usada a cuenta (Ver descripción):", dto.Enganche);
                                        });
                                    });
                                });
                            });

                            col.Item().Height(5);

                            // ── Equipo adicional | Unidad usada ──────────────────
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Border(0.5f).Column(sec =>
                                {
                                    sec.Item().BorderBottom(0.5f).Padding(2)
                                       .Text("EQUIPO Y ACCESORIOS ADICIONALES:")
                                       .Bold().FontSize(7.5f);
                                    sec.Item().MinHeight(50).Padding(3).Column(c =>
                                    {
                                        for (int i = 0; i < 4; i++)
                                            c.Item().BorderBottom(0.3f).Height(12).Text("");
                                    });
                                    sec.Item().Padding(3).BorderTop(0.5f)
                                       .Text("Total equipo y accesorios adicionales:");
                                });

                                row.ConstantItem(4);

                                row.RelativeItem().Border(0.5f).Column(sec =>
                                {
                                    sec.Item().BorderBottom(0.5f).Padding(2)
                                       .Text("DESCRIPCIÓN UNIDAD USADA A CUENTA:")
                                       .Bold().FontSize(7.5f);
                                    sec.Item().Padding(3).Column(c =>
                                    {
                                        LabelValue(c, "Número de identificación vehicular:", dto.UsadaNiv ?? "");
                                        LabelValue(c, "Marca:", dto.UsadaMarca ?? "");
                                        LabelValue(c, "Submarca:", dto.UsadaSubmarca ?? "");
                                        LabelValue(c, "Tipo o versión:", dto.UsadaVersion ?? "");
                                        LabelValue(c, "Color:", dto.UsadaColor ?? "");
                                        LabelValue(c, "Año-modelo:", dto.UsadaAnio ?? "");
                                        LabelValue(c, "Número de inscripción al REPUVE:", dto.Repuve ?? "");
                                        LabelValue(c, "Valor de la unidad:", dto.UsadaValor ?? "");
                                    });
                                });
                            });

                            col.Item().Height(6);

                            // ── Condiciones ───────────────────────────────────────
                            col.Item().Text(t =>
                            {
                                t.Justify();
                                t.Span("CONDICIONES DEL CONTRATO DE COMPRA-VENTA DE VEHÍCULO USADO AL CONTADO.").Bold();
                            });

                            // Cláusula 6 — texto con checkboxes dinámicos
                            var clausula6 =
                                $"El Cliente acepta que por tratarse de un vehículo usado, lo adquiere en el estado de uso en el que se encuentra, el cual le fue facilitado para su revisión de forma detallada y cuenta con el siguiente equipo; " +
                                $"Exteriores: {Chk(dto.ExtLimpiaparabrisas)} Limpiaparabrisas (plumas); {Chk(dto.ExtLuces)} Unidades de luces; {Chk(dto.ExtAntena)} Antena; {Chk(dto.ExtEspejosLat)} Espejos laterales; {Chk(dto.ExtCristales)} Cristales; {Chk(dto.ExtTapones)} Tapones de ruedas; {Chk(dto.ExtMolduras)} Molduras completas; {Chk(dto.ExtTaponGas)} Tapón de gasolina; {Chk(dto.ExtClaxon)} Claxon; " +
                                $"Interiores: {Chk(dto.IntInstrumentos)} Instrumentos del tablero; {Chk(dto.IntCalefaccion)} Calefacción; {Chk(dto.IntAire)} Aire acondicionado; {Chk(dto.IntRadio)} Radio/Tipo; {Chk(dto.IntBocinas)} Bocinas; {Chk(dto.IntEncendedor)} Encendedor; {Chk(dto.IntEspejoRet)} Espejo Retrovisor; {Chk(dto.IntCeniceros)} ceniceros; {Chk(dto.IntCinturones)} Cinturones de seguridad; {Chk(dto.IntTapetes)} Tapetes; Manijas y/o controles interiores; {Chk(dto.IntEquipoAd)} Equipo adicional; {Chk(dto.IntAccesorios)} Accesorios; {Chk(dto.IntOtros)} otros. " +
                                $"El vehículo se encuentra en las siguientes condiciones generales: Aspectos mecánicos: {Chk(dto.MecLlantas)} Llantas, {Chk(dto.MecRuedas)} Ruedas, {Chk(dto.MecRines)} Rines, {Chk(dto.MecEscape)} Escape, {Chk(dto.MecDireccion)} Dirección, {Chk(dto.MecSuspension)} Suspensión, {Chk(dto.MecFrenos)} Frenos, {Chk(dto.MecParabrisas)} Parabrisas, {Chk(dto.MecCarroceria)} aspectos de carrocería.";

                            // Cláusula 7 — documentos unidad usada
                            var clausula7 =
                                $"En caso de que el Cliente entregue un vehículo usado a cuenta del precio, entrega también la documentación correspondiente, consistente en: " +
                                $"{Chk(dto.Doc7Factura)} Factura; {Chk(dto.Doc7Tarjeta)} Tarjeta de circulación; {Chk(dto.Doc7DocsOficiales)} Documentos Oficiales que acrediten su legal estancia en el país; {Chk(dto.Doc7Manual)} Manual del Usuario; {Chk(dto.Doc7Tenencias)} Comprobante de pago de tenencias; {Chk(dto.Doc7Verificacion)} Comprobante de verificación ambiental; {Chk(dto.Doc7Multas)} Comprobantes de pago multas y recargos, " +
                                "declarando de manera expresa que dicha documentación es legítima. Los impuestos anteriores no pagados así como sus accesorios serán por cuenta y responsabilidad del Cliente. Asimismo, el Cliente manifiesta que el vehículo está libre de gravamen y no tiene problema judicial y/o administrativo alguno, por lo que en este acto libera al Distribuidor de adeudos o conflictos que por cualquier motivo pudiera generar dicho vehículo previo a la celebración del presente contrato.";

                            // Cláusula 8 — garantía con checkboxes
                            var clausula8 =
                                $"El vehículo usado se vende:\n" +
                                $"{Chk(dto.SinGarantia)} Sin garantía; en este caso el proveedor no está obligado a realizar ningún tipo de reparación, por lo que el Cliente asumirá los costos por reparaciones, suministro de refacciones, mano de obra calificadas, entre otros.\n\n" +
                                $"{Chk(dto.ConGarantia)} Con garantía por un plazo de 90 días. (Art. 77 de la Ley Federal de Protección al Consumidor, la garantía no podrá ser inferior a 90 días naturales) contados a partir de la entrega del vehículo usado, excluyéndose la correspondiente a partes eléctricas y deberá hacerse válida en el domicilio, teléfonos y horarios de atención señalados en el rubro del presente contrato, siempre y cuando no se haya efectuado una reparación por un tercero. Asimismo, el Distribuidor será el responsable por las descomposturas, daños o pérdidas parciales o totales imputables a él, mientras el vehículo se encuentre bajo su responsabilidad para llevar a cabo el cumplimiento de la garantía.";

                            // Cláusula 10 — documentos que entrega el distribuidor
                            var clausula10 =
                                $"El Distribuidor entrega junto con el vehículo usado los siguientes documentos: " +
                                $"{Chk(dto.Doc10Factura)} Factura emitida por el Distribuidor; {Chk(dto.Doc10DocsOficiales)} Documentos oficiales que acrediten su legal estancia en el país; {Chk(dto.Doc10Constancia)} Constancia de cambio de propietario; {Chk(dto.Doc10Tenencias)} Comprobante de pago de tenencias; {Chk(dto.Doc10Verificacion)} Comprobante de verificación ambiental; {Chk(dto.Doc10Multas)} Comprobantes de pago multas y recargos; {Chk(dto.Doc10Manual)} Manual del Usuario. " +
                                "Los trámites y gastos de trámite de \"cambio de propietario\", serán por cuenta del Cliente. El Cliente recibe el vehículo usado descrito en el presente contrato, por lo que en este acto libera al Distribuidor de adeudos o conflictos que por cualquier motivo pudiera generar dicho vehículo a partir de la fecha de su entrega, en el entendido de que con anterioridad a esta fecha, el Distribuidor asume los adeudos o conflictos que por cualquier motivo pudiera generar dicho vehículo, obligándose a responder por el saneamiento para el caso de evicción.";

                            var condiciones = new[]
                            {
                        ("1.",  "En virtud de este contrato, el Distribuidor (Proveedor) como legítimo propietario vende al Cliente (Consumidor) el vehículo usado cuyas características se detallan en este contrato, lo que recibe después de haber efectuado una revisión de forma detallada, el cual cumple con las Normas Oficiales Mexicanas vigentes aplicables en materia de seguridad y protección al medio ambiente y que conforme las disposiciones aplicables, el Distribuidor y el vehículo usado cumplen con todas las especificaciones legales y comerciales para poder realizar la presente compraventa."),
                        ("2.",  "Se proporcionará al cliente toda la información relativa a las restricciones que pudieran aplicar."),
                        ("3.",  "El vehículo usado cuenta con el equipo opcional y accesorios adicionales solicitados y autorizados por el Cliente, detallados en el presente contrato."),
                        ("4.",  "Las partes manifiestan que no se hará cargo alguno por servicios adicionales a los pactados en el presente instrumento, sin previo consentimiento del cliente."),
                        ("5.",  "El precio total de la compraventa será cubierto en la fecha de firma del presente contrato, incluyendo, en su caso, los equipos y accesorios adicionales."),
                        ("6.",  clausula6),
                        ("7.",  clausula7),
                        ("8.",  clausula8),
                        ("9.",  "El Distribuidor cuenta con personal capacitado y responsable para atender dudas, aclaraciones, reclamaciones y servicios de orientación. Estos servicios se proporcionarán de manera gratuita en el domicilio, teléfonos y horarios de atención señalados en el rubro del presente contrato. Asimismo, cuenta con la capacidad, infraestructura, servicios y recursos necesarios para dar cabal cumplimiento a las obligaciones contenidas en el presente contrato."),
                        ("10.", clausula10),
                        ("11.", "El cliente podrá revocar su consentimiento, en un plazo de 5 días hábiles mediante aviso personal, correo electrónico o correo certificado, siempre y cuando no le haya sido entregado el vehículo materia del presente contrato."),
                        ("12.", "Son causas de rescisión del presente contrato: (i) Que el Distribuidor incumpla con la entrega del vehículo en las condiciones pactadas en el presente contrato por causas imputables a él. -El Cliente le notificará por escrito el incumplimiento de dicha obligación y el Distribuidor devolverá las cantidades que por cualquier concepto hubiese recibido del Cliente con motivo de esta compraventa, en un plazo no mayor de 5 días hábiles a partir de la fecha en que fue notificado dicho incumplimiento, más la cantidad por concepto de pena convencional equivalente al 1% del precio total de venta del vehículo, en el que se incluye el IVA."),
                        ("13.", "Las partes están de acuerdo en someterse a la competencia de la Procuraduría Federal del Consumidor en la vía administrativa para resolver cualquier controversia que se suscite sobre la interpretación o cumplimiento de los términos y condiciones del presente contrato y de las disposiciones de la Ley Federal de Protección al Consumidor, la Norma Oficial Mexicana NOM-122-SCFI-2010, Prácticas Comerciales-Elementos Normativos para la Comercialización y/o Consignación de Vehículos Usados y cualquier otra disposición aplicable, sin perjuicio del derecho que tienen las partes de someterse a la jurisdicción de los Tribunales competentes del domicilio del Distribuidor, renunciando las partes expresamente a cualquier otra jurisdicción que pudiera corresponderles por razón de sus domicilios futuros."),
                        ("14.", "El Distribuidor se obliga, a no ceder o transmitir a terceros, con fines mercadotécnicos o publicitarios, los datos e información proporcionada por el cliente con motivo del presente contrato, no enviar publicidad sobre bienes y servicios, salvo autorización expresa del cliente en la presente cláusula ___."),
                        ("15.", "El Cliente y el Distribuidor aceptan la realización de la presente compraventa en los términos establecidos en este contrato, y sabedores de su contenido legal, lo firman por duplicado."),
                        ("16.", "Consentimiento por medios electrónicos. Las partes acuerdan que, en lugar de una firma original autógrafa, este contrato, así como cualquier otro consentimiento, aprobación u otros documentos relacionados con el mismo, podrán ser firmados por medio del uso de firmas electrónicas, digitales, numéricas, alfanuméricas, huellas de voz, biométricas o de cualquier otra forma y que dichos medios alternativos de firma y los registros en donde sean aplicadas dichas firmas, serán consideradas para todos los efectos, incluyendo pero no limitado a la legislación civil, mercantil, protección al consumidor y a la NOM-151-SCFI-2016, con la misma fuerza y consecuencia que la firma autógrafa original física de la parte firmante. Si el contrato o cualquier otro documento relacionado con el mismo es firmado por medios electrónicos o digitales, las Partes acuerdan que los formatos del contrato y los demás documentos firmados de tal modo serán conservados y estarán a disposición del consumidor, por lo que conviene que cada una y toda la información enviada por el Proveedor a la dirección de correo electrónico proporcionada por el Consumidor al momento de celebrar el presente Contrato será considerada como entregada en el momento en que la misma es enviada, siempre y cuando exista confirmación de recepción."),
                        ("17.", "(*) El presente contrato fue registrado en la Procuraduría Federal del Consumidor bajo el número 9827-2023 de fecha 24 de noviembre de 2023. Cualquier variación del presente contrato en perjuicio del cliente, frente al contrato de adhesión registrado, se tendrá por no puesta."),
                    };

                            col.Item().Column(c =>
                            {
                                foreach (var (num, texto) in condiciones)
                                    ClausulaItem(c, num, texto);
                            });

                            col.Item().Height(10);

                            // ── Firmas ────────────────────────────────────────────
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().AlignCenter().Text("EL DISTRIBUIDOR").Bold();
                                    c.Item().AlignCenter().Text("(Nombre y Firma)").FontSize(6.5f);
                                    if (bytesDistribuidor != null)
                                        c.Item().Height(40).AlignCenter().Image(bytesDistribuidor).FitHeight();
                                    else
                                        c.Item().Height(40).BorderBottom(0.5f).Text("");
                                    c.Item().AlignCenter().Text(dto.Denominacion).Bold().FontSize(7);
                                });

                                row.ConstantItem(20);

                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().AlignCenter().Text("EL CLIENTE").Bold();
                                    c.Item().AlignCenter().Text("(Nombre y Firma)").FontSize(6.5f);
                                    if (bytesCliente != null)
                                        c.Item().Height(40).AlignCenter().Image(bytesCliente).FitHeight();
                                    else
                                        c.Item().Height(40).BorderBottom(0.5f).Text("");
                                    c.Item().AlignCenter().Text(dto.NombreCliente).Bold().FontSize(7);
                                });
                            });

                            col.Item().Height(10);

                            // ── Nota PROFECO ──────────────────────────────────────
                            col.Item().Text(
                                "(*) El presente contrato fue registrado en la Procuraduría Federal del Consumidor bajo el número 9827-2023 el día 24 de noviembre de 2023. Cualquier variación del presente contrato en perjuicio del cliente, frente al contrato de adhesión registrado, se tendrá por no puesta."
                            ).FontSize(7).Italic();
                        });
                    });
                });

                var bytes = pdf.GeneratePdf();
                return File(bytes, "application/pdf", $"ContratoUsado_{dto.Folio}_{dto.NombreCliente}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Helpers privados
        // ─────────────────────────────────────────────────────────────────────────────

        private static void LabelValue(ColumnDescriptor col, string label, string value)
        {
            col.Item().Row(r =>
            {
                r.AutoItem().Text(label).Bold();
                r.ConstantItem(4);
                r.RelativeItem().Text(value ?? "");
            });
            col.Item().Height(2);
        }

        private static void MontoRow(ColumnDescriptor col, string label, string valor, bool bold = false)
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Text(t =>
                {
                    var span = t.Span(label);
                    if (bold) span.Bold();
                });
                r.ConstantItem(4);
                r.AutoItem().Text(t =>
                {
                    var span = t.Span(valor ?? "");
                    if (bold) span.Bold();
                });
            });
            col.Item().Height(2);
        }

        private static void ClausulaItem(ColumnDescriptor col, string numero, string texto)
        {
            col.Item().Text(t =>
            {
                t.Justify();
                t.Span(numero + " ").Bold();
                // Respetar saltos de línea (usado en cláusula 8 para separar opciones)
                var partes = texto.Split('\n');
                for (int i = 0; i < partes.Length; i++)
                {
                    if (i > 0) t.Line("");
                    t.Span(partes[i]);
                }
            });
            col.Item().Height(5);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarDocumentoOper([FromBody] GuardarDocumentoOperDto dto)
        {
            if (dto == null || dto.IdVenta <= 0 || string.IsNullOrWhiteSpace(dto.IdSharePoint))
                return BadRequest(new { mensaje = "Datos de documento inválidos." });

            try
            {
                var negocio = HttpContext.Session.GetInt32("Negocio") ?? 1;

                var ventaMov = await _repositorio.ObtenerMovVentaAsync(dto.IdVenta.ToString(), negocio);
                if (ventaMov == null)
                    return NotFound(new { mensaje = $"La venta con ID {dto.IdVenta} no existe." });

                await _operDocumentosRepositorio.GuardarDocumentoAsync(
                    dto, ventaMov.Mov, ventaMov.MovId, "Contrato Adhesion");

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al guardar el documento.", detalle = ex.Message });
            }
        }

        private static string ObtenerNombreLogo(int negocio) => negocio switch
        {
            1 => "Toyota.jpg",    // Toque
            2 => "KIA2.png",      // Kique
            3 => "RENAULT.png",   // Reque
            4 => "NISSAN.png",    // Nicui
            5 => "NISSAN.png",    // Nivil
            _ => "Toyota.jpg"
        };
    }
}
