using Contratos_Adhesion.Services;
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    private readonly IRepositorioContratoNuevos _repositorio;

    public HomeController(IRepositorioContratoNuevos repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<IActionResult> Index(string? id, int? negocio)
    {
        if (string.IsNullOrWhiteSpace(id) || negocio == null)
            return View();
        HttpContext.Session.SetInt32("Negocio", negocio.Value);
        var mov = await _repositorio.ObtenerTipoContratoAsync(id, negocio.Value);
        var (url, titulo) = mov?.Trim() switch
        {
            "Fact Sist Financ" or "Fact Autofinan" or "Fact Autos Nuevos" or "Factura Nvo Flotilla"
                => ($"/ContratoNuevos?ventaId={id}", "Contrato Vehículo Nuevo"),
            "Factura Seminuevos" => ($"/ContratoSeminuevos?ventaId={id}", "Contrato Vehículo Seminuevo"),
            "Servicio" => ($"/ContratoServicio?idServicio={id}", "Orden de Servicio"),
            _ => ((string?)null, (string?)null)
        };
        if (url == null) return View();
        ViewBag.ContratoUrl = url;
        ViewBag.ContratoTitulo = titulo;
        return View();
    }
}