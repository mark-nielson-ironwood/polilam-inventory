using PolilamInventory.Web.Services;

namespace PolilamInventory.Web.Middleware;

public class PlannedClaimConversionMiddleware
{
    private readonly RequestDelegate _next;

    public PlannedClaimConversionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, PlannedClaimConversionService conversionService)
    {
        await conversionService.ConvertDueClaims();
        await _next(context);
    }
}
