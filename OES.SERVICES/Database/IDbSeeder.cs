namespace OES.SERVICES.Database;

public interface IDbSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
