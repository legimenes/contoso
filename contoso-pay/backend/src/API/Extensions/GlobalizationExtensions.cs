namespace API.Extensions;

internal static class GlobalizationExtensions
{
    internal static IApplicationBuilder UseGlobalization(this IApplicationBuilder app)
    {
        string[] supportedCultures = ["en-US"];
        var localizationOptions = new RequestLocalizationOptions()
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);

        app.UseRequestLocalization(localizationOptions);
        return app;
    }
}