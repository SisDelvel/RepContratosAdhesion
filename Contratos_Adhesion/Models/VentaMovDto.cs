namespace Contratos_Adhesion.Models
{
    public class VentaMovDto
    {
        public string Mov { get; set; }
        public string MovId { get; set; }
    }

    public class GuardarDocumentoOperDto
    {
        public int IdVenta { get; set; }
        public string Archivo { get; set; }
        public string? Url { get; set; }
        public string IdSharePoint { get; set; }
        public string MimeType { get; set; } = "pdf";
        public int Tamano { get; set; }
    }
}