using AdaptadorGEO;
using Microsoft.Data.SqlClient;

namespace AdaptadorGEO.Samples.AdoNet;

internal static class Program
{
    public static void Main()
    {
        // A aplicação já possui a conexão concreta; a fachada resolve o provider.
        using var connection = new SqlConnection("Server=localhost;Database=Geo;Trusted_Connection=True;TrustServerCertificate=True");
        connection.Open();

        // Monta a expressão espacial e traduz para SQL nativo.
        var geo = GeoDatabase.For(connection);
        var areaFragment = geo.Translate(
            Geo.Literal(Geo.Polygon(
                Geo.Point(-23.55, -46.63),
                Geo.Point(-23.56, -46.64),
                Geo.Point(-23.57, -46.65),
                Geo.Point(-23.55, -46.63))));

        // ADO.NET executa o SQL com a geometria já traduzida para o provider ativo.
        using var command = new SqlCommand
        {
            Connection = connection,
            CommandText = $@"
INSERT INTO regions (name, area)
VALUES (@name, {areaFragment.CommandText});"
        };

        command.Parameters.AddWithValue("@name", "Região Central");

        foreach (var parameter in areaFragment.Parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
        }

        Console.WriteLine("ADO.NET sample");
        Console.WriteLine($"Provider: {geo.ProviderName}");
        Console.WriteLine("Insert command:");
        Console.WriteLine("CommandText:");
        Console.WriteLine(command.CommandText);
        Console.WriteLine("Parameters:");
        foreach (SqlParameter parameter in command.Parameters)
        {
            Console.WriteLine($"- {parameter.ParameterName} = {parameter.Value}");
        }
    }
}
