using AdaptadorGEO;
using AdaptadorGEO.Integration;
using AdaptadorGEO.Integration.Dapper;
using Microsoft.Data.SqlClient;

namespace AdaptadorGEO.Samples.Dapper;

internal static class Program
{
    public static void Main()
    {
        // A aplicação já tem a conexão e pede para a integração resolver a fachada.
        using var connection = new SqlConnection("Server=localhost;Database=Geo;Trusted_Connection=True;TrustServerCertificate=True");

        // A fachada descobre o provider e monta o SQL espacial.
        var geo = connection.AsGeoDatabase();
        var fragment = geo.Translate(
            Geo.Column("area").Intersects(Geo.Point(-23.55052, -46.63331)));

        // O fragmento é consumido pelo Dapper sem duplicar a montagem dos parâmetros.
        Console.WriteLine("Dapper sample");
        Console.WriteLine($"Provider: {geo.ProviderName}");
        Console.WriteLine("CommandText:");
        Console.WriteLine(fragment.CommandText);
        Console.WriteLine("Parameters:");
        foreach (var parameter in fragment.Parameters)
        {
            Console.WriteLine($"- {parameter.Name} = {parameter.Value}");
        }
        Console.WriteLine("Dapper parameter names:");
        foreach (var name in fragment.ToDynamicParameters().ParameterNames)
        {
            Console.WriteLine($"- {name}");
        }
    }
}
