using ChatbotApplication.Models;

namespace ChatbotApplication.ApiClient
{
    public class UsuarioApiClient
    {
        private readonly HttpClient _httpClient;

        public UsuarioApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Usuario?> ObtenerUsuario(int rut, string dv)
        {
            var response = await _httpClient.GetAsync($"api/Usuario?p_rut={rut}&p_dv={dv}");

            if (!response.IsSuccessStatusCode)
                return null;
            
            return await response.Content.ReadFromJsonAsync<Usuario>();
        }
    }
}
