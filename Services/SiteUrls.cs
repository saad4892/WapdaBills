namespace WapdaBill.Services;

public static class SiteUrls
{
    public const string DefaultPublicOrigin = "https://electricitybills.runasp.net";

    public static string PublicOrigin(IConfiguration config, HttpRequest? request = null)
    {
        var configured = config["Site:PublicOrigin"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }

        if (request is not null)
        {
            return $"{request.Scheme}://{request.Host.Value}".TrimEnd('/');
        }

        return DefaultPublicOrigin;
    }

    public static string Absolute(IConfiguration config, HttpRequest request, string? path = null)
    {
        var origin = PublicOrigin(config, request);
        var p = string.IsNullOrWhiteSpace(path) ? request.Path.Value ?? "/" : path;
        if (string.IsNullOrWhiteSpace(p))
        {
            p = "/";
        }

        if (!p.StartsWith('/'))
        {
            p = "/" + p;
        }

        // Prefer no trailing slash except for home.
        if (p.Length > 1 && p.EndsWith('/'))
        {
            p = p.TrimEnd('/');
        }

        return origin + p;
    }

    public static string DefaultOgImage(IConfiguration config, HttpRequest request) =>
        Absolute(config, request, "/img/og-default.svg");
}
