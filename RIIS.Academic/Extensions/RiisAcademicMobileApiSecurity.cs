using System.Security.Cryptography;
using System.Text;

namespace RIIS.Academic.Extensions;

internal static class RiisAcademicMobileApiSecurity
{
    public static bool IsAuthorized(
        HttpRequest request,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var requireApiKey = configuration.GetValue(
            "MobileApi:RequireApiKey",
            defaultValue: !environment.IsDevelopment());

        if (!requireApiKey)
        {
            return true;
        }

        var configuredApiKey = configuration["MobileApi:ApiKey"];
        var suppliedApiKey = request.Headers["X-RIIS-Mobile-Key"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(configuredApiKey)
            || string.IsNullOrWhiteSpace(suppliedApiKey))
        {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(configuredApiKey);
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedApiKey);

        return expectedBytes.Length == suppliedBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}
