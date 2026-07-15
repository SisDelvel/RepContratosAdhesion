using System.Text.Json;
using Contratos_Adhesion.Models;
using Microsoft.AspNetCore.Mvc;

namespace Contratos_Adhesion.Controllers
{
    public class SharepointController : Controller
    {
        private readonly IConfiguration _configuration;

        public SharepointController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GenerarToken()
        {
            using var client = new HttpClient();

            string clientId = _configuration["Sharepoint:ClientId"];
            string clientSecret = _configuration["Sharepoint:ClientSecret"];
            string tenantId = _configuration["Sharepoint:TenantId"];
            string driveId = _configuration["Sharepoint:DriveId"];

            var values = new Dictionary<string, string>
            {
                { "grant_type",    "client_credentials" },
                { "client_id",     clientId             },
                { "client_secret", clientSecret         },
                { "scope",         "https://graph.microsoft.com/.default" }
            };

            try
            {
                var response = await client.PostAsync(
                    $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
                    new FormUrlEncodedContent(values));

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, responseContent);

                var model = JsonSerializer.Deserialize<SharePointToken>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                model.DriveId = driveId;

                return Ok(model);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al generar token: {ex.Message}");
            }
        }
    }
}