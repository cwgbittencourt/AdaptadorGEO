using AdaptadorGEO;
using Microsoft.Data.SqlClient;

namespace AdaptadorGEO.Samples.AdoNet;

internal static class Program
{
    public static void Main()
    {
        // A aplicação já possui a conexão concreta; a fachada resolve o provider.
        using var connection = new SqlConnection("Server=localhost;Database=Geo;Trusted_Connection=True;TrustServerCertificate=True");

        // Monta a expressão espacial e traduz para SQL nativo.
        var geo = GeoDatabase.For(connection);
        var fragment = geo.Translate(
            Geo.Column("area").Within(Geo.Point(-23.55052, -46.63331)));

        // ADO.NET executa o SQL com parâmetros que vieram do fragmento.
        using var command = new SqlCommand
        {
            CommandText = fragment.CommandText
        };

        foreach (var parameter in fragment.Parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
        }

        Console.WriteLine("ADO.NET sample");
        Console.WriteLine($"Provider: {geo.ProviderName}");
        Console.WriteLine("CommandText:");
        Console.WriteLine(command.CommandText);
        Console.WriteLine("Parameters:");
        foreach (SqlParameter parameter in command.Parameters)
        {
            Console.WriteLine($"- {parameter.ParameterName} = {parameter.Value}");
        }
    }
}
