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

    public class ContratoServicioController : Controller
    {
        private readonly IRepositorioContratoServicio _repositorio;
        private readonly IRepositorioOperDocumentos _operDocumentosRepositorio; 

        public ContratoServicioController(
            IRepositorioContratoServicio repositorio,
            IRepositorioOperDocumentos operDocumentosRepositorio)
        {
            _repositorio = repositorio;
            _operDocumentosRepositorio = operDocumentosRepositorio;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> ObtenerDatos(string idServicio)
        {
            if (string.IsNullOrWhiteSpace(idServicio))
                return BadRequest(new { mensaje = "El número de orden es requerido." });
            try
            {
                var negocio = HttpContext.Session.GetInt32("Negocio") ?? 1; // ← nuevo
                var datos = await _repositorio.ObtenerDatosOrdenServicioAsync(idServicio, negocio); // ← nuevo
                if (datos == null)
                    return NotFound(new { mensaje = $"No se encontró la orden con ID: {idServicio}" });
                return Ok(datos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno.", detalle = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> GuardarContrato([FromBody] GuardarContratoServicioDto dto)
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

        [HttpGet]

        public async Task<IActionResult> GenerarPDF(string idServicio)

        {

            var negocio = HttpContext.Session.GetInt32("Negocio") ?? 1; // ← nuevo
            var dto = await _repositorio.ObtenerDatosOrdenServicioAsync(idServicio, negocio); // ← nuevo

            if (dto is null)
                return NotFound(new { mensaje = $"No se encontró la orden con ID: {idServicio}" });

            try

            {

                // Rutas de imágenes

                var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                var negocio2 = HttpContext.Session.GetInt32("Negocio") ?? 1;
                var logoPath = Path.Combine(wwwroot, "imagenes", ObtenerNombreLogo(negocio2));
                var cochesPath = Path.Combine(wwwroot, "imagenes", "CochesServicio.jpg");

                var logoBytes = System.IO.File.Exists(logoPath) ? System.IO.File.ReadAllBytes(logoPath) : null;

                var cochesBytes = System.IO.File.Exists(cochesPath) ? System.IO.File.ReadAllBytes(cochesPath) : null;



                // Calcular alto real de la imagen del coche en mm

                // sin ninguna librería externa — parsea el header JPEG/PNG directamente

                const float anchoImagenMm = 191.9f;  // Letter - márgenes 1.2cm * 2

                float altoImagenMm = 72f;             // valor por defecto razonable

                if (cochesBytes != null)

                {

                    var (imgW, imgH) = GetImageDimensions(cochesBytes);

                    if (imgW > 0 && imgH > 0)

                        altoImagenMm = anchoImagenMm * ((float)imgH / imgW);

                }



                // Helper base64 → bytes

                static byte[]? Base64ToBytes(string? b64)

                {

                    if (string.IsNullOrEmpty(b64)) return null;

                    var comma = b64.IndexOf(',');

                    var data = comma >= 0 ? b64[(comma + 1)..] : b64;

                    try { return Convert.FromBase64String(data); } catch { return null; }

                }



                var bytesDistribuidor = Base64ToBytes(dto.FirmaDistribuidor);

                var bytesCliente = Base64ToBytes(dto.FirmaCliente);

                var bytesExtra = Base64ToBytes(dto.FirmaExtra);



                // Helper checkbox

                static string Chk(bool val) => val ? "(✓)" : "( )";



                // Mapear daños de BD → tipo interno

                var danos = (dto.Danos ?? new List<DanoServicioDto>())

                    .Select(d => new

                    {

                        Type = d.IdTipoDano == 1 ? "golpe" : "rayon",

                        X = (double)d.CoordX,

                        Y = (double)d.CoordY

                    })

                    .ToList();



                // Tamaño del marcador de daño en mm

                const float marcadorMm = 5f;



                QuestPDF.Settings.License = LicenseType.Community;



                var pdf = QuestPDF.Fluent.Document.Create(container =>

                {

                    container.Page(page =>

                    {

                        page.Size(PageSizes.Letter);

                        page.Margin(1.2f, Unit.Centimetre);

                        page.DefaultTextStyle(x => x.FontSize(7.5f).FontFamily("Arial"));



                        page.Content().Column(col =>

                        {

                            // ══════ HEADER ══════════════════════════════════════

                            col.Item().Row(row =>

                            {

                                row.ConstantItem(80).Column(c =>

                                {

                                    if (logoBytes != null) c.Item().Image(logoBytes).FitArea();

                                });

                                row.ConstantItem(8);

                                row.RelativeItem(3).Column(c =>

                                {

                                    c.Item().Text(dto.Denominacion).Bold();

                                    c.Item().Text($"Domicilio: {dto.Domicilio}");

                                    c.Item().Text($"RFC: {dto.Rfc}");

                                    c.Item().Text($"Tel: {dto.Telefono}");

                                    c.Item().Text($"Horarios: {dto.HorarioAtencion}");

                                    c.Item().Text($"Fax: N/A   Email: {dto.CorreoDistribuidora}");

                                });

                                row.ConstantItem(8);

                                row.RelativeItem(2).Column(c =>

                                {

                                    c.Item().Border(0.5f).Padding(3).Column(inner =>

                                    {

                                        inner.Item().Text($"Orden de Reparación: {dto.NumOrden}").Bold();

                                        inner.Item().Text($"FECHA: {dto.FechaOrden}").Bold();

                                    });

                                    c.Item().Height(4);

                                    c.Item().Column(inner =>

                                    {

                                        inner.Item().Text("LOCALIDAD:").Bold();

                                        inner.Item().Text(dto.Localidad);

                                    });

                                });

                            });



                            col.Item().Height(6);



                            // ══════ DATOS DEL CLIENTE ════════════════════════════

                            col.Item().Border(0.5f).Column(sec =>

                            {

                                sec.Item().BorderBottom(0.5f).Padding(2)

                                   .Text("DATOS DE CLIENTE (CONSUMIDOR)").Bold().FontSize(7.5f);

                                sec.Item().Padding(3).Column(inner =>
                                {
                                    inner.Item().Row(r =>
                                    {
                                        r.AutoItem().Text("Nombre:  ").Bold();
                                        r.RelativeItem(3).Text(dto.NombreCliente ?? "");
                                        r.ConstantItem(10);
                                        r.AutoItem().Text("RFC:  ").Bold();
                                        r.RelativeItem(2).Text(dto.RfcCliente ?? "");
                                        r.ConstantItem(10);
                                        r.AutoItem().Text("Email:  ").Bold();
                                        r.RelativeItem(3).Text(dto.Correo ?? "");
                                    });
                                    inner.Item().Height(2);
                                    inner.Item().Row(r =>
                                    {
                                        r.AutoItem().Text("Domicilio:  ").Bold();
                                        r.AutoItem().Text("Calle: ").Bold();
                                        r.RelativeItem(3).Text($"{dto.Calle} No. {dto.NumExt}");
                                        r.ConstantItem(10);
                                        r.AutoItem().Text("Colonia: ").Bold();
                                        r.RelativeItem(3).Text(dto.Colonia ?? "");
                                    });
                                    inner.Item().Height(2);
                                    inner.Item().Row(r =>
                                    {
                                        r.AutoItem().Text("Delegación o Municipio: ").Bold();
                                        r.RelativeItem(2).Text(dto.Delegacion ?? "");
                                        r.ConstantItem(10);
                                        r.AutoItem().Text("Estado: ").Bold();
                                        r.RelativeItem(2).Text(dto.Estado ?? "");
                                        r.ConstantItem(10);
                                        r.AutoItem().Text("Tel: ").Bold();
                                        r.RelativeItem(2).Text(dto.Telefonos ?? "");
                                    });
                                });

                            });



                            col.Item().Height(5);



                            // ══════ VEHÍCULO + ASESOR ════════════════════════════

                            col.Item().Border(0.5f).Row(row =>

                            {

                                row.RelativeItem().BorderRight(0.5f).Column(sec =>

                                {

                                    sec.Item().BorderBottom(0.5f).Padding(2)

                                       .Text("CARACTERÍSTICAS GENERALES DEL VEHÍCULO").Bold().FontSize(7.5f);

                                    sec.Item().Padding(3).Column(c =>

                                    {

                                        LabelValue(c, "Marca:", dto.Marca);

                                        LabelValue(c, "Submarca:", dto.Submarca);

                                        LabelValue(c, "Tipo o versión:", dto.TipoVersion);

                                        LabelValue(c, "Color:", dto.Color);

                                        LabelValue(c, "Año-Modelo:", dto.AnioModelo);

                                        LabelValue(c, "Número Identificación Vehicular:", dto.Niv);

                                        LabelValue(c, "Capacidad:", dto.Capacidad);

                                        LabelValue(c, "Número de kilómetros recorridos:", dto.KmRecorridos);

                                        LabelValue(c, "Número de Placas:", dto.Placas);

                                    });

                                });

                                row.RelativeItem().Column(sec =>

                                {

                                    sec.Item().BorderBottom(0.5f).Padding(2)

                                       .Text("ASESOR").Bold().FontSize(7.5f);

                                    sec.Item().Padding(3).Column(c =>

                                    {

                                        LabelValue(c, "Asesor:", dto.Asesor);

                                        LabelValue(c, "Pirámide:", dto.Piramide);

                                        LabelValue(c, "Fecha y hora de Recepción del Vehículo:", dto.FechaRecepcion);

                                        LabelValue(c, "Fecha y hora de Entrega del Vehículo:", dto.FechaEntrega);

                                        c.Item().Height(4);

                                        c.Item().Text(t =>
                                        {
                                            t.Span("Se entregan las partes o refacciones reemplazadas al consumidor: ");
                                            t.Span($"SI {Chk(dto.SeEntreganPartes)}  NO {Chk(!dto.SeEntreganPartes)}");
                                        });

                                        c.Item().Height(3);

                                        c.Item().Text("NOTA: Las partes y/o refacciones NO se entregarán al consumidor cuando:").Bold();

                                        c.Item().Text("a) Sean cambiadas en uso de garantía");

                                        c.Item().Text("b) Se trate de residuos considerados peligrosos de acuerdo con las disposiciones legales aplicables");

                                        c.Item().Height(3);

                                        c.Item().Text("Servicio en el domicilio del consumidor:  SI ( )   NO (✓)");

                                        c.Item().Text("Póliza de seguro para cubrir al consumidor los daños o el extravío de bienes:");

                                        c.Item().Text(t =>

                                        {

                                            var tienePoliza = !string.IsNullOrEmpty(dto.PolizaNumero);

                                            t.Span($"{Chk(tienePoliza)} SI  Número: ");

                                            t.Span(dto.PolizaNumero ?? "");

                                            t.Span($"  {Chk(!tienePoliza)}  NO");

                                        });

                                    });

                                });

                            });



                            col.Item().Height(5);



                            // ══════ TABLA OPERACIONES ════════════════════════════

                            col.Item().Border(0.5f).Column(sec =>

                            {

                                sec.Item().Row(header =>

                                {

                                    header.RelativeItem(2).BorderBottom(0.5f).Padding(2)

                                          .Text("OPERACIONES A EFECTUAR:").Bold().FontSize(7.5f);

                                    header.RelativeItem(4).BorderBottom(0.5f).Padding(2)

                                          .Text("PARTES Y/O REFACCIONES:").Bold().FontSize(7.5f);

                                    header.RelativeItem(1).BorderBottom(0.5f).Padding(2)

                                          .AlignCenter().Text("CANT.").Bold().FontSize(7.5f);

                                    header.RelativeItem(2).BorderBottom(0.5f).Padding(2)

                                          .AlignRight().Text("PRECIOS UNITARIOS").Bold().FontSize(7.5f);

                                    header.RelativeItem(2).BorderBottom(0.5f).Padding(2)

                                          .AlignRight().Text("TOTAL").Bold().FontSize(7.5f);

                                });

                                var ops = dto.Operaciones ?? new List<LineaOperacionDto>();

                                foreach (var op in ops)

                                {

                                    sec.Item().BorderTop(0.3f).Row(fila =>

                                    {

                                        fila.RelativeItem(2).Padding(2).Text(op.Articulo ?? "");

                                        fila.RelativeItem(4).BorderLeft(0.3f).Padding(2).Text(op.Descripcion ?? "");

                                        fila.RelativeItem(1).BorderLeft(0.3f).Padding(2).AlignCenter().Text(op.CantidadStr);

                                        fila.RelativeItem(2).BorderLeft(0.3f).Padding(2).AlignRight().Text(op.PrecioUnitStr);

                                        fila.RelativeItem(2).BorderLeft(0.3f).Padding(2).AlignRight().Text(op.TotalStr);

                                    });

                                }

                                for (int i = ops.Count; i < 8; i++)

                                {

                                    sec.Item().BorderTop(0.3f).Row(fila =>

                                    {

                                        fila.RelativeItem(2).Padding(2).Text("");

                                        fila.RelativeItem(4).BorderLeft(0.3f).Padding(2).Text("");

                                        fila.RelativeItem(1).BorderLeft(0.3f).Padding(2).Text("");

                                        fila.RelativeItem(2).BorderLeft(0.3f).Padding(2).Text("");

                                        fila.RelativeItem(2).BorderLeft(0.3f).Padding(2).Text("");

                                    });

                                }

                                sec.Item().BorderTop(0.5f).Row(fila =>

                                {

                                    fila.RelativeItem(9).Padding(2).AlignRight().Text("Monto total de la operación:").Bold();

                                    fila.RelativeItem(2).BorderLeft(0.3f).Padding(2).AlignRight().Text(dto.PrecioTotalStr).Bold();

                                });

                            });



                            col.Item().Height(5);



                            // ══════ FORMA DE PAGO | SERVICIOS ADICIONALES ═══════

                            col.Item().Row(row =>

                            {

                                row.RelativeItem().Border(0.5f).Column(sec =>

                                {

                                    sec.Item().BorderBottom(0.5f).Padding(2)

                                       .Text("FORMA DE PAGO:").Bold().FontSize(7.5f);

                                    sec.Item().Padding(3).Column(c =>

                                    {

                                        MontoRow(c, "Monto de la operación:", dto.ImporteStr);

                                        MontoRow(c, "Otros Cargos:", "");

                                        MontoRow(c, "Servicios Adicionales:", "");

                                        MontoRow(c, "Parcial:", dto.ImporteStr);

                                        MontoRow(c, "Impuesto al Valor Agregado:", dto.ImpuestosStr);

                                        c.Item().Height(3);

                                        MontoRow(c, "Monto Total (incluye mano de obra):", dto.PrecioTotalStr, bold: true);

                                        c.Item().Height(4);

                                        c.Item().Text("Efectivo ( )   Tarjeta de crédito ( )   Otro ( )");

                                    });

                                });

                                row.ConstantItem(4);

                                row.RelativeItem().Border(0.5f).Column(sec =>

                                {

                                    sec.Item().BorderBottom(0.5f).Padding(2)

                                       .Text("SERVICIOS ADICIONALES:").Bold().FontSize(7.5f);

                                    sec.Item().Padding(3).Column(c =>

                                    {

                                        MontoRow(c, "Total refacciones y servicios adicionales:", "");

                                        c.Item().Height(4);

                                        c.Item().Text("Fecha y monto de anticipo:");

                                        c.Item().Height(3);

                                        c.Item().Text(t =>

                                        {

                                            t.Justify();

                                            t.Span("El resto del monto total de la operación, se liquidará en la fecha señalada para la entrega del vehículo.");

                                        });

                                    });

                                });

                            });



                            col.Item().Height(6);



                            // ══════ CONDICIONES DEL CONTRATO ════════════════════

                            col.Item().AlignCenter()

                               .Text("CONDICIONES DEL CONTRATO DE PRESTACIÓN DE SERVICIOS DE REPARACION Y/O MANTENIMIENTO DE VEHÍCULOS")

                               .Bold().FontSize(8);

                            col.Item().Height(5);



                            var clausula5 =

                                "Las condiciones generales del vehículo materia de reparación y/o mantenimiento, son las siguientes: " +

                                $"Exteriores: {Chk(dto.C5ExtLimpiaparabrisas)} Limpiaparabrisas (plumas); {Chk(dto.C5ExtLuces)} Unidades de luces; {Chk(dto.C5ExtAntena)} Antena; {Chk(dto.C5ExtEspejos)} Espejos laterales; {Chk(dto.C5ExtCristales)} Cristales; {Chk(dto.C5ExtTapones)} Tapones de ruedas; {Chk(dto.C5ExtMolduras)} Molduras completas; {Chk(dto.C5ExtTaponGas)} Tapón de gasolina; {Chk(dto.C5ExtClaxon)} Claxon; " +

                                $"Interiores: {Chk(dto.C5IntInstrumentos)} Instrumentos del tablero; {Chk(dto.C5IntCalefaccion)} Calefacción; {Chk(dto.C5IntAire)} Aire acondicionado; {Chk(dto.C5IntRadio)} Radio/Tipo; {Chk(dto.C5IntBocinas)} Bocinas; {Chk(dto.C5IntEncendedor)} Encendedor; {Chk(dto.C5IntEspejoRet)} Espejo retrovisor; {Chk(dto.C5IntCeniceros)} Ceniceros; {Chk(dto.C5IntCinturones)} Cinturones de seguridad; {Chk(dto.C5IntTapetes)} Tapetes; {Chk(dto.C5IntManijas)} Manijas y/o controles interiores; {Chk(dto.C5IntEquipoAd)} Equipo adicional; {Chk(dto.C5IntAccesorios)} Accesorios; {Chk(dto.C5IntAditamentos)} Aditamentos especiales; {Chk(dto.C5IntOtros)} Otros. " +

                                "El vehículo se encuentra en las siguientes condiciones generales: Aspectos mecánicos: DESCRITOS EN LA HOJA DE INVENTARIO Y CONDICIONES DE LA UNIDAD, Aspectos de carrocería DESCRITOS EN LA HOJA DE INVENTARIO Y CONDICIONES DE LA UNIDAD.";



                            var clausula6 =

                                $"La prestación del servicio de reparación y/o mantenimiento del vehículo materia de este contrato, se otorga {Chk(dto.SinGarantia)} sin garantía; {Chk(dto.ConGarantia)} con garantía por un plazo de 90 días. " +

                                "(Art. 77 de la LFPC* no podrá ser inferior a 90 días) contados a partir de la entrega del vehículo. Para la garantía en partes, piezas, refacciones y accesorios, El Distribuidor transmitirá la otorgada por el fabricante, la garantía deberá hacerse válida en el domicilio, teléfonos y horarios de atención señalados en el catálogo o anverso del presente contrato, siempre y cuando no se haya efectuado una reparación por un tercero. El tiempo que dure la reparación y/o mantenimiento del vehículo, bajo la protección de la garantía, no es computable dentro del plazo de la misma. Las partes y/o refacciones empleadas en la reparación y/o mantenimiento del vehículo materia de este contrato, son nuevas y apropiadas para el funcionamiento del mismo. De igual forma, los gastos en que incurra el Cliente para hacer válida la garantía en un domicilio diverso al del Distribuidor, deberán ser cubiertos por éste último.";



                            var condiciones = new[]

                            {

                                ("1.",  "En virtud de este contrato (*), el Distribuidor presta el servicio de reparación y/o mantenimiento al Cliente (Consumidor), del vehículo cuyas características se detallan en este contrato."),

                                ("2.",  "El Cliente expresa ser el dueño del vehículo y/o estar facultado para autorizar la reparación y/o mantenimiento del vehículo descrito en el presente contrato, por lo que acepta las condiciones y términos bajo los cuales se realizará la prestación del servicio descrita en el presente contrato. Asimismo, es sabedor de las posibles consecuencias que puede sufrir el vehículo con motivo de su reparación y/o mantenimiento y se responsabiliza de las mismas. El consumidor acepta haber tenido a la vista los precios por mano de obra, refacciones y/o reparaciones a emplear en las operaciones a efectuar por parte del Distribuidor."),

                                ("3.",  "El precio total por concepto de la prestación del servicio de reparación y/o mantenimiento será cubierto en las instalaciones del Distribuidor y en moneda nacional en la forma y términos expresados en este contrato, incluyendo, en su caso, las partes y/o refacciones y los servicios adicionales que el cliente haya aceptado previamente."),

                                ("4.",  "En la situación de que el Cliente solicite, o en su caso, el Distribuidor avise al Cliente de servicios adicionales a los establecidos en el presente contrato, éste último los podrá autorizar vía telefónica. Asimismo, todas las quejas y sugerencias serán atendidas en el domicilio, teléfonos y horarios de atención señalados en el catálogo o anverso del presente contrato."),

                                ("5.",  clausula5),

                                ("6.",  clausula6),

                                ("7.",  "El Distribuidor será el responsable por las descomposturas, daños o pérdidas parciales o totales imputables a él, mientras el vehículo se encuentre bajo su resguardo para llevar a cabo la prestación del servicio de reparación y/o mantenimiento, o como consecuencia de la prestación del servicio, o bien, en el cumplimiento de la garantía, de acuerdo a lo establecido en el presente contrato. Asimismo, el Cliente autoriza al Distribuidor a usar el vehículo para efectos de prueba o verificación de las operaciones a realizar o realizadas. El Cliente libera al Distribuidor de cualquier responsabilidad que hubiere surgido o pudiera surgir con relación al origen, propiedad o posesión del vehículo."),

                                ("8.",  "El cliente podrá revocar su consentimiento, en un plazo de 5 días hábiles mediante aviso personal, correo electrónico o correo certificado, siempre y cuando no se hayan iniciado los trabajos de reparación y/o mantenimiento."),

                                ("9.",  "Las causas que autoricen una cancelación se darán a conocer al cliente."),

                                ("10.", "En caso de que el consumidor cancele la operación, está obligado a pagar de manera inmediata y previa a la entrega del vehículo, el importe de las operaciones efectuadas y partes y/o refacciones colocadas o adquiridas hasta el retiro del mismo."),

                                ("11.", "Son causas de rescisión del presente contrato: (i) Que el Distribuidor incumpla en la fecha y lugar de entrega del vehículo por causas imputables a él. -El Cliente le notificará por escrito el incumplimiento de dicha obligación y el Distribuidor entregará de manera inmediata el vehículo, debiendo descontar del monto total de la operación, la cantidad equivalente al 15% por concepto de pena convencional (ii) Que el Cliente incumpla con su obligación de pago. -En el evento que el Cliente incumpla con el pago por concepto de la reparación y/o mantenimiento del vehículo, el Distribuidor le notificará por escrito su incumplimiento y podrá exigirle la rescisión o cumplimiento del contrato en mora, más la pena convencional equivalente al 15% del precio total por concepto de la reparación y/o mantenimiento, más las costas judiciales entre las partes."),

                                ("12.", "El Consumidor deberá recoger el vehículo en la fecha y lugar establecida en el presente contrato, en caso contrario, se obliga a pagar al Distribuidor, la cantidad que resulte por concepto de almacenaje del vehículo por cada día que transcurra, tomando como referencia una tarifa no mayor al precio general establecido para estacionamientos públicos ubicados en la cercanía del Distribuidor, después de un período de 15 días naturales a partir de la fecha o plazo de entrega del vehículo, y el Cliente no acuda a recoger el mismo, el Distribuidor sin responsabilidad alguna, pondrá a disposición de la autoridad correspondiente dicho vehículo. Sin perjuicio de lo anterior, el Distribuidor podrá realizar el otro correspondiente por concepto de almacenaje."),

                                ("13.", "El Distribuidor se obliga a: expedir la factura o comprobante de pago por las operaciones efectuadas, en la cual se especificarán los precios por mano de obra, refacciones, materiales y accesorios empleados, así como la garantía en términos del artículo 13 fracción IV y artículo 82 de la Ley Federal de Protección al Consumidor."),

                                ("14.", "El Distribuidor se obliga a: (i) No ceder o transmitir a terceros, con fines mercadotécnicos o publicitarios, los datos e información proporcionada por el cliente con motivo del presente contrato (ii) No enviar publicidad sobre bienes y servicios, salvo autorización expresa del Cliente en la presente cláusula."),

                                ("15.", "Las partes están de acuerdo en someterse a la competencia de la Procuraduría Federal del Consumidor en la vía administrativa para resolver cualquier controversia que se suscite sobre la interpretación o cumplimiento de los términos y condiciones del presente contrato y de las disposiciones de la Ley Federal de Protección al Consumidor, la Norma Oficial Mexicana NOM-174-SCFI-2007, Prácticas comerciales-Elementos de información para la prestación de servicios en general y cualquier otra disposición aplicable, sin perjuicio del derecho que tienen las partes de someterse a la jurisdicción de los Tribunales competentes del domicilio del Distribuidor, renunciando las partes expresamente a cualquier otra jurisdicción que pudiera corresponderles por razón de sus domicilios futuros."),

                                ("16.", "El Cliente y el Distribuidor aceptan la realización de la prestación del servicio de reparación y/o mantenimiento, en los términos establecidos en este contrato, y sabedores de su alcance legal, lo firman por duplicado."),

                                ("17.", "Consentimiento por medios electrónicos. Las partes acuerdan que en lugar de una firma original autógrafa, este contrato, así como cualquier consentimiento, aprobación u otros documentos relacionados con el mismo, podrán ser firmados por medio del uso de firmas electrónicas, digitales, numéricas, alfanuméricas, huellas de voz, biométricas o de cualquier otra forma y que dichos medios alternativos de firma y los registros en donde sean aplicadas dichas firmas, serán consideradas para todos los efectos, incluyendo pero no limitado a la legislación civil, mercantil, protección al consumidor y a la NOM-151-SCFI-2016, con la misma fuerza y consecuencia que la firma autógrafa original física de la parte firmante. Si el contrato o cualquier otro documento relacionado con el mismo es firmado por medios electrónicos o digitales, las Partes acuerdan que los formatos del contrato y los demás documentos firmados de tal modo serán conservados y estarán a disposición del consumidor, por lo que convienen que cada una y toda la información enviada por el Distribuidor a la dirección de correo electrónico proporcionada por el Consumidor al momento de celebrar el presente Contrato será considerada como entregada en el momento en que la misma es enviada, siempre y cuando exista confirmación de recepción."),

                            };



                            col.Item().Column(c =>

                            {

                                foreach (var (num, texto) in condiciones)

                                    ClausulaItem(c, num, texto);

                            });



                            col.Item().Height(6);



                            // Firma autorización consumidor

                            col.Item().AlignCenter().Text("Firma o rúbrica de autorización del consumidor").FontSize(7);

                            col.Item().Height(4);

                            col.Item().AlignCenter().Column(c =>

                            {

                                if (bytesExtra != null)

                                    c.Item().Width(200).Height(40).Image(bytesExtra).FitHeight();

                                else

                                    c.Item().Width(200).Height(40).BorderBottom(0.5f).Text("");

                            });

                            col.Item().Height(8);



                            // ══════ FIRMAS PÁGINA 1 ══════════════════════════════

                            col.Item().Row(row =>

                            {

                                row.RelativeItem().Column(c =>

                                {

                                    c.Item().AlignCenter().Text("EL DISTRIBUIDOR").Bold();

                                    c.Item().AlignCenter().Text("(Nombre y Firma)").FontSize(6.5f);

                                    if (bytesDistribuidor != null)

                                        c.Item().Height(35).AlignCenter().Image(bytesDistribuidor).FitHeight();

                                    else

                                        c.Item().Height(35).BorderBottom(0.5f).Text("");

                                    c.Item().AlignCenter().Text(dto.Denominacion).FontSize(7);

                                });

                                row.ConstantItem(20);

                                row.RelativeItem().Column(c =>

                                {

                                    c.Item().AlignCenter().Text("EL CLIENTE").Bold();

                                    c.Item().AlignCenter().Text("(Nombre y Firma)").FontSize(6.5f);

                                    if (bytesCliente != null)

                                        c.Item().Height(35).AlignCenter().Image(bytesCliente).FitHeight();

                                    else

                                        c.Item().Height(35).BorderBottom(0.5f).Text("");

                                    c.Item().AlignCenter().Text(dto.NombreCliente).FontSize(7);

                                });

                            });



                            col.Item().Height(6);

                            col.Item().Text("(*) El presente contrato fue registrado en la Procuraduría Federal del Consumidor bajo el número 9829-2023 de fecha 24 de noviembre de 2023.").FontSize(6.5f).Italic();

                            col.Item().Text("*LFPC - Ley Federal de Protección al Consumidor").FontSize(6.5f).Italic();



                            // ══════ HOJA DE INVENTARIO (página 2) ═══════════════

                            col.Item().PageBreak();



                            col.Item().Row(row =>

                            {

                                row.ConstantItem(80).Column(c =>

                                {

                                    if (logoBytes != null) c.Item().Image(logoBytes).FitArea();

                                });

                                row.ConstantItem(8);

                                row.RelativeItem(3).Column(c =>

                                {

                                    c.Item().Text(dto.Denominacion).Bold();

                                    c.Item().Text($"Domicilio: {dto.Domicilio}");

                                    c.Item().Text($"RFC: {dto.Rfc}");

                                    c.Item().Text($"Tel: {dto.Telefono}   Horarios: {dto.HorarioAtencion}");

                                    c.Item().Text($"Fax: N/A   Email: {dto.CorreoDistribuidora}");

                                });

                                row.ConstantItem(8);

                                row.RelativeItem(2).Column(c =>

                                {

                                    c.Item().Border(0.5f).Padding(3).Column(inner =>

                                    {

                                        inner.Item().Text($"ORDEN DE REPARACIÓN: {dto.NumOrden}").Bold();

                                        inner.Item().Text($"FECHA: {dto.FechaOrden}").Bold();

                                    });

                                    c.Item().Height(4);

                                    c.Item().Column(inner =>

                                    {

                                        inner.Item().Text("LOCALIDAD:").Bold();

                                        inner.Item().Text(dto.Localidad);

                                    });

                                });

                            });



                            col.Item().Height(5);

                            col.Item().Row(r =>

                            {

                                r.AutoItem().Text("Asesor:  ");

                                r.RelativeItem().BorderBottom(0.5f).Text(dto.Asesor).Bold();

                                r.ConstantItem(10);

                                r.AutoItem().Text("Teléfono:  ");

                                r.RelativeItem().BorderBottom(0.5f).Text(dto.TelAsesor);

                                r.ConstantItem(10);

                                r.AutoItem().Text("Correo:  ");

                                r.RelativeItem().BorderBottom(0.5f).Text(dto.EmailAsesor);

                            });

                            col.Item().Height(3);

                            col.Item().Row(r =>

                            {

                                r.AutoItem().Text("NOMBRE DEL CLIENTE:  ");

                                r.RelativeItem().BorderBottom(0.5f).Text(dto.NombreCliente).Bold();

                            });

                            col.Item().Height(5);

                            col.Item().AlignCenter().Text("INVENTARIO Y CONDICIONES DE LA UNIDAD").Bold().FontSize(9);

                            col.Item().Height(4);

                            col.Item().Row(r =>

                            {

                                r.AutoItem().Text("Hora Entrega:  ");

                                r.RelativeItem().BorderBottom(0.5f).Text(dto.HoraEntrega);

                                r.ConstantItem(8);

                                r.AutoItem().Text("Placas:  ");

                                r.RelativeItem().BorderBottom(0.5f).Text(dto.Placas);

                                r.ConstantItem(8);

                                r.AutoItem().Text("VIN:  ");

                                r.RelativeItem().BorderBottom(0.5f).Text(dto.Niv);

                                r.ConstantItem(8);

                                r.AutoItem().Text("Kilometraje:  ");

                                r.RelativeItem().BorderBottom(0.5f).Text(dto.KmRecorridos);

                                r.ConstantItem(8);

                                r.AutoItem().Text("Torre:  ");

                                r.RelativeItem().BorderBottom(0.5f).Text(dto.Piramide);

                            });

                            col.Item().Height(5);



                            // ── Tabla inventario ─────────────────────────────────

                            col.Item().Border(0.5f).Row(inv =>

                            {

                                inv.RelativeItem().BorderRight(0.5f).Column(c =>

                                {

                                    c.Item().BorderBottom(0.5f).Padding(2)

                                     .AlignCenter().Text("INTERIOR").Bold().FontSize(7.5f);

                                    c.Item().Padding(3).Row(r =>

                                    {

                                        r.RelativeItem().Column(col2 =>

                                        {

                                            CheckItemDyn(col2, "Tapetes", dto.InvTapetes);

                                            CheckItemDyn(col2, "Ceniceros", dto.InvCeniceros);

                                            CheckItemDyn(col2, "Bocinas", dto.InvBocinas);

                                            CheckItemDyn(col2, "Instrumentos", dto.InvInstrumentos);

                                            CheckItemDyn(col2, "Encendedores", dto.InvEncendedores);

                                            CheckItemDyn(col2, "Radio", dto.InvRadio);

                                            CheckItemDyn(col2, "Claxon", dto.InvClaxon);

                                            CheckItemDyn(col2, "A/C", dto.InvAC);

                                            CheckItemDyn(col2, "Retrovisor", dto.InvRetrovisor);

                                            CheckItemDyn(col2, "Manijas", dto.InvManijas);

                                            CheckItemDyn(col2, "Vestiduras", dto.InvVestiduras);

                                            CheckItemDyn(col2, "Cinturones", dto.InvCinturones);

                                            CheckItemDyn(col2, "Manual Prop.", dto.InvManualProp);

                                            CheckItemDyn(col2, "Carnet servicio", dto.InvCarnetServicio);

                                        });

                                        r.ConstantItem(4);

                                        r.RelativeItem().Column(col2 =>

                                        {

                                            CheckItemDyn(col2, "Tjta. Circulacion", dto.InvTarjetaCirculacion);

                                            CheckItemDyn(col2, "Poliza de Seguro", dto.InvPolizaSeguro);

                                            CheckItemDyn(col2, "Verificación", dto.InvVerificacion);

                                            CheckItemDyn(col2, "Alfombrado Caj.", dto.InvAlfombradoCaj);

                                            CheckItemDyn(col2, "Llanta refaccion", dto.InvLlantaRefaccion);

                                            CheckItemDyn(col2, "Triángulos", dto.InvTriangulos);

                                            CheckItemDyn(col2, "Extintor", dto.InvExtintor);

                                            CheckItemDyn(col2, "Cables batería", dto.InvCablesBateria);

                                            CheckItemDyn(col2, "Gato", dto.InvGato);

                                            CheckItemDyn(col2, "Herramientas", dto.InvHerramientas);

                                            CheckItemDyn(col2, "Botiquín", dto.InvBotiquin);

                                            CheckItemDyn(col2, "Red protectora", dto.InvRedProtectora);

                                            CheckItemDyn(col2, "Birlo de seguridad", dto.InvBirloSeguridad);

                                            col2.Item().Text("Otros:").FontSize(7);

                                        });

                                    });

                                });



                                inv.RelativeItem().BorderRight(0.5f).Column(c =>

                                {

                                    c.Item().BorderBottom(0.5f).Padding(2)

                                     .AlignCenter().Text("EXTERIOR").Bold().FontSize(7.5f);

                                    c.Item().Padding(3).Column(col2 =>

                                    {

                                        CheckItemDyn(col2, "Cristales", dto.ExtCristales);

                                        CheckItemDyn(col2, "Limpiadores", dto.ExtLimpiadores);

                                        CheckItemDyn(col2, "Tapones", dto.ExtTapones);

                                        CheckItemDyn(col2, "Faros de niebla", dto.ExtFarosNiebla);

                                        CheckItemDyn(col2, "Antena", dto.ExtAntena);

                                        CheckItemDyn(col2, "Tapón Gas", dto.ExtTaponGas);

                                        CheckItemDyn(col2, "Molduras", dto.ExtMolduras);

                                        CheckItemDyn(col2, "Espejos", dto.ExtEspejos);

                                        CheckItemDyn(col2, "Faros delanteros", dto.ExtFarosDelanteros);

                                        CheckItemDyn(col2, "Luces Traseras", dto.ExtLucesTraseras);

                                        CheckItemDyn(col2, "Golpes", dto.ExtGolpes);

                                        col2.Item().Text("Otros:").FontSize(7);

                                    });

                                });



                                inv.RelativeItem().BorderRight(0.5f).Column(c =>

                                {

                                    c.Item().BorderBottom(0.5f).Padding(2)

                                     .AlignCenter().Text("TESTIGOS ENCENDIDOS").Bold().FontSize(7f);

                                    c.Item().Padding(2).Column(col2 =>

                                    {

                                        CheckItemDyn(col2, "Llantas", dto.TestLlantas);

                                        CheckItemDyn(col2, "Check engine", dto.TestCheckEngine);

                                        CheckItemDyn(col2, "Vscrack", dto.TestVscrack);

                                        CheckItemDyn(col2, "Presión aceite", dto.TestPresionAceite);

                                        CheckItemDyn(col2, "Control Estabilidad", dto.TestControlEstabilidad);

                                        CheckItemDyn(col2, "Bolsas de Aire", dto.TestBolsasAire);

                                        CheckItemDyn(col2, "Batería", dto.TestBateria);

                                        CheckItemDyn(col2, "Temperatura", dto.TestTemperatura);

                                    });

                                });



                                inv.RelativeItem().Column(c =>

                                {

                                    c.Item().BorderBottom(0.5f).Padding(2)

                                     .AlignCenter().Text("OBSERVACIONES:").Bold().FontSize(7.5f);

                                    c.Item().Padding(3).Column(items =>

                                    {

                                        items.Item().Text("SIMBOLOGÍA DE DAÑOS").Bold().FontSize(6.5f);

                                        items.Item().Height(2);

                                        items.Item().Row(r =>

                                        {

                                            r.ConstantItem(14).Background("#dc3545").AlignCenter()

                                             .Text("X").FontColor("#FFFFFF").Bold().FontSize(7f);

                                            r.ConstantItem(4);

                                            r.RelativeItem().Text("GOLPE").FontSize(6.5f);

                                        });

                                        items.Item().Height(2);

                                        items.Item().Row(r =>

                                        {

                                            r.ConstantItem(14).Background("#fd7e14").AlignCenter()

                                             .Text("~").FontColor("#FFFFFF").Bold().FontSize(7f);

                                            r.ConstantItem(4);

                                            r.RelativeItem().Text("RAYÓN").FontSize(6.5f);

                                        });

                                        items.Item().Height(5);



                                        if (danos.Count > 0)

                                        {

                                            items.Item().Text("DAÑOS:").Bold().FontSize(6f);

                                            items.Item().Height(2);

                                            foreach (var d in danos)

                                            {

                                                var simbolo = d.Type == "golpe" ? "X" : "~";

                                                var colorD = d.Type == "golpe" ? "#dc3545" : "#fd7e14";

                                                var tipoD = d.Type == "golpe" ? "Golpe" : "Rayón";

                                                items.Item().Row(r =>

                                                {

                                                    r.ConstantItem(12).Background(colorD).AlignCenter()

                                                     .Text(simbolo).FontColor("#FFFFFF").Bold().FontSize(6f);

                                                    r.ConstantItem(3);

                                                    r.RelativeItem()

                                                     .Text($"{tipoD} ({(d.X * 100):F0}%, {(d.Y * 100):F0}%)")

                                                     .FontSize(6f);

                                                });

                                                items.Item().Height(2);

                                            }

                                            items.Item().Height(3);

                                        }



                                        // Medidor gasolina

                                        items.Item().Text("GASOLINA").Bold().FontSize(6.5f);

                                        items.Item().Height(3);

                                        var nivel = dto.NivelGasolina ?? 50;

                                        items.Item().Row(gaugeRow =>

                                        {

                                            gaugeRow.AutoItem().Text("E").Bold().FontSize(7.5f).FontColor("#dc2626");

                                            gaugeRow.ConstantItem(2);

                                            gaugeRow.RelativeItem().Column(barCol =>

                                            {

                                                barCol.Item().Row(bar =>

                                                {

                                                    var s1f = Math.Min(nivel, 25);

                                                    if (s1f > 0) bar.RelativeItem(s1f).Background("#dc2626").Height(10);

                                                    if (25 - s1f > 0) bar.RelativeItem(25 - s1f).Background("#fee2e2").Height(10);

                                                    var s2f = Math.Max(0, Math.Min(nivel - 25, 25));

                                                    if (s2f > 0) bar.RelativeItem(s2f).Background("#f97316").Height(10);

                                                    if (25 - s2f > 0) bar.RelativeItem(25 - s2f).Background("#ffedd5").Height(10);

                                                    var s3f = Math.Max(0, Math.Min(nivel - 50, 25));

                                                    if (s3f > 0) bar.RelativeItem(s3f).Background("#eab308").Height(10);

                                                    if (25 - s3f > 0) bar.RelativeItem(25 - s3f).Background("#fef9c3").Height(10);

                                                    var s4f = Math.Max(0, Math.Min(nivel - 75, 25));

                                                    if (s4f > 0) bar.RelativeItem(s4f).Background("#22c55e").Height(10);

                                                    if (25 - s4f > 0) bar.RelativeItem(25 - s4f).Background("#dcfce7").Height(10);

                                                });

                                                barCol.Item().Row(marks =>

                                                {

                                                    marks.RelativeItem(25);

                                                    marks.ConstantItem(1).Background("#64748b").Height(4);

                                                    marks.RelativeItem(25);

                                                    marks.ConstantItem(1).Background("#64748b").Height(4);

                                                    marks.RelativeItem(25);

                                                    marks.ConstantItem(1).Background("#64748b").Height(4);

                                                    marks.RelativeItem(25);

                                                });

                                                barCol.Item().Row(labels =>

                                                {

                                                    labels.RelativeItem().AlignLeft().Text("¼").FontSize(5f).FontColor("#64748b");

                                                    labels.RelativeItem().AlignCenter().Text("½").FontSize(5f).FontColor("#64748b");

                                                    labels.RelativeItem().AlignRight().Text("¾").FontSize(5f).FontColor("#64748b");

                                                });

                                            });

                                            gaugeRow.ConstantItem(2);

                                            gaugeRow.AutoItem().Text("F").Bold().FontSize(7.5f).FontColor("#16a34a");

                                        });

                                        items.Item().Height(2);

                                        items.Item().AlignCenter()

                                             .Text($"{nivel}%").Bold().FontSize(7f)

                                             .FontColor(nivel <= 25 ? "#dc2626" :

                                                        nivel <= 50 ? "#f97316" :

                                                        nivel <= 75 ? "#eab308" : "#16a34a");

                                    });

                                });

                            });



                            col.Item().Height(4);



                            // ══════ IMAGEN DEL COCHE CON DAÑOS SUPERPUESTOS ══════

                            if (cochesBytes != null)
                            {
                                col.Item()
                                   .Layers(layers =>
                                   {
                                       // Capa base: imagen original del coche
                                       layers.PrimaryLayer()
                                             .Image(cochesBytes)
                                             .FitWidth(); // <-- Cambiado de FitArea a FitWidth para coincidir con el cálculo matemático

                                       // Una capa por cada daño registrado
                                       foreach (var d in danos)
                                       {
                                           bool esGolpe = d.Type == "golpe";
                                           string colorMar = esGolpe ? "#DC3545" : "#FD7E14";
                                           string simbolo = esGolpe ? "X" : "~";

                                           // Centro del marcador en mm según coordenadas relativas
                                           float cx = (float)(d.X * anchoImagenMm);
                                           float cy = (float)(d.Y * altoImagenMm);

                                           // Padding desde esquina sup-izq para centrar el cuadro del marcador
                                           float padL = Math.Max(0f, cx - marcadorMm / 2f);
                                           float padT = Math.Max(0f, cy - marcadorMm / 2f);

                                           layers.Layer()
                                                 // SE AÑADIÓ explícitamente Unit.Millimetre a las propiedades espaciales
                                                 .PaddingLeft(padL, Unit.Millimetre)
                                                 .PaddingTop(padT, Unit.Millimetre)
                                                 .Width(marcadorMm, Unit.Millimetre)
                                                 .Height(marcadorMm, Unit.Millimetre)
                                                 .Background(colorMar)
                                                 .Border(0.5f)
                                                 .BorderColor("#FFFFFF")
                                                 .AlignCenter()
                                                 .AlignMiddle()
                                                 .Text(simbolo)
                                                     .FontColor("#FFFFFF")
                                                     .Bold()
                                                     .FontSize(4);
                                       }
                                   });
                            }



                            col.Item().Height(4);



                            // Comentarios

                            col.Item().AlignCenter().Text("COMENTARIOS").Bold().FontSize(8);

                            col.Item().Height(3);

                            col.Item().Border(0.5f).MinHeight(60).Padding(4)

                               .Text(dto.Comentarios ?? "").FontSize(7.5f);



                            col.Item().Height(5);

                            col.Item().Row(r =>

                            {

                                r.AutoItem().Text("Conductor o Contacto: Distinto al Propietario ( si ) ( no )  ");

                                r.RelativeItem().BorderBottom(0.5f).Text(dto.Conductor ?? "");

                            });

                            col.Item().Height(3);

                            col.Item().Row(r =>

                            {

                                r.AutoItem().Text($"¿Desea ser contactado? {Chk(dto.DeseaContacto)} SI  {Chk(!dto.DeseaContacto)} NO   Medio de Contacto: ");

                                r.RelativeItem().BorderBottom(0.5f).Text(dto.MedioContacto ?? "");

                                r.ConstantItem(10);

                                r.AutoItem().Text("Teléfono:  ");

                                r.RelativeItem().BorderBottom(0.5f).Text(dto.TelContacto ?? "");

                            });

                            col.Item().Height(3);

                            col.Item().Row(r =>

                            {

                                r.AutoItem().Text("KATASHIKI:  ");

                                r.RelativeItem().BorderBottom(0.5f).Text(dto.Katashiki ?? "");

                                r.ConstantItem(15);

                                r.AutoItem().Text("FECHA DOFU:  ");

                                r.RelativeItem().BorderBottom(0.5f).Text(dto.FechaDofu ?? "");

                            });



                            col.Item().Height(8);



                            // ══════ FIRMAS INVENTARIO ════════════════════════════

                            col.Item().Row(row =>

                            {

                                row.RelativeItem().Column(c =>

                                {

                                    c.Item().AlignCenter().Text("EL DISTRIBUIDOR").Bold();

                                    c.Item().AlignCenter().Text("(Nombre y Firma)").FontSize(6.5f);

                                    if (bytesDistribuidor != null)

                                        c.Item().Height(30).AlignCenter().Image(bytesDistribuidor).FitHeight();

                                    else

                                        c.Item().Height(30).BorderBottom(0.5f).Text("");

                                    c.Item().AlignCenter().Text("Geisha Querétaro, S de RL de CV").FontSize(7);

                                });

                                row.ConstantItem(40);

                                row.RelativeItem().Column(c =>

                                {

                                    c.Item().AlignCenter().Text("EL CLIENTE").Bold();

                                    c.Item().AlignCenter().Text("(Nombre y Firma)").FontSize(6.5f);

                                    if (bytesCliente != null)

                                        c.Item().Height(30).AlignCenter().Image(bytesCliente).FitHeight();

                                    else

                                        c.Item().Height(30).BorderBottom(0.5f).Text("");

                                    c.Item().AlignCenter().Text(dto.NombreCliente ?? "").FontSize(7);

                                });

                            });



                            col.Item().Height(5);

                            col.Item().Text("Nota: Agradecemos su preferencia, el lavado de su vehículo es de cortesía y no se hace responsable por objetos olvidados o no reportados dentro del vehículo.").FontSize(6.5f).Italic();

                        });

                    });

                });



                var bytes = pdf.GeneratePdf();

                return File(bytes, "application/pdf", $"OrdenServicio_{dto.NumOrden}_{dto.NombreCliente}.pdf");

            }

            catch (Exception ex)

            {

                return StatusCode(500, ex.Message);

            }

        }

        private static (int Width, int Height) GetImageDimensions(byte[] imageBytes)

        {

            if (imageBytes == null || imageBytes.Length < 12)

                return (1, 1);



            // PNG: bytes 0-7 son la firma; ancho en [16-19], alto en [20-23] (big-endian)

            if (imageBytes[0] == 0x89 && imageBytes[1] == 0x50 &&

                imageBytes[2] == 0x4E && imageBytes[3] == 0x47 &&

                imageBytes.Length >= 24)

            {

                int w = (imageBytes[16] << 24) | (imageBytes[17] << 16) |

                        (imageBytes[18] << 8) | imageBytes[19];

                int h = (imageBytes[20] << 24) | (imageBytes[21] << 16) |

                        (imageBytes[22] << 8) | imageBytes[23];

                return (w, h);

            }



            // JPEG: buscar marcador SOFx (FF C0..C3, C5..C7, C9..CB, CD..CF)

            // Estructura SOF: FF [marker] [len 2b] [precision 1b] [alto 2b] [ancho 2b]

            if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)

            {

                int i = 2;

                while (i < imageBytes.Length - 8)

                {

                    if (imageBytes[i] != 0xFF) { i++; continue; }

                    while (i < imageBytes.Length && imageBytes[i] == 0xFF) i++;

                    if (i >= imageBytes.Length) break;



                    byte marker = imageBytes[i++];



                    if ((marker >= 0xC0 && marker <= 0xC3) ||

                        (marker >= 0xC5 && marker <= 0xC7) ||

                        (marker >= 0xC9 && marker <= 0xCB) ||

                        (marker >= 0xCD && marker <= 0xCF))

                    {

                        if (i + 4 >= imageBytes.Length) break;

                        i += 3; // saltar longitud (2b) + precisión (1b)

                        int h = (imageBytes[i] << 8) | imageBytes[i + 1]; i += 2;

                        int w = (imageBytes[i] << 8) | imageBytes[i + 1];

                        return (w, h);

                    }



                    // Marcadores sin segmento de datos

                    if (marker == 0xD8 || marker == 0xD9 ||

                        (marker >= 0xD0 && marker <= 0xD7))

                        continue;



                    // Saltar segmento: leer longitud big-endian y avanzar

                    if (i + 1 >= imageBytes.Length) break;

                    int segLen = (imageBytes[i] << 8) | imageBytes[i + 1];

                    i += segLen;

                }

            }



            return (1, 1); // fallback

        }

        // ── Helpers QuestPDF ─────────────────────────────────────────────────────────
        private static void LabelValue(ColumnDescriptor col, string label, string? value)

        {

            col.Item().Row(r =>

            {

                r.AutoItem().Text(label).Bold();

                r.ConstantItem(4);

                r.RelativeItem().Text(value ?? "");

            });

            col.Item().Height(2);

        }

        private static void MontoRow(ColumnDescriptor col, string label, string? valor, bool bold = false)

        {

            col.Item().Row(r =>

            {

                r.RelativeItem().Text(t => { var s = t.Span(label); if (bold) s.Bold(); });

                r.ConstantItem(4);

                r.AutoItem().Text(t => { var s = t.Span(valor ?? ""); if (bold) s.Bold(); });

            });

            col.Item().Height(2);

        }

        private static void ClausulaItem(ColumnDescriptor col, string numero, string texto)

        {

            col.Item().Text(t =>

            {

                t.Justify();

                t.Span(numero + " ").Bold();

                t.Span(texto);

            });

            col.Item().Height(4);

        }

        private static void CheckItemDyn(ColumnDescriptor col, string label, bool checked_)

        {

            col.Item().Row(r =>

            {

                r.AutoItem().Text(checked_ ? "(✓)  " : "( )  ").FontSize(7);

                r.RelativeItem().Text(label).FontSize(7);

            });

            col.Item().Height(1);

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
                    return NotFound(new { mensaje = $"No se encontró la orden con ID: {dto.IdVenta}" });

                await _operDocumentosRepositorio.GuardarDocumentoAsync(
                    dto, ventaMov.Mov, ventaMov.MovId, "Contrato Adhesion");

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al guardar el documento.", detalle = ex.Message });
            }
        }

    }
}
