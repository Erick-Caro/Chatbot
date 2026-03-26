using ChatbotApplication.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RiveScript;

namespace ChatbotApplication.Controllers
{
    [ApiController]
    [Route("webhook")]
    public class RecibirController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private static RiveScript.RiveScript _bot;

        public RecibirController(IConfiguration configuration)
        {
            _configuration = configuration;

            if (_bot == null)
            {
                _bot = new RiveScript.RiveScript(true);
                _bot.loadFile("guia.rive");
                _bot.sortReplies();
            }
        }

        [HttpGet]
        public IActionResult Webhook(
            [FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.challenge")] string challenge,
            [FromQuery(Name = "hub.verify_token")] string verify_token)
        {
            string tokenVerificacion = _configuration["WhatsApp:VerifyToken"];

            if (!string.IsNullOrEmpty(verify_token) && verify_token == tokenVerificacion)
            {
                return Content(challenge ?? string.Empty, "text/plain");
            }

            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> Datos([FromBody] WebHookResponseModel entry)
        {
            try
            {
                var mensaje = entry?.entry?[0]?.changes?[0]?.value?.messages?[0];
                if (mensaje == null || mensaje.text == null || string.IsNullOrWhiteSpace(mensaje.text.body))
                {
                    return Ok();
                }

                string mensajeRecibido = mensaje.text.body.Trim();
                string idWa = mensaje.id ?? string.Empty;
                string telefonoWa = mensaje.from ?? string.Empty;

                string respuesta = _bot.reply(telefonoWa, mensajeRecibido.ToLowerInvariant());

                if (string.IsNullOrWhiteSpace(respuesta))
                {
                    respuesta = "No entendí tu mensaje.\nEscribe menu para volver al inicio.";
                }

                respuesta = respuesta
                    .Replace("\\n", "\n")
                    .Replace("^", "\n")
                    .Trim();

                string texto = "";
                texto += "mensaje_recibido=" + mensajeRecibido + Environment.NewLine;
                texto += "id_wa=" + idWa + Environment.NewLine;
                texto += "telefono_wa=" + telefonoWa + Environment.NewLine;
                texto += "respuesta=" + respuesta + Environment.NewLine;

                await System.IO.File.WriteAllTextAsync("texto.txt", texto);
                await EnviaAsync(telefonoWa, respuesta);

                return Ok();
            }
            catch (Exception ex)
            {
                await System.IO.File.WriteAllTextAsync("error.txt", ex.ToString());
                return Ok();
            }
        }

        private async Task EnviaAsync(string telefono, string mensaje)
        {
            string token = _configuration["WhatsApp:AccessToken"];
            string idTelefono = _configuration["WhatsApp:PhoneNumberId"];

            using HttpClient client = new HttpClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://graph.facebook.com/v15.0/{idTelefono}/messages"
            );

            request.Headers.Add("Authorization", "Bearer " + token);

            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = telefono,
                type = "text",
                text = new
                {
                    body = mensaje
                }
            };

            string json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            await System.IO.File.WriteAllTextAsync("respuesta_whatsapp.txt", responseBody);
        }
    }
}