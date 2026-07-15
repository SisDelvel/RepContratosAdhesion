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

    public class ContratoNuevosController : Controller
    {
        private readonly IRepositorioContratoNuevos repositorio;


        public ContratoNuevosController(IRepositorioContratoNuevos repositorio)
        {
            this.repositorio = repositorio;
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
                var datos = await repositorio.ObtenerDatosContratoAsync(ventaId, negocio); // ← nuevo
                if (datos == null)
                    return NotFound(new { mensaje = $"La venta con ID {ventaId} no existe en el sistema." });
                return Ok(datos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno al procesar el contrato.", detalle = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GuardarContrato([FromBody] GuardarContratoNuevoDto dto)
        {
            try
            {
                var negocio = HttpContext.Session.GetInt32("Negocio") ?? 1; // ← nuevo
                await repositorio.GuardarContratoAsync(dto, negocio); // ← nuevo
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }


        public async Task<IActionResult> GenerarPDFContrato(int ventaId)
        {
            var negocio = HttpContext.Session.GetInt32("Negocio") ?? 1; // ← nuevo
            var dto = await repositorio.ObtenerDatosContratoAsync(ventaId.ToString(), negocio); // ← nuevo

            if (dto is null)
                return NotFound(new { mensaje = $"La venta con ID {ventaId} no existe." });
            QuestPDF.Settings.License = LicenseType.Community;

            var pdf = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(7.5f).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // ════════════════════════════════════════════════════
                        // PÁGINA 1
                        // ════════════════════════════════════════════════════

                        // ── Header: Distribuidora | Folio/Fecha | Localidad ──
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(3).Column(c =>
                            {
                                c.Item().Text($"Denominacion: {dto.Denominacion ?? "GEISHA QUERÉTARO, S. DE R.L. DE C.V"}").Bold();
                                c.Item().Text($"RFC: {dto.Rfc}");
                                c.Item().Text($"Domicilio: {dto.Domicilio}");
                                c.Item().Text($"Tel: {dto.Telefono}    Fax:");
                                c.Item().Text("Horarios de Atencion:");
                                c.Item().Text("Lunes a Viernes de 9:00 a 19:00 hrs, Sabado 9:00 a 14:00 hrs");
                                c.Item().Text($"Correo Electronico: {dto.CorreoDistribuidora ?? "atencionclientes@toyotaqueretaro.mx"}");
                            });

                            row.ConstantItem(10);

                            row.RelativeItem(2).Column(c =>
                            {
                                c.Item().Text($"FOLIO: {dto.Folio}").Bold().FontSize(9);
                                c.Item().Height(8);
                                c.Item().Text($"FECHA: {dto.Fecha}").Bold();
                            });

                            row.ConstantItem(10);

                            row.RelativeItem(2).Column(c =>
                            {
                                c.Item().Text("LOCALIDAD:").Bold();
                                c.Item().Text(dto.Localidad ?? "");
                            });
                        });

                        col.Item().Height(6);

                        // ── Sección: Datos del Cliente ───────────────────────
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
                                    r.RelativeItem(1).Column(c =>
                                    {
                                        c.Item().BorderBottom(0.5f).Text(dto.RfcCliente);
                                        c.Item().Text("R.F.C.:").FontSize(6.5f);
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
                                        c.Item().BorderBottom(0.5f).Text($"Ext. {dto.NumExt}  Int. {dto.NumInt}");
                                        c.Item().Text("(Número exterior e interior)").FontSize(6.5f);
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
                                        c.Item().BorderBottom(0.5f).Text(dto.Estado ?? "");
                                        c.Item().Text("(Estado)").FontSize(6.5f);
                                    });
                                });

                                inner.Item().Height(3);

                                inner.Item().Row(r =>
                                {
                                    r.RelativeItem(1).Column(c =>
                                    {
                                        c.Item().BorderBottom(0.5f).Text(dto.Telefonos);
                                        c.Item().Text("(Teléfonos)").FontSize(6.5f);
                                    });
                                    r.ConstantItem(6);
                                    r.RelativeItem(2).Column(c =>
                                    {
                                        c.Item().BorderBottom(0.5f).Text(dto.Correo);
                                        c.Item().Text("(Correo electrónico)").FontSize(6.5f);
                                    });
                                });
                            });
                        });

                        col.Item().Height(5);

                        // ── Sección: Características del Vehículo ────────────
                        col.Item().Border(0.5f).Column(sec =>
                        {
                            sec.Item().BorderBottom(0.5f).Padding(2)
                            .Text("CARACTERÍSTICAS DEL VEHÍCULO")
                            .Bold().FontSize(7.5f);

                            sec.Item().Padding(3).Row(r =>
                            {
                                r.RelativeItem().Column(c =>
                                {
                                    LabelValue(c, "Marca", dto.Marca);
                                    LabelValue(c, "Submarca", dto.Submarca);
                                    LabelValue(c, "Tipo o versión:", dto.TipoVersion);
                                    LabelValue(c, "Color:", dto.Color);
                                    c.Item().Height(4);
                                    LabelValue(c, "Año-Modelo", dto.AnioModelo);
                                });

                                r.ConstantItem(10);

                                r.RelativeItem().Column(c =>
                                {
                                    LabelValue(c, "Catálogo", dto.Catalogo);
                                    LabelValue(c, "Número Identificación Vehicular:", dto.Niv);
                                    LabelValue(c, "Capacidad:", dto.Capacidad);
                                    LabelValue(c, "Fecha de entrega del vehículo:", dto.FechaEntrega ?? "");
                                    c.Item().Height(4);
                                    LabelValue(c, "Lugar de entrega del Vehículo:", dto.LugarEntrega ?? "");
                                });
                            });
                        });

                        col.Item().Height(5);

                        // ── Monto de la Operación | Forma de Pago ────────────
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Border(0.5f).Column(sec =>
                            {
                                sec.Item().BorderBottom(0.5f).Padding(2)
                                .Text("MONTO DE LA OPERACIÓN")
                                .Bold().FontSize(7.5f);

                                sec.Item().Padding(3).Column(c =>
                                {
                                    MontoRow(c, "Precio del vehículo:", dto.PrecioVehiculo);
                                    MontoRow(c, "Equipo y accesorios adicionales(ver análisis):", dto.Equipo);
                                    MontoRow(c, "Otros cargos:", dto.OtrosCargos);
                                    MontoRow(c, "Impuestos al valor agregado:", dto.Iva);
                                    c.Item().Height(3);
                                    MontoRow(c, "Monto total de la operación:", dto.MontoTotal, bold: true);
                                });
                            });

                            row.ConstantItem(4);

                            row.RelativeItem().Border(0.5f).Column(sec =>
                            {
                                sec.Item().BorderBottom(0.5f).Padding(2)
                                .Text("FORMA DE PAGO")
                                .Bold().FontSize(7.5f);

                                sec.Item().Padding(3).Column(c =>
                                {
                                    MontoRow(c, "Contado:", dto.PagoContado);
                                    MontoRow(c, "Enganche o Unidad usada a cuenta:", dto.Enganche);
                                    c.Item().Text("(Ver Descripción)").FontSize(6.5f);
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
                                .Text("DESCRIPCIÓN UNIDAD USADA:")
                                .Bold().FontSize(7.5f);
                                sec.Item().Padding(3).Column(c =>
                                {
                                    LabelValue(c, "Número de identificación vehicular:", dto.UsadaNiv ?? "");
                                    LabelValue(c, "Marca:", dto.UsadaMarca ?? "");
                                    LabelValue(c, "Submarca:", dto.UsadaSubmarca ?? "");
                                    LabelValue(c, "Tipo o versión:", dto.UsadaVersion ?? "");
                                    LabelValue(c, "Color:", dto.UsadaColor ?? "");
                                    LabelValue(c, "Año-modelo:", dto.UsadaAnio ?? "");
                                    LabelValue(c, "Valor de la unidad:", dto.UsadaValor ?? "");
                                });
                            });
                        });

                        col.Item().Height(6);

                        col.Item().Text(t =>
                        {
                            t.Justify();
                            t.Span("Previo a la celebración del presente contrato, el Distribuidor dio a conocer al cliente el aviso de privacidad para el " +
                                    "tratamiento de sus datos personales.");
                        });

                        // ════════════════════════════════════════════════════
                        // PÁGINA 2 — Cuerpo del contrato
                        // ════════════════════════════════════════════════════
                        col.Item().PageBreak();

                        col.Item().Text(t =>
                        {
                            t.Justify();
                            t.Span("CONTRATO DE COMPRAVENTA DE VEHÍCULO NUEVO AUTOMOTOR AL CONTADO QUE CELEBRAN POR UNA PARTE ").Bold();
                            t.Span($"{dto.Denominacion ?? "GEISHA QUERÉTARO, S. DE R.L. DE C.V."}, ").Bold();
                            t.Span($"REPRESENTANDO EN ESTE ACTO POR {dto.RepresentanteLegal ?? "LUIS FRANCISCO OROZCO PEREZ"} A QUIEN EN LO SUCESIVO SE LE DENOMINARÁ 'EL VENDEDOR' Y POR LA OTRA EL CONSUMIDOR ").Bold();
                            t.Span($"{dto.NombreCliente} ").Bold();
                            t.Span("QUIEN EN LO SUCESIVO SE LE DENOMINARÁ 'EL COMPRADOR' AL TENOR DEL SIGUIENTE GLOSARIO, ASÍ COMO DE LAS DECLARACIONES Y CLÁUSULAS:").Bold();
                        });

                        col.Item().Height(8);

                        col.Item().AlignCenter().Text("GLOSARIO:").Bold().FontSize(9);
                        col.Item().Height(4);

                        col.Item().Column(c =>
                        {
                            GlosarioItem(c, "a) Consumidor:",
                                "Es la persona física o moral que adquiere en propiedad un vehículo nuevo automotor; a quien, de acuerdo a la naturaleza " +
                                "de este contrato y para los efectos correspondientes en este acto jurídico, se le denominará el comprador o cliente.");
                            GlosarioItem(c, "b) Proveedor:",
                                "Es la persona física o moral que ofrece en venta un vehículo nuevo automotor; a quien, en atención a la naturaleza jurídica " +
                                "de este contrato y para los efectos correspondientes, se le denominará el vendedor o distribuidor.");
                            GlosarioItem(c, "c) Vehículo nuevo:",
                                "El automotor de procedencia nacional o extranjera, destinado al transporte terrestre de personas y/o de bienes que el " +
                                "proveedor comercializa al consumidor por primera vez, con no más de 1,000 kilómetros recorridos.");
                        });

                        col.Item().Height(37);

                        col.Item().AlignCenter().Text("DECLARACIONES:").Bold().FontSize(9);
                        col.Item().Height(4);

                        col.Item().Text("PRIMERA. DECLARA EL VENDEDOR:").Bold();
                        col.Item().Height(3);

                        col.Item().Column(c =>
                        {
                            GlosarioItem(c, "a)",
                                $"Ser una persona moral mexicana según consta en la escritura pública número {dto.DistribuidoraEscrituraNumero}, " +
                                $"de fecha {dto.DistribuidoraEscrituraFecha}, exhibida ante la fe del {dto.DistribuidoraNotarioNombre}, " +
                                $"titular de la Notaría número {dto.DistribuidoraNotariaNumero}, del {dto.DistribuidoraNotariaEstado}, " +
                                $"e inscrita en el Registro Público del Comercio de {dto.DistribuidoraRegistroPublico} bajo el número de notaría " +
                                $"{dto.DistribuidoraRegistroNumero} de fecha {dto.DistribuidoraRegistroFecha}, y que su representante legal " +
                                $"acredita su personalidad mediante el testimonio notarial {dto.DistribuidoraTestimonio}, ante la fe de " +
                                $"{dto.DistribuidoraNotariaRLNombre} titular de la Notaria número {dto.DistribuidoraNotariaRLNumero}, " +
                                $"en el Estado de {dto.DistribuidoraNotariaRLEstado}.");
                            GlosarioItem(c, "b)",
                                $"Tener como domicilio convencional el ubicado en {dto.Domicilio}, con número telefónico {dto.Telefono}, " +
                                $"con correo electrónico {dto.CorreoDistribuidora ?? "atencionclientes@toyotaqueretaro.mx"}, " +
                                $"identificándose con la credencial para votar con fotografía número {dto.DistribuidoraIne}, " +
                                "expedida por el Instituto Nacional Electoral, misma que previo cotejo se devuelve a su propietario, " +
                                "exhibiéndose en los anexos del contrato una copia simple de la misma.");
                            GlosarioItem(c, "c)",
                                $"Que está inscrito en el Registro Federal de Contribuyentes bajo el número: {dto.Rfc} y, en su caso, contar con registro " +
                                "en el Sistema de Información Empresarial Mexicano número:");
                            GlosarioItem(c, "d)",
                                "Que cuenta con las licencias, permisos, avisos y autorizaciones previstos en la legislación nacional para ofrecer el " +
                                "servicio de venta de vehículos nuevos.");
                            GlosarioItem(c, "e)",
                                $"Que cuenta con personal capacitado, responsable y dispuesto para atender a los consumidores, en un plazo no mayor a 48 horas, " +
                                "en sus quejas, reclamaciones o comentarios referentes del bien adquirido; para lo cual se proporcionan los siguientes datos: " +
                                $"Número telefónico gratuito: {dto.Telefono}, Fax: ------- y correo electrónico: " +
                                $"{dto.CorreoDistribuidora ?? "atencionclientes@toyotaqueretaro.mx"}. " +
                                "En los siguientes horarios de atención al público: Lunes a Viernes de 9:00 a 19:00 hrs, Sábado 9:00 a 14:00 hrs.");
                            GlosarioItem(c, "f)",
                                "Que el vehículo nuevo objeto de este contrato cumple íntegramente con las disposiciones legales y Normas Oficiales " +
                                "Mexicanas vigentes en materia de seguridad y protección al medio ambiente para ser comercializado.");
                            GlosarioItem(c, "g)",
                                "Que cuenta con la infraestructura y la capacidad técnica en equipo y mano de obra para proporcionar los servicios de " +
                                "reparación, mantenimiento y garantía en los vehículos nuevos, así como en sus refacciones y accesorios.");
                        });

                        col.Item().Text("SEGUNDA: DECLARA EL COMPRADOR:").Bold();
                        col.Item().Height(3);

                        col.Item().Column(c =>
                        {
                            GlosarioItem(c, "a) En caso de Persona Moral:",
                                "Ser una persona moral mexicana según consta en la escritura pública número ___, de fecha ___, del(a) Lic., Notario " +
                                "Público número ___, en el Estado de ___, e inscrita en el Registro Público del Comercio de ___ bajo el número de " +
                                "fecha ___ de ___, y que su representante legal acredita su personalidad mediante el testimonio notarial número ___, " +
                                "exhibida ante la fé del(a) Lic., Notario Público número ___, en el Estado de ___.");
                            GlosarioItem(c, "b) En caso de Persona Física:",
                                "Llamarse como ha quedado anotado en el proemio de este contrato.");
                            GlosarioItem(c, "c)",
                                $"Que esta inscrito en el Registro Federal de Contribuyentes, bajo el número: {dto.RfcCliente}");
                            GlosarioItem(c, "d)",
                                "Que recibió del vendedor toda la información relativa al vehículo materia de este contrato, incluyendo sus especificaciones " +
                                "técnicas y rendimiento de combustible.");
                            GlosarioItem(c, "e)",
                                "Que recibió del vendedor toda la información relativa a las restricciones que pudieran aplicar, mismas que serán " +
                                "detalladas en la póliza de garantía respectiva.");
                        });

                        col.Item().Height(10);
                        col.Item().AlignCenter().Text("CLÁUSULAS:").Bold().FontSize(9);
                        col.Item().Height(6);

                        var clausulas = new[]
                        {
                    ("PRIMERA:", "En virtud de este contrato, el Distribuidor vende al Cliente (Consumidor) el vehículo cuyas características se detallan en este documento."),
                    ("SEGUNDA:", "El vehículo cuenta con el equipo opcional y accesorios adicionales solicitados y autorizados por el Cliente; mismos que se describen en la carátula del presente contrato."),
                    ("TERCERA:", "El monto total de la compraventa será cubierto íntegramente en la fecha de firma del presente contrato, incluyendo, en su caso, los equipos y accesorios adicionales solicitados y autorizados por el cliente."),
                    ("CUARTA:", "En caso de que el Cliente entregue un vehículo usado a cuenta del precio, entregará también la documentación correspondiente, sus accesorios y gastos de trámite de \"baja\", serán por cuenta del Cliente. Asimismo, el Cliente manifiesta que el vehículo está libre de gravamen y no tiene problema judicial y/o administrativo alguno, por lo que en este acto libera al Distribuidor de adeudos o conflictos que por cualquier motivo pudiera presentar dicho vehículo hasta la fecha de su entrega."),
                    ("QUINTA:", "Las partes manifiestan que el vendedor previo a la formalización del contrato de compraventa de vehículo nuevo informó al comprador sobre la garantía que ofrece a los vehículos nuevos comercializados, cuya vigencia, de acuerdo al art. 77 de la Ley Federal de Protección al Consumidor, no podrá ser inferior a 90 días naturales, y cuya cobertura y mecanismos para hacerla efectiva se especifican en el manual del usuario y póliza de garantía, así como las causas de procedencia o improcedencia de la misma."),
                    ("SEXTA:", "Las partes manifiestan que no se hará cargo alguno por servicios adicionales a los pactados en el presente instrumento, sin previo consentimiento del cliente."),
                    ("SÉPTIMA:", "El Distribuidor entrega junto con el vehículo: (i) la Carta-Factura; (ii) el Manual del Usuario; y (iii) la Póliza de Garantía en idioma español, otorgada por el fabricante y/o importador la cual contiene: a) Nombre y denominación social y domicilio del proveedor que la ofrece; b) Los datos de identificación del vehículo; c) La fecha de entrega del vehículo al consumidor; d) Vigencia, cobertura y mecanismos para hacerla efectiva; e) Los datos de los establecimientos ubicados en la República Mexicana en los que se deberá hacer efectiva la garantía y f) Los datos de la póliza para hacer efectiva la garantía, debidamente sellada y firmada."),
                    ("OCTAVA:", "Ante desperfectos en el vehículo y dentro del plazo de vigencia de la garantía, el consumidor debe acudir ante el distribuidor autorizado que comercializó el vehículo, para que el proveedor obligado a cumplir con la garantía, le informe, a través del medio que el consumidor elija (correo electrónico, teléfono, o correo certificado, etc.) en un plazo no mayor a 10 días naturales sobre la procedencia o improcedencia de la reparación de acuerdo a lo establecido en la póliza de garantía respectiva. Asimismo, en caso de proceder la reparación, el proveedor obligado asumirá la obligación de remplazar cualquier pieza o componente sin costo adicional para el consumidor; en caso de no proceder, el distribuidor hará saber por el medio elegido al consumidor la respuesta emitida por el fabricante en la que se detallarán las causas de la improcedencia."),
                };

                        col.Item().Column(c =>
                        {
                            foreach (var (num, texto) in clausulas)
                                ClausulaItem(c, num, texto);
                        });

                        string cesionSi = dto.CesionDatos == true ? "X" : " ";
                        string cesionNo = dto.CesionDatos == false ? "X" : " ";
                        string pubSi = dto.PublicidadDatos == true ? "X" : " ";
                        string pubNo = dto.PublicidadDatos == false ? "X" : " ";

                        var clausulasFinales = new[]
                        {
                    ("NOVENA:", "El tiempo que transcurra desde el momento en que el consumidor solicite la garantía hasta que le sea devuelto el vehículo reparado, no será computado dentro de la vigencia de la misma."),
                    ("DÉCIMA:", "El Distribuidor entregará al Cliente la Factura Original dentro de un plazo de 8 días contados a partir de la fecha en la que el Cliente hubiese liquidado el monto de la compraventa."),
                    ("DÉCIMA PRIMERA:", "En caso de que dentro del periodo de garantía, el Cliente acuda ante cualquier distribuidor autorizado para solicitar la reparación del vehículo conforme a la garantía otorgada por el fabricante y/o importador, y el distribuidor autorizado no cuente con las refacciones necesarias para la reparación del vehículo en un plazo máximo de 60 días naturales contados a partir de la fecha en la que el Cliente haya presentado el vehículo para su reparación, quien haya otorgado la garantía asumirá ante el Cliente los costos por el incumplimiento en los términos establecidos en la garantía, en la NOM-160-SCFI-2014, y de acuerdo con las políticas y procedimientos de garantía convenidos entre el fabricante o el importador con el Distribuidor."),
                    ("DÉCIMA SEGUNDA:", "El cliente podrá revocar su consentimiento, en un plazo de 5 días hábiles mediante aviso personal, correo electrónico, o correo certificado siempre y cuando no le haya sido entregado el vehículo materia del presente contrato."),
                    ("DÉCIMA TERCERA:", "Son causas de rescisión del presente contrato: (I) Incumplimiento de los términos del contrato, (II) Que el proveedor no esté en posibilidad de cumplir los compromisos establecidos en la garantía que otorgó, por no contar con las refacciones necesarias, en un plazo máximo de 60 días. El Cliente le notificará el incumplimiento de dicha obligación y el Distribuidor devolverá la cantidad que por cualquier concepto hubiese recibido del Cliente con motivo de esta compraventa en un plazo no mayor de 5 días hábiles a partir de la fecha en que fue notificado dicho incumplimiento. (III) Que el vehículo presente vicios ocultos derivados de la fabricación, importación o ensamble, por lo que el cliente podrá hacer valer su derecho ante la vía jurisdiccional correspondiente."),
                    ("DÉCIMA CUARTA:", "Las partes están de acuerdo en someterse a la competencia de la Procuraduría Federal del Consumidor en la vía administrativa para resolver cualquier controversia que se suscite sobre la interpretación o cumplimiento de los términos y condiciones del presente contrato y de las disposiciones de la Ley Federal de Protección al Consumidor, la Norma Oficial Mexicana NOM-160-SCFI-2014, Prácticas Comerciales-Elementos Normativos para la Comercialización de Vehículos Nuevos y cualquier otra disposición aplicable, sin perjuicio del derecho que tienen las partes de someterse a la jurisdicción de los Tribunales competentes del domicilio del Distribuidor, renunciando las partes expresamente a cualquier otra jurisdicción que pudiera corresponderles por razón de sus domicilios futuros."),
                    ("DÉCIMA QUINTA:", "Consentimiento por medios electrónicos. Las partes acuerdan que en lugar de una firma original autógrafa, este contrato, así como cualquier consentimiento u otros documentos relacionados con el mismo, podrán ser firmados por medio del uso de firmas electrónicas, digitales, numéricas, alfanuméricas, huellas de voz, biométricas o de cualquier otro tipo y que dichos medios alternativos de firma y los registros en donde sean aplicadas dichas firmas, serán consideradas para todos los efectos, incluyendo pero no limitado a la legislación civil, mercantil, protección al consumidor y a la NOM-151-SCFI-2016, con la misma fuerza y consecuencias que la firma autógrafa original física de la parte firmante."),
                    ("DÉCIMA SEXTA:", $"El consumidor SI( {cesionSi} ) NO( {cesionNo} ) acepta que el Vendedor ceda o transmita a terceros, con fines mercadotécnicos o publicitarios, la información proporcionada por él con motivo del presente contrato y SI( {pubSi} ) NO( {pubNo} ) acepta que el Distribuidor le envíe publicidad sobre bienes y servicios."),
                };

                        col.Item().Column(c =>
                        {
                            foreach (var (num, texto) in clausulasFinales)
                                ClausulaItem(c, num, texto);
                        });

                        col.Item().Height(10);

                        // ── Firma de autorización del consumidor ─────────────
                        col.Item().AlignCenter().Width(200).Column(c =>
                        {
                            byte[] firmaAuto = ObtenerBytesFirma(dto.FirmaAutorizacion);
                            if (firmaAuto != null)
                                c.Item().Height(35).AlignCenter().Image(firmaAuto).FitArea();
                            else
                                c.Item().Height(35).BorderBottom(0.5f);

                            c.Item().AlignCenter().Text("Firma de autorización del consumidor").FontSize(7);
                            c.Item().Height(3);
                            c.Item().AlignCenter().Text(dto.NombreCliente).Bold();
                        });

                        col.Item().Height(8);

                        col.Item().Text(t =>
                        {
                            t.Justify();
                            t.Span("El Cliente y el Distribuidor aceptan la realización de la presente compraventa, en los términos establecidos en este contrato, y sabedores de su alcance legal, lo firman por duplicado.");
                        });

                        col.Item().Height(8);

                        // ── Firma Vendedor | Firma Comprador ─────────────────
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("EL VENDEDOR").Bold();

                                byte[] firmaVend = ObtenerBytesFirma(dto.FirmaVendedor);
                                if (firmaVend != null)
                                    c.Item().Height(40).AlignCenter().Image(firmaVend).FitArea();
                                else
                                    c.Item().Height(40).BorderBottom(0.5f);

                                c.Item().AlignCenter().Text(dto.Denominacion ?? "GEISHA QUERÉTARO, S. DE R.L. DE C.V.").Bold().FontSize(7);
                            });

                            row.ConstantItem(20);

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("EL COMPRADOR").Bold();

                                byte[] firmaComp = ObtenerBytesFirma(dto.FirmaComprador);
                                if (firmaComp != null)
                                    c.Item().Height(40).AlignCenter().Image(firmaComp).FitArea();
                                else
                                    c.Item().Height(40).BorderBottom(0.5f);

                                c.Item().AlignCenter().Text(dto.NombreCliente).Bold().FontSize(7);
                            });
                        });

                        col.Item().Height(10);

                        col.Item().Text(
                            "(*) El presente contrato fue registrado en la Procuraduría Federal del Consumidor bajo el número 9826-2023 el día 24 de noviembre de 2023."
                        ).FontSize(7).Italic();
                    });
                });
            });

            var bytes = pdf.GeneratePdf();
            return File(bytes, "application/pdf", $"Contrato_{dto.Folio}_{dto.NombreCliente}.pdf");
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

        private static void GlosarioItem(ColumnDescriptor col, string inciso, string texto)
        {
            col.Item().Text(t =>
            {
                t.Justify();
                t.Span(inciso + " ").Bold();
                t.Span(texto);
            });
            col.Item().Height(4);
        }

        private static void ClausulaItem(ColumnDescriptor col, string numero, string texto)
        {
            col.Item().Text(t =>
            {
                t.Justify();
                t.Span(numero + " ").Bold();
                t.Span(texto);
            });
            col.Item().Height(5);
        }

        private static byte[] ObtenerBytesFirma(string base64Firma)
        {
            if (string.IsNullOrWhiteSpace(base64Firma))
                return null;

            try
            {
                // Limpiamos el prefijo (ej. data:image/png;base64,) en caso de que lo traiga el front
                var partes = base64Firma.Split(',');
                string data = partes.Length > 1 ? partes[1] : partes[0];
                return Convert.FromBase64String(data);
            }
            catch
            {
                // En caso de que el string no sea un Base64 válido
                return null;
            }
        }

    }
}