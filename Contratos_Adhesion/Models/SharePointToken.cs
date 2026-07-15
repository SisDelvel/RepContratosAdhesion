namespace Contratos_Adhesion.Models
{
    public class SharePointToken
    {
        public string Token_type { get; set; }
        public int Expires_in { get; set; }
        public int Ext_expires_in { get; set; }
        public string Access_token { get; set; }

        public string DriveId { get; set; }
    }
}
