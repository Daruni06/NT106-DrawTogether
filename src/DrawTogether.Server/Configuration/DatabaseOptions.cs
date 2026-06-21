namespace DrawTogether.Server.Configuration;

public sealed class DatabaseOptions
{
    public string ConnectionString { get; init; } =
        "Server=localhost;Port=3306;Database=draw_together;User ID=root;Password=YOUR_PASSWORD;";
}
