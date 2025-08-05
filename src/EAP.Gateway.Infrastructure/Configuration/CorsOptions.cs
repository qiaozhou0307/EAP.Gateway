namespace EAP.Gateway.Infrastructure.Configuration;

public class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public string[] AllowedMethods { get; set; } = { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
    public string[] AllowedHeaders { get; set; } = { "*" };
    public bool AllowCredentials { get; set; } = true;
}
