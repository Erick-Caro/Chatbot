using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace ChatbotApplication.Controllers
{
    public class EnviaController : ControllerBase
    {
            //RECIBIMOS LOS DATOS DE VALIDACION VIA GET
            [HttpGet]
            //DENTRO DE LA RUTA envia
            [Route("envia")]
            //RECIBIMOS LOS PARAMETROS QUE NOS ENVIA WHATSAPP PARA VALIDAR NUESTRA URL
            public async Task enviaAsync()
            {
                string token = "EAAXOTAid8UEBRGfywldb5lxxdhDpVFFDkKsYMgjZBDGYmbZASaqEvfCto3DI3ZAJLr0SBN7YuZCdqPvmCkzLyPhZBkiZBgOhwa8B06JbmUaOvSBk0lTK7OywRnlslqr0Huwp9rvmAvNGPL0cGGHelUKAYkQpz4Vw1zxQc2MfMiB4qtrnTBrfNnQFC3m6t9YjYeEb8fSlKJ0h7rwBW4fetauLE3PSm1NYlaeBhtefA9Y0ZBTfgfgLtMH9mlxPC6ZCZCu2erfRFBeZCQygc8f0KrNyKh";
                //Identificador de número de teléfono
                string idTelefono = "1053459377850656";
                //Nuestro telefono
                string telefono = "56961263889";
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://graph.facebook.com/v15.0/" + idTelefono + "/messages");
                request.Headers.Add("Authorization", "Bearer " + token);
                request.Content = new StringContent("{\"messaging_product\": \"whatsapp\",\"recipient_type\": \"individual\",\"to\": \"" + telefono + "\",\"type\": \"text\",\"text\": {\"body\": \"Este es un mensaje de prueba.\"}}");
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = await client.SendAsync(request);
                //response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

            
        }

    }
}
