using AdaptadorGEO;
using AdaptadorGEO.Integration.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AdaptadorGEO.Samples.EFCore;

internal static class Program
{
    public static void Main()
    {
        // O DbContext representa a aplicação real; a fachada usa a conexão do provider atual.
        using var context = new GeoSampleDbContext();

        // A fachada gera o fragmento SQL no dialeto correto do banco configurado.
        var geo = context.Database.AsGeoDatabase();
        var fragment = geo.Translate(
            Geo.Column("area").Contains(
                Geo.Polygon(
                    Geo.Point(-23.55, -46.63),
                    Geo.Point(-23.56, -46.64),
                    Geo.Point(-23.57, -46.65),
                    Geo.Point(-23.55, -46.63))));

        // O EF Core executa o SQL já traduzido.
        Console.WriteLine("EF Core sample");
        Console.WriteLine($"Provider: {geo.ProviderName}");
        Console.WriteLine("CommandText:");
        Console.WriteLine(fragment.CommandText);
        Console.WriteLine("Parameters:");
        foreach (var parameter in fragment.Parameters)
        {
            Console.WriteLine($"- {parameter.Name} = {parameter.Value}");
        }
    }
}

public sealed class GeoSampleDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=localhost;Database=Geo;Trusted_Connection=True;TrustServerCertificate=True");
    }
}
