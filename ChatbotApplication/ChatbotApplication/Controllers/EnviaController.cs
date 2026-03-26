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
                string token = "EAALosUMDcQMBRFF0VvTfzs1JfBDeyAT5p1yLtahGX3pKSsMZCwWQaVSiP25ZA52ZAjZAfnRZAmEck4xfuuxEST1YOiXksQDQg1dr6dK891M1kC8iIzBZCwRlu4xctK8ZCbXhZBCwq4EfzTB1WCg0eKcpgq8Y6gcM2zqI1KEuj6gFUhigLAYLQ0avtuZBp2W9luQwDugxMjDhuBZAFlCMa79HpzrcHKCUZBLRr3VJyxlEENV8dOMTZAR9QZBktZALGriaFWE34CdBxMFWYXbcbuTGEdclbRAwZDZD";
                //Identificador de número de teléfono
                string idTelefono = "965351430004754";
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
