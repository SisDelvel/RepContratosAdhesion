namespace Contratos_Adhesion.Models
{
    public class ContratoDto
    {
        // Distribuidora (se llenan desde configuración, no desde SQL)
        public string Denominacion { get; set; }
        public string Rfc { get; set; }
        public string Domicilio { get; set; }
        public string Telefono { get; set; }
        public string HorarioAtencion { get; set; }
        public string CorreoDistribuidora { get; set; }
        public string Folio { get; set; }
        public string Fecha { get; set; }
        public string Localidad { get; set; }
        public string RepresentanteLegal { get; set; }

        // Cliente
        public string NombreCliente { get; set; }
        public string RfcCliente { get; set; }
        public string Calle { get; set; }
        public string NumExt { get; set; }
        public string NumInt { get; set; }
        public string Colonia { get; set; }
        public string CodigoPostal { get; set; }
        public string Delegacion { get; set; }
        public string? TipoZona { get; set; }
        public string Estado { get; set; }
        public string Telefonos { get; set; }
        public string Correo { get; set; }

        // Vehículo
        public string Marca { get; set; }
        public string Submarca { get; set; }
        public string Catalogo { get; set; }
        public string TipoVersion { get; set; }
        public string Color { get; set; }
        public string AnioModelo { get; set; }
        public string Niv { get; set; }
        public string Capacidad { get; set; }
        public string FechaEntrega { get; set; }
        public string LugarEntrega { get; set; }

        // Monto  ← se agregan Equipo y Condicion
        public string Condicion { get; set; }  // ← NUEVO: para lógica CONTADO/CREDITO
        public string PrecioVehiculo { get; set; }
        public string Equipo { get; set; }  // ← NUEVO: accesorios adicionales
        public string OtrosCargos { get; set; }
        public string Iva { get; set; }
        public string MontoTotal { get; set; }
        public string PagoContado { get; set; }
        public string Enganche { get; set; }

        // Agregar al ContratoDto
        public string UsadaNiv { get; set; }
        public string UsadaMarca { get; set; }
        public string UsadaSubmarca { get; set; }
        public string UsadaVersion { get; set; }
        public string UsadaColor { get; set; }
        public string UsadaAnio { get; set; }
        public string UsadaValor { get; set; }

        public bool? CesionDatos { get; set; }
        public bool? PublicidadDatos { get; set; }
        public string FirmaAutorizacion { get; set; }
        public string FirmaVendedor { get; set; }
        public string FirmaComprador { get; set; }
        public string EscrituraNumero { get; set; }       // NumeroActa
        public string EscrituraFecha { get; set; }         // FechaConstitucion
        public string NotarioNombre { get; set; }          // NomNotario
        public string NotariaNumero { get; set; }          // NumeroNotaria
        public string NotariaEstado { get; set; }          // UbicacionNotaria
        public string RegistroPublico { get; set; }        // RegPubCom
        public string RegistroNumero { get; set; }         // NumeroRPC
        public string RegistroDia { get; set; }            // DAY(FechaRPC)
        public string RegistroMes { get; set; }            // MONTH(FechaRPC)
        public string RegistroAnio { get; set; }           // YEAR(FechaRPC)
        public string TestimonioNotarial { get; set; }     // NumNotarial
        public string NotariaRLNumero { get; set; }        // NumNotariaRPC
        public string NotariaRLNombre { get; set; }        // NomNotarioRPC
        public string NotariaRLEstado { get; set; }
        public string DistribuidoraEscrituraNumero { get; set; }   // Empresa.EscrituraPublica
        public string DistribuidoraEscrituraFecha { get; set; }   // Empresa.FechaEscritura
        public string DistribuidoraNotarioNombre { get; set; }   // Empresa.NomNotario
        public string DistribuidoraNotariaNumero { get; set; }   // Empresa.NumNotaria
        public string DistribuidoraNotariaEstado { get; set; }   // Empresa.UbicacionNotaria
        public string DistribuidoraRegistroPublico { get; set; }   // Empresa.RegPubCom
        public string DistribuidoraRegistroNumero { get; set; }   // Empresa.NumeroRPC
        public string DistribuidoraRegistroFecha { get; set; }   // Empresa.FechaRPC
        public string DistribuidoraNotariaRLNombre { get; set; }   // Empresa.NomNotarioRPC
        public string DistribuidoraNotariaRLNumero { get; set; }   // Empresa.NumNotariaRPC
        public string DistribuidoraNotariaRLEstado { get; set; }   // Empresa.UbicacionNotariaRPC
        public string DistribuidoraTestimonio { get; set; }   // Empresa.NumNotarial
        public string DistribuidoraRepresentante { get; set; }   // Empresa.RepresentanteContrato
        public string DistribuidoraIne { get; set; }
        public string? SharePointUniqueId { get; set; }



    }

    public class GuardarContratoNuevoDto
    {
        public int IdVenta { get; set; }
        public string? FechaEntrega { get; set; }
        public bool? CesionDatos { get; set; }
        public bool? PublicidadDatos { get; set; }
        public string? FirmaAutorizacion { get; set; }
        public string? FirmaVendedor { get; set; }
        public string? FirmaComprador { get; set; }
        public string? CorreoDistribuidora { get; set; }
        public string? SharePointUniqueId { get; set; }

    }

    internal class CostosContratoDto
    {
        public decimal PrecioVehiculo { get; set; }
        public decimal Equipo { get; set; }
        public decimal OtrosCargos { get; set; }
        public decimal Iva { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal PagoContado { get; set; }
        public decimal Enganche { get; set; }
        public string Condicion { get; set; }
    }

    internal class UnidadUsadaDto
    {
        public string UsadaNiv { get; set; }
        public string UsadaMarca { get; set; }
        public string UsadaSubmarca { get; set; }
        public string UsadaVersion { get; set; }
        public string UsadaColor { get; set; }
        public string UsadaAnio { get; set; }
        public decimal UsadaValor { get; set; }
    }

    public class ContratoSeminuevoDto
    {
        // ── Distribuidora (dinámico desde Empresa) ───────────────
        public string? Denominacion { get; set; }
        public string? Rfc { get; set; }
        public string? Domicilio { get; set; }
        public string? Telefono { get; set; }
        public string HorarioAtencion { get; set; } = "Lunes a Viernes de 9:00 a 19:00 hrs, Sábado 9:00 a 14:00 hrs";
        public string CorreoDistribuidora { get; set; } = "atencionclientes@toyotaqueretaro.mx";
        public string? Localidad { get; set; }

        // ── Venta ────────────────────────────────────────────────
        public string? Folio { get; set; }
        public string? Fecha { get; set; }
        public string? Condicion { get; set; }
        public string? Niv { get; set; }
        public string? FechaEntrega { get; set; }

        // ── Cliente ──────────────────────────────────────────────
        public string? NombreCliente { get; set; }
        public string? RfcCliente { get; set; }
        public string? Calle { get; set; }
        public string? NumExt { get; set; }
        public string? NumInt { get; set; }
        public string? Colonia { get; set; }
        public string? CodigoPostal { get; set; }
        public string? Delegacion { get; set; }
        public string? Estado { get; set; }
        public string? Telefonos { get; set; }
        public string? Correo { get; set; }

        // ── Vehículo ─────────────────────────────────────────────
        public string? Marca { get; set; }
        public string? Submarca { get; set; }
        public string? Catalogo { get; set; }
        public string? TipoVersion { get; set; }
        public string? Color { get; set; }
        public string? AnioModelo { get; set; }
        public string? Capacidad { get; set; }
        public string? KmRecorridos { get; set; }
        public string? Placas { get; set; }
        public string? NumPropietario { get; set; }
        public string? Repuve { get; set; }
        public string? LugarEntrega { get; set; }

        // ── Monto ────────────────────────────────────────────────
        public string? PrecioVehiculo { get; set; }
        public string? Equipo { get; set; }
        public string? OtrosCargos { get; set; }
        public string? Iva { get; set; }
        public string? MontoTotal { get; set; }
        public string? PagoContado { get; set; }
        public string? Enganche { get; set; }

        // ── Unidad usada ─────────────────────────────────────────
        public string? UsadaNiv { get; set; }
        public string? UsadaMarca { get; set; }
        public string? UsadaSubmarca { get; set; }
        public string? UsadaVersion { get; set; }
        public string? UsadaColor { get; set; }
        public string? UsadaAnio { get; set; }
        public string? UsadaValor { get; set; }

        // ── Firmas ───────────────────────────────────────────────
        public string? FirmaDistribuidor { get; set; }
        public string? FirmaCliente { get; set; }

        // ── Exteriores (Cláusula 6) ──────────────────────────────
        public bool ExtLimpiaparabrisas { get; set; }
        public bool ExtLuces { get; set; }
        public bool ExtAntena { get; set; }
        public bool ExtEspejosLat { get; set; }
        public bool ExtCristales { get; set; }
        public bool ExtTapones { get; set; }
        public bool ExtMolduras { get; set; }
        public bool ExtTaponGas { get; set; }
        public bool ExtClaxon { get; set; }

        // ── Interiores (Cláusula 6) ──────────────────────────────
        public bool IntInstrumentos { get; set; }
        public bool IntCalefaccion { get; set; }
        public bool IntAire { get; set; }
        public bool IntRadio { get; set; }
        public bool IntBocinas { get; set; }
        public bool IntEncendedor { get; set; }
        public bool IntEspejoRet { get; set; }
        public bool IntCeniceros { get; set; }
        public bool IntCinturones { get; set; }
        public bool IntTapetes { get; set; }
        public bool IntManijas { get; set; }
        public bool IntEquipoAd { get; set; }
        public bool IntAccesorios { get; set; }
        public bool IntOtros { get; set; }

        // ── Aspectos mecánicos (Cláusula 6) ─────────────────────
        public bool MecLlantas { get; set; }
        public bool MecRuedas { get; set; }
        public bool MecRines { get; set; }
        public bool MecEscape { get; set; }
        public bool MecDireccion { get; set; }
        public bool MecSuspension { get; set; }
        public bool MecFrenos { get; set; }
        public bool MecParabrisas { get; set; }
        public bool MecCarroceria { get; set; }

        // ── Documentos unidad usada a cuenta (Cláusula 7) ───────
        public bool Doc7Factura { get; set; }
        public bool Doc7Tarjeta { get; set; }
        public bool Doc7DocsOficiales { get; set; }
        public bool Doc7Manual { get; set; }
        public bool Doc7Tenencias { get; set; }
        public bool Doc7Verificacion { get; set; }
        public bool Doc7Multas { get; set; }

        // ── Garantía (Cláusula 8) ────────────────────────────────
        public bool SinGarantia { get; set; }
        public bool ConGarantia { get; set; }

        // ── Documentos que entrega el distribuidor (Cláusula 10) ─
        public bool Doc10Factura { get; set; }
        public bool Doc10DocsOficiales { get; set; }
        public bool Doc10Constancia { get; set; }
        public bool Doc10Tenencias { get; set; }
        public bool Doc10Verificacion { get; set; }
        public bool Doc10Multas { get; set; }
        public bool Doc10Manual { get; set; }
        public string? SharePointUniqueId { get; set; }

    }
    public class GuardarContratoSeminuevoDto
    {
        public int IdVenta { get; set; }
        public string? FechaEntrega { get; set; }
        public string? CorreoDistribuidora { get; set; }

        // ── Exteriores (Cláusula 6) ──────────────────────────────
        public bool ExtLimpiaparabrisas { get; set; }
        public bool ExtLuces { get; set; }
        public bool ExtAntena { get; set; }
        public bool ExtEspejosLat { get; set; }
        public bool ExtCristales { get; set; }
        public bool ExtTapones { get; set; }
        public bool ExtMolduras { get; set; }
        public bool ExtTaponGas { get; set; }
        public bool ExtClaxon { get; set; }

        // ── Interiores (Cláusula 6) ──────────────────────────────
        public bool IntInstrumentos { get; set; }
        public bool IntCalefaccion { get; set; }
        public bool IntAire { get; set; }
        public bool IntRadio { get; set; }
        public bool IntBocinas { get; set; }
        public bool IntEncendedor { get; set; }
        public bool IntEspejoRet { get; set; }
        public bool IntCeniceros { get; set; }
        public bool IntCinturones { get; set; }
        public bool IntTapetes { get; set; }
        public bool IntManijas { get; set; }
        public bool IntEquipoAd { get; set; }
        public bool IntAccesorios { get; set; }
        public bool IntOtros { get; set; }

        // ── Aspectos mecánicos (Cláusula 6) ─────────────────────
        public bool MecLlantas { get; set; }
        public bool MecRuedas { get; set; }
        public bool MecRines { get; set; }
        public bool MecEscape { get; set; }
        public bool MecDireccion { get; set; }
        public bool MecSuspension { get; set; }
        public bool MecFrenos { get; set; }
        public bool MecParabrisas { get; set; }
        public bool MecCarroceria { get; set; }

        // ── Documentos unidad usada a cuenta (Cláusula 7) ───────
        public bool Doc7Factura { get; set; }
        public bool Doc7Tarjeta { get; set; }
        public bool Doc7DocsOficiales { get; set; }
        public bool Doc7Manual { get; set; }
        public bool Doc7Tenencias { get; set; }
        public bool Doc7Verificacion { get; set; }
        public bool Doc7Multas { get; set; }

        // ── Garantía (Cláusula 8) ────────────────────────────────
        public bool SinGarantia { get; set; }
        public bool ConGarantia { get; set; }

        // ── Documentos que entrega el distribuidor (Cláusula 10) ─
        public bool Doc10Factura { get; set; }
        public bool Doc10DocsOficiales { get; set; }
        public bool Doc10Constancia { get; set; }
        public bool Doc10Tenencias { get; set; }
        public bool Doc10Verificacion { get; set; }
        public bool Doc10Multas { get; set; }
        public bool Doc10Manual { get; set; }

        // ── Firmas en base64 ────────────────────────────────────
        public string? FirmaDistribuidor { get; set; }
        public string? FirmaCliente { get; set; }
        public string? SharePointUniqueId { get; set; }

    }

    public class ContratoServicioDto
    {
        // ── Venta / Orden ────────────────────────────────────────
        public string? NumOrden { get; set; }
        public string? FechaOrden { get; set; }
        public string? FechaRecepcion { get; set; }
        public string? HoraEntrega { get; set; }
        public string? FechaEntrega { get; set; }
        public decimal Importe { get; set; }
        public decimal Impuestos { get; set; }
        public decimal PrecioTotal { get; set; }
        public string? Niv { get; set; }
        public string? Comentarios { get; set; }

        // ── Cliente ──────────────────────────────────────────────
        public string? NombreCliente { get; set; }
        public string? RfcCliente { get; set; }
        public string? Correo { get; set; }
        public string? Calle { get; set; }
        public string? NumExt { get; set; }
        public string? NumInt { get; set; }
        public string? Colonia { get; set; }
        public string? CodigoPostal { get; set; }
        public string? Delegacion { get; set; }
        public string? Estado { get; set; }
        public string? Telefonos { get; set; }

        // ── Vehículo ─────────────────────────────────────────────
        public string? Marca { get; set; }
        public string? Submarca { get; set; }
        public string? TipoVersion { get; set; }
        public string? Color { get; set; }
        public string? AnioModelo { get; set; }
        public string? KmRecorridos { get; set; }
        public string? Placas { get; set; }
        public string? Capacidad { get; set; }

        // ── Distribuidora ────────────────────────────────────────
        public string? Denominacion { get; set; }
        public string? Rfc { get; set; }
        public string? Domicilio { get; set; }
        public string? Telefono { get; set; }
        public string HorarioAtencion { get; set; } = "Lunes–Viernes 08:00–19:00 hrs, Sábado 08:00–14:00 hrs";
        public string CorreoDistribuidora { get; set; } = "atencionclientes@toyotaqueretaro.mx";
        public string? Localidad { get; set; }

        // ── Asesor / Orden ───────────────────────────────────────
        public string? Asesor { get; set; }
        public string? Piramide { get; set; }
        public string? TelAsesor { get; set; }
        public string? EmailAsesor { get; set; }
        public string? PolizaNumero { get; set; }
        public bool SeEntreganPartes { get; set; }


        // ── Datos adicionales ────────────────────────────────────
        public string? Katashiki { get; set; }
        public string? FechaDofu { get; set; }
        public string? MedioContacto { get; set; }
        public string? TelContacto { get; set; }
        public string? Conductor { get; set; }

        // ── Líneas de operaciones ────────────────────────────────
        public List<LineaOperacionDto> Operaciones { get; set; } = new();

        // ── Daños guardados ──────────────────────────────────────
        public List<DanoServicioDto> Danos { get; set; } = new();

        // ── Interior izquierda ───────────────────────────────────
        public bool InvTapetes { get; set; }
        public bool InvCeniceros { get; set; }
        public bool InvBocinas { get; set; }
        public bool InvInstrumentos { get; set; }
        public bool InvEncendedores { get; set; }
        public bool InvRadio { get; set; }
        public bool InvClaxon { get; set; }
        public bool InvAC { get; set; }
        public bool InvRetrovisor { get; set; }
        public bool InvManijas { get; set; }
        public bool InvVestiduras { get; set; }
        public bool InvCinturones { get; set; }
        public bool InvManualProp { get; set; }
        public bool InvCarnetServicio { get; set; }

        // ── Interior derecha ─────────────────────────────────────
        public bool InvTarjetaCirculacion { get; set; }
        public bool InvPolizaSeguro { get; set; }
        public bool InvVerificacion { get; set; }
        public bool InvAlfombradoCaj { get; set; }
        public bool InvLlantaRefaccion { get; set; }
        public bool InvTriangulos { get; set; }
        public bool InvExtintor { get; set; }
        public bool InvCablesBateria { get; set; }
        public bool InvGato { get; set; }
        public bool InvHerramientas { get; set; }
        public bool InvBotiquin { get; set; }
        public bool InvRedProtectora { get; set; }
        public bool InvBirloSeguridad { get; set; }

        // ── Exterior ─────────────────────────────────────────────
        public bool ExtCristales { get; set; }
        public bool ExtLimpiadores { get; set; }
        public bool ExtTapones { get; set; }
        public bool ExtFarosNiebla { get; set; }
        public bool ExtAntena { get; set; }
        public bool ExtTaponGas { get; set; }
        public bool ExtMolduras { get; set; }
        public bool ExtEspejos { get; set; }
        public bool ExtFarosDelanteros { get; set; }
        public bool ExtLucesTraseras { get; set; }
        public bool ExtGolpes { get; set; }

        // ── Testigos encendidos ──────────────────────────────────
        public bool TestLlantas { get; set; }
        public bool TestCheckEngine { get; set; }
        public bool TestVscrack { get; set; }
        public bool TestPresionAceite { get; set; }
        public bool TestControlEstabilidad { get; set; }
        public bool TestBolsasAire { get; set; }
        public bool TestBateria { get; set; }
        public bool TestTemperatura { get; set; }

        // ── Nivel de gasolina ────────────────────────────────────
        public int? NivelGasolina { get; set; }

        // ── Exteriores condición 5 ───────────────────────────────
        public bool C5ExtLimpiaparabrisas { get; set; }
        public bool C5ExtLuces { get; set; }
        public bool C5ExtAntena { get; set; }
        public bool C5ExtEspejos { get; set; }
        public bool C5ExtCristales { get; set; }
        public bool C5ExtTapones { get; set; }
        public bool C5ExtMolduras { get; set; }
        public bool C5ExtTaponGas { get; set; }
        public bool C5ExtClaxon { get; set; }

        // ── Interiores condición 5 ───────────────────────────────
        public bool C5IntInstrumentos { get; set; }
        public bool C5IntCalefaccion { get; set; }
        public bool C5IntAire { get; set; }
        public bool C5IntRadio { get; set; }
        public bool C5IntBocinas { get; set; }
        public bool C5IntEncendedor { get; set; }
        public bool C5IntEspejoRet { get; set; }
        public bool C5IntCeniceros { get; set; }
        public bool C5IntCinturones { get; set; }
        public bool C5IntTapetes { get; set; }
        public bool C5IntManijas { get; set; }
        public bool C5IntEquipoAd { get; set; }
        public bool C5IntAccesorios { get; set; }
        public bool C5IntAditamentos { get; set; }
        public bool C5IntOtros { get; set; }

        // ── Garantía condición 6 ────────────────────────────────
        public bool SinGarantia { get; set; }
        public bool ConGarantia { get; set; }

        // ── Firmas en base64 ────────────────────────────────────
        public string? FirmaDistribuidor { get; set; }
        public string? FirmaCliente { get; set; }
        public string? FirmaExtra { get; set; }

        public decimal TotalRefacciones { get; set; }
        public decimal Iva { get; set; }

        // ── Partes y/o Refacciones ───────────────────────────────
        public bool PartesEntregaSi { get; set; }
        public bool PartesEntregaNo { get; set; }
        public bool PartesGarantia { get; set; }
        public bool PartesResiduos { get; set; }
        public bool ServicioDomicilioSi { get; set; }
        public bool ServicioDomicilioNo { get; set; }
        public bool PolizaSeguros { get; set; }
        public bool PolizaNoSeguros { get; set; }

        // ── ¿Desea ser contactado? ───────────────────────────────
        public bool DeseaContacto { get; set; }

        // ── Anticipo ─────────────────────────────────────────────
        public string? Anticipo { get; set; }
        public string? SharePointUniqueId { get; set; }

        // ── Propiedades calculadas ───────────────────────────────
        public string ImporteStr => Importe.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("es-MX"));
        public string ImpuestosStr => Impuestos.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("es-MX"));
        public string PrecioTotalStr => PrecioTotal.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("es-MX"));
    }
    public class LineaOperacionDto
    {
        public string? Articulo { get; set; }
        public string? Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnit { get; set; }
        public decimal Total { get; set; }

        public string CantidadStr => Cantidad.ToString("G29");
        public string PrecioUnitStr => PrecioUnit.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("es-MX"));
        public string TotalStr => Total.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("es-MX"));
    }

    public class GuardarContratoServicioDto
    {
        public int IdVenta { get; set; }

        // ── Partes y/o Refacciones ───────────────────────────────
        public bool PartesEntregaSi { get; set; }
        public bool PartesEntregaNo { get; set; }
        public bool PartesGarantia { get; set; }
        public bool PartesResiduos { get; set; }
        public bool ServicioDomicilioSi { get; set; }
        public bool ServicioDomicilioNo { get; set; }
        public bool PolizaSeguros { get; set; }
        public bool PolizaNoSeguros { get; set; }

        // ── ¿Desea ser contactado? ───────────────────────────────
        public bool DeseaContacto { get; set; }

        // ── Anticipo ─────────────────────────────────────────────
        public string? Anticipo { get; set; }

        // ── Datos Adicionales ────────────────────────────────────
        public string? Conductor { get; set; }
        public string? MedioContacto { get; set; }
        public string? TelContacto { get; set; }
        public string? FechaDofu { get; set; }
        public string? Katashiki { get; set; }

        // ── Interior col. izquierda ──────────────────────────────
        public bool InvTapetes { get; set; }
        public bool InvCeniceros { get; set; }
        public bool InvBocinas { get; set; }
        public bool InvInstrumentos { get; set; }
        public bool InvEncendedores { get; set; }
        public bool InvRadio { get; set; }
        public bool InvClaxon { get; set; }
        public bool InvAC { get; set; }
        public bool InvRetrovisor { get; set; }
        public bool InvManijas { get; set; }
        public bool InvVestiduras { get; set; }
        public bool InvCinturones { get; set; }
        public bool InvManualProp { get; set; }
        public bool InvCarnetServicio { get; set; }

        // ── Interior col. derecha ────────────────────────────────
        public bool InvTarjetaCirculacion { get; set; }
        public bool InvPolizaSeguro { get; set; }
        public bool InvVerificacion { get; set; }
        public bool InvAlfombradoCaj { get; set; }
        public bool InvLlantaRefaccion { get; set; }
        public bool InvTriangulos { get; set; }
        public bool InvExtintor { get; set; }
        public bool InvCablesBateria { get; set; }
        public bool InvGato { get; set; }
        public bool InvHerramientas { get; set; }
        public bool InvBotiquin { get; set; }
        public bool InvRedProtectora { get; set; }
        public bool InvBirloSeguridad { get; set; }

        // ── Exterior ────────────────────────────────────────────
        public bool ExtCristales { get; set; }
        public bool ExtLimpiadores { get; set; }
        public bool ExtTapones { get; set; }
        public bool ExtFarosNiebla { get; set; }
        public bool ExtAntena { get; set; }
        public bool ExtTaponGas { get; set; }
        public bool ExtMolduras { get; set; }
        public bool ExtEspejos { get; set; }
        public bool ExtFarosDelanteros { get; set; }
        public bool ExtLucesTraseras { get; set; }
        public bool ExtGolpes { get; set; }

        // ── Testigos encendidos ──────────────────────────────────
        public bool TestLlantas { get; set; }
        public bool TestCheckEngine { get; set; }
        public bool TestVscrack { get; set; }
        public bool TestPresionAceite { get; set; }
        public bool TestControlEstabilidad { get; set; }
        public bool TestBolsasAire { get; set; }
        public bool TestBateria { get; set; }
        public bool TestTemperatura { get; set; }

        // ── Nivel de gasolina (0-100) ────────────────────────────
        public int? NivelGasolina { get; set; }

        // ── Exteriores condición 5 ───────────────────────────────
        public bool C5ExtLimpiaparabrisas { get; set; }
        public bool C5ExtLuces { get; set; }
        public bool C5ExtAntena { get; set; }
        public bool C5ExtEspejos { get; set; }
        public bool C5ExtCristales { get; set; }
        public bool C5ExtTapones { get; set; }
        public bool C5ExtMolduras { get; set; }
        public bool C5ExtTaponGas { get; set; }
        public bool C5ExtClaxon { get; set; }

        // ── Interiores condición 5 ───────────────────────────────
        public bool C5IntInstrumentos { get; set; }
        public bool C5IntCalefaccion { get; set; }
        public bool C5IntAire { get; set; }
        public bool C5IntRadio { get; set; }
        public bool C5IntBocinas { get; set; }
        public bool C5IntEncendedor { get; set; }
        public bool C5IntEspejoRet { get; set; }
        public bool C5IntCeniceros { get; set; }
        public bool C5IntCinturones { get; set; }
        public bool C5IntTapetes { get; set; }
        public bool C5IntManijas { get; set; }
        public bool C5IntEquipoAd { get; set; }
        public bool C5IntAccesorios { get; set; }
        public bool C5IntAditamentos { get; set; }
        public bool C5IntOtros { get; set; }

        // ── Garantía condición 6 ────────────────────────────────
        public bool SinGarantia { get; set; }
        public bool ConGarantia { get; set; }

        // ── Firmas en base64 ────────────────────────────────────
        public string? FirmaDistribuidor { get; set; }
        public string? FirmaCliente { get; set; }
        public string? FirmaExtra { get; set; }
        public string? SharePointUniqueId { get; set; }
        // ── Daños ───────────────────────────────────────────────
        public List<DanoServicioDto> Danos { get; set; } = new();
    }

    public class DanoServicioDto
    {
        public int IdTipoDano { get; set; }  // 1=Golpe, 2=Rayón
        public decimal CoordX { get; set; }
        public decimal CoordY { get; set; }
    }
    public class CostosServicioDto
    {
        public decimal TotalRefacciones { get; set; }
        public decimal Iva { get; set; }
    }
}
