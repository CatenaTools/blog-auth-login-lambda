using System.Net.Mime;
using System.Text;

namespace ServerlessAPI.Responses;

internal static class ResultsExtensions
{
    public static IResult Html(this IResultExtensions resultExtensions, string html)
    {
        ArgumentNullException.ThrowIfNull(resultExtensions);

        return new HtmlResult(html);
    }
    
    public static IResult HtmlWithCookie(this IResultExtensions resultExtensions, string html, Dictionary<string,string> cookies)
    {
        ArgumentNullException.ThrowIfNull(resultExtensions);

        return new HtmlResultWithCookie(html, cookies);
    }

}

internal class HtmlResult(string html) : IResult
{
    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = MediaTypeNames.Text.Html;
        httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(html);
        return httpContext.Response.WriteAsync(html);
    }
}


internal class HtmlResultWithCookie(string html, Dictionary<string, string> cookies) : IResult
{
    public Task ExecuteAsync(HttpContext httpContext)
    {
        foreach (var keyValuePair in cookies)
        {
            httpContext.Response.Cookies.Append(keyValuePair.Key, keyValuePair.Value);
        }
        httpContext.Response.ContentType = MediaTypeNames.Text.Html;
        httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(html);
        return httpContext.Response.WriteAsync(html);
        
    }
}