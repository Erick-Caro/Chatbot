using ChatbotApplication.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RiveScript;
using ChatbotApplication.ApiClient;
using ChatbotApplication.Utility;

namespace ChatbotApplication.Controllers
{
    [ApiController]
    [Route("webhook")]
    public class RecibirController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private static RiveScript.RiveScript _bot;
        private readonly UsuarioApiClient _usuarioApi;

        // 🔥 Estado en memoria (simple)
        private static Dictionary<string, string> _estadoUsuario = new();

        public RecibirController(IConfiguration configuration, UsuarioApiClient usuarioApi)
        {
            _usuarioApi = usuarioApi;
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
                    return Ok();

                string mensajeRecibido = mensaje.text.body.Trim().ToLower();
                string telefonoWa = mensaje.from ?? "";

                string respuesta = "";

                // =========================
                // 🔴 SALIR GLOBAL
                // =========================
                if (mensajeRecibido == "0" || mensajeRecibido == "salir")
                {
                    _estadoUsuario.Remove(telefonoWa);

                    await EnviaAsync(telefonoWa,
                        "👋 *Gracias por contactarnos*\n\nQue tengas un excelente día.");
                    return Ok();
                }

                // =========================
                // 🔍 DETECTAR RUT
                // =========================
                var rutExtraido = RutChileno.ExtraerRut(mensajeRecibido);

                bool pareceRut = System.Text.RegularExpressions.Regex.IsMatch(
                    mensajeRecibido,
                    @"^\d{1,2}\.?\d{3}\.?\d{3}-?[\dkK]$"
                );

                if (rutExtraido != null && pareceRut)
                {
                    var (rut, dv) = rutExtraido.Value;

                    if (RutChileno.ValidarRut(rut, dv))
                    {
                        await EnviaAsync(telefonoWa,
                            "⏳ *Un momento...*\nEstamos consultando tu información.");

                        var usuario = await _usuarioApi.ObtenerUsuario(rut, dv);

                        if (usuario != null)
                        {
                            _estadoUsuario[telefonoWa] = "menu";

                            respuesta =
                                $"Hola {usuario.Nombre} 👋\n" +
                                "Bienvenido a nuestro servicio.\n\n" +
                                "📋 *MENÚ PRINCIPAL*\n" +
                                "──────────────────\n" +
                                "1️⃣ Ventas\n" +
                                "2️⃣ Soporte\n\n" +
                                "0️⃣ Salir\n\n" +
                                "👉 Responde con el número de opción";
                        }
                        else
                        {
                            respuesta = "❌ No encontré información para ese RUT.";
                        }
                    }
                    else
                    {
                        respuesta = "❌ El RUT ingresado no es válido.";
                    }
                }
                else
                {
                    // =========================
                    // 🧠 FLUJO DE MENÚ
                    // =========================
                    string estado = _estadoUsuario.ContainsKey(telefonoWa)
                        ? _estadoUsuario[telefonoWa]
                        : "inicio";

                    switch (estado)
                    {
                        case "inicio":
                            respuesta = "👋 Hola, por favor ingresa tu RUT para comenzar.";
                            break;

                        case "menu":
                            if (mensajeRecibido == "1")
                            {
                                _estadoUsuario[telefonoWa] = "ventas";
                                respuesta =
                                    "🛒 *VENTAS*\n" +
                                    "──────────────────\n" +
                                    "1️⃣ Productos\n" +
                                    "2️⃣ Precios\n\n" +
                                    "9️⃣ Volver\n" +
                                    "0️⃣ Salir\n\n" +
                                    "👉 Elige una opción";
                            }
                            else if (mensajeRecibido == "2")
                            {
                                _estadoUsuario[telefonoWa] = "soporte";
                                respuesta =
                                    "🛠 *SOPORTE*\n" +
                                    "──────────────────\n" +
                                    "1️⃣ Incidencias\n" +
                                    "2️⃣ Contacto\n\n" +
                                    "9️⃣ Volver\n" +
                                    "0️⃣ Salir\n\n" +
                                    "👉 Elige una opción";
                            }
                            else
                            {
                                respuesta =
                                    "❌ Opción no válida.\n\n" +
                                    "1️⃣ Ventas\n2️⃣ Soporte\n0️⃣ Salir";
                            }
                            break;

                        case "ventas":
                            if (mensajeRecibido == "1")
                            {
                                _estadoUsuario[telefonoWa] = "productos";
                                respuesta =
                                    "📦 *PRODUCTOS*\n" +
                                    "──────────────────\n" +
                                    "1️⃣ Ver catálogo\n" +
                                    "2️⃣ Consultar stock\n\n" +
                                    "9️⃣ Volver\n" +
                                    "0️⃣ Salir";
                            }
                            else if (mensajeRecibido == "2")
                            {
                                _estadoUsuario[telefonoWa] = "precios";
                                respuesta =
                                    "💰 *PRECIOS*\n" +
                                    "──────────────────\n" +
                                    "1️⃣ Lista de precios\n" +
                                    "2️⃣ Solicitar cotización\n\n" +
                                    "9️⃣ Volver\n" +
                                    "0️⃣ Salir";
                            }
                            else if (mensajeRecibido == "9")
                            {
                                _estadoUsuario[telefonoWa] = "menu";
                                respuesta =
                                    "📋 *MENÚ PRINCIPAL*\n" +
                                    "──────────────────\n" +
                                    "1️⃣ Ventas\n" +
                                    "2️⃣ Soporte\n\n" +
                                    "0️⃣ Salir";
                            }
                            else
                            {
                                respuesta = "❌ Opción no válida.";
                            }
                            break;

                        case "productos":
                            if (mensajeRecibido == "1")
                                respuesta = "📦 Puedes revisar el catálogo en nuestro sitio web.";
                            else if (mensajeRecibido == "2")
                                respuesta = "🔍 Indica el código del producto para consultar stock.";
                            else if (mensajeRecibido == "9")
                            {
                                _estadoUsuario[telefonoWa] = "ventas";
                                respuesta =
                                    "🛒 *VENTAS*\n" +
                                    "1️⃣ Productos\n2️⃣ Precios\n9️⃣ Volver\n0️⃣ Salir";
                            }
                            else
                                respuesta = "❌ Opción no válida.";
                            break;

                        case "precios":
                            if (mensajeRecibido == "1")
                                respuesta = "💰 La lista de precios está disponible por canal oficial.";
                            else if (mensajeRecibido == "2")
                                respuesta = "📝 Envíanos tus datos para generar una cotización.";
                            else if (mensajeRecibido == "9")
                            {
                                _estadoUsuario[telefonoWa] = "ventas";
                                respuesta =
                                    "🛒 *VENTAS*\n" +
                                    "1️⃣ Productos\n2️⃣ Precios\n9️⃣ Volver\n0️⃣ Salir";
                            }
                            else
                                respuesta = "❌ Opción no válida.";
                            break;

                        case "soporte":
                            if (mensajeRecibido == "1")
                            {
                                _estadoUsuario[telefonoWa] = "incidencias";
                                respuesta =
                                    "⚠️ *INCIDENCIAS*\n" +
                                    "──────────────────\n" +
                                    "1️⃣ Reportar problema\n" +
                                    "2️⃣ Estado de ticket\n\n" +
                                    "9️⃣ Volver\n" +
                                    "0️⃣ Salir";
                            }
                            else if (mensajeRecibido == "2")
                            {
                                _estadoUsuario[telefonoWa] = "contacto";
                                respuesta =
                                    "📞 *CONTACTO*\n" +
                                    "──────────────────\n" +
                                    "1️⃣ Horario de atención\n" +
                                    "2️⃣ Hablar con ejecutivo\n\n" +
                                    "9️⃣ Volver\n" +
                                    "0️⃣ Salir";
                            }
                            else if (mensajeRecibido == "9")
                            {
                                _estadoUsuario[telefonoWa] = "menu";
                                respuesta =
                                    "📋 *MENÚ PRINCIPAL*\n" +
                                    "1️⃣ Ventas\n2️⃣ Soporte\n0️⃣ Salir";
                            }
                            else
                                respuesta = "❌ Opción no válida.";
                            break;

                        case "incidencias":
                            if (mensajeRecibido == "1")
                                respuesta = "📝 Describe el problema que estás presentando.";
                            else if (mensajeRecibido == "2")
                                respuesta = "📌 Indica el número de tu ticket.";
                            else if (mensajeRecibido == "9")
                            {
                                _estadoUsuario[telefonoWa] = "soporte";
                                respuesta =
                                    "🛠 *SOPORTE*\n1️⃣ Incidencias\n2️⃣ Contacto\n9️⃣ Volver\n0️⃣ Salir";
                            }
                            else
                                respuesta = "❌ Opción no válida.";
                            break;

                        case "contacto":
                            if (mensajeRecibido == "1")
                                respuesta = "🕒 Atención de lunes a viernes de 09:00 a 18:00.";
                            else if (mensajeRecibido == "2")
                                respuesta = "👤 Un ejecutivo te contactará a la brevedad.";
                            else if (mensajeRecibido == "9")
                            {
                                _estadoUsuario[telefonoWa] = "soporte";
                                respuesta =
                                    "🛠 *SOPORTE*\n1️⃣ Incidencias\n2️⃣ Contacto\n9️⃣ Volver\n0️⃣ Salir";
                            }
                            else
                                respuesta = "❌ Opción no válida.";
                            break;

                        default:
                            respuesta = "👉 Escribe *menu* para comenzar.";
                            break;
                    }
                }

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