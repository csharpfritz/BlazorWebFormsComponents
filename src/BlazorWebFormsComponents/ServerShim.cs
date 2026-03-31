using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace BlazorWebFormsComponents;

/// <summary>
/// Compatibility shim for Web Forms <c>Server</c> (HttpServerUtility).
/// Provides <c>Server.MapPath()</c>, <c>Server.HtmlEncode()</c>,
/// <c>Server.HtmlDecode()</c>, <c>Server.UrlEncode()</c>, and
/// <c>Server.UrlDecode()</c>.
/// </summary>
public class ServerShim
{
    private readonly IWebHostEnvironment _env;

    public ServerShim(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Maps a virtual path to a physical path on the server.
    /// <c>~/</c> prefix maps to WebRootPath (wwwroot).
    /// Other paths map relative to ContentRootPath.
    /// </summary>
    public string MapPath(string virtualPath)
    {
        if (string.IsNullOrEmpty(virtualPath))
            return _env.ContentRootPath;

        if (virtualPath.StartsWith("~/", StringComparison.Ordinal))
            return Path.Combine(_env.WebRootPath ?? _env.ContentRootPath,
                virtualPath[2..].Replace('/', Path.DirectorySeparatorChar));

        return Path.Combine(_env.ContentRootPath,
            virtualPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
    }

    /// <summary>HTML-encodes a string.</summary>
    public string HtmlEncode(string text) => System.Net.WebUtility.HtmlEncode(text);

    /// <summary>HTML-decodes a string.</summary>
    public string HtmlDecode(string text) => System.Net.WebUtility.HtmlDecode(text);

    /// <summary>URL-encodes a string.</summary>
    public string UrlEncode(string text) => System.Net.WebUtility.UrlEncode(text);

    /// <summary>URL-decodes a string.</summary>
    public string UrlDecode(string text) => System.Net.WebUtility.UrlDecode(text);
}
