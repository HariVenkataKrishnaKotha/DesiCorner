namespace DesiCorner.Gateway.Infrastructure;

public static class Problem
{
    public static IResult Json(int status, string title, string detail, HttpContext ctx)
        => Results.Json(new
        {
            title,
            detail,
            status,
            requestId = ctx.TraceIdentifier
        }, statusCode: status);
}