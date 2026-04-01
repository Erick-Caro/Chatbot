using System.Text.RegularExpressions;

namespace ChatbotApplication.Utility
{
    public class RutChileno
    {
        public static (int rut, string dv)? ExtraerRut(string texto)
        {
            var match = Regex.Match(texto, @"(\d{7,8})-?([\dkK])");

            if (!match.Success)
                return null;

            int rut = int.Parse(match.Groups[1].Value);
            string dv = match.Groups[2].Value.ToUpper();

            return (rut, dv);
        }

        public static bool ValidarRut(int rut, string dv)
        {
            int suma = 0;
            int multiplicador = 2;

            while (rut > 0)
            {
                int digito = rut % 10;
                suma += digito * multiplicador;

                rut /= 10;
                multiplicador++;

                if (multiplicador > 7)
                    multiplicador = 2;
            }

            int resto = 11 - (suma % 11);

            string dvEsperado;

            if (resto == 11)
                dvEsperado = "0";
            else if (resto == 10)
                dvEsperado = "K";
            else
                dvEsperado = resto.ToString();

            return dvEsperado.Equals(dv.ToUpper(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
