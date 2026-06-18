using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Net;

namespace NeuralKernel.Plugins.Core.Http;

/// <summary>
/// 提供HTTP请求功能的插件，支持GET、POST、PUT、DELETE等多种HTTP方法。可通过AllowedDomains属性限制允许访问的目标地址，确保安全性。
/// </summary>
[Description("提供HTTP请求功能的插件，支持GET、POST、PUT、DELETE等多种HTTP方法")]
public sealed class HttpPlugin(HttpClient client)
{
    private HashSet<string>? _allowedDomains = [];

    /// <summary>
    /// 允许访问的域名列表
    /// </summary>
    public IEnumerable<string>? AllowedDomains
    {
        get => this._allowedDomains;
        set => this._allowedDomains = value is null ? null : new HashSet<string>(value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 发送GET请求
    /// </summary>
    [KernelFunction, Description("向指定URI发送GET请求")]
    public Task<string> GetAsync(
        [Description("目标URI地址")] string uri,
        CancellationToken cancellationToken = default)
        => this.SendRequestAsync(uri, HttpMethod.Get, requestContent: null, cancellationToken);

    /// <summary>
    /// 发送POST请求
    /// </summary>
    [KernelFunction, Description("向指定URI发送POST请求")]
    public Task<string> PostAsync(
        [Description("目标URI地址")] string uri,
        [Description("请求体内容")] string body,
        CancellationToken cancellationToken = default) =>
        this.SendRequestAsync(uri, HttpMethod.Post, new StringContent(body), cancellationToken);

    /// <summary>
    /// 发送PUT请求
    /// </summary>
    [KernelFunction, Description("向指定URI发送PUT请求")]
    public Task<string> PutAsync(
        [Description("目标URI地址")] string uri,
        [Description("请求体内容")] string body,
        CancellationToken cancellationToken = default)
        => this.SendRequestAsync(uri, HttpMethod.Put, new StringContent(body), cancellationToken);

    /// <summary>
    /// 发送DELETE请求
    /// </summary>
    [KernelFunction, Description("向指定URI发送DELETE请求")]
    public Task<string> DeleteAsync(
        [Description("目标URI地址")] string uri,
        CancellationToken cancellationToken = default)
        => this.SendRequestAsync(uri, HttpMethod.Delete, requestContent: null, cancellationToken);

    private bool IsUriAllowed(Uri uri)
    {
        return this._allowedDomains is not null
            && this._allowedDomains.Count > 0
            && this._allowedDomains.Contains(uri.Host);
    }

    private async Task<string> SendRequestAsync(string uriStr, HttpMethod method, HttpContent? requestContent, CancellationToken cancellationToken)
    {
        var uri = new Uri(uriStr);
        if (!this.IsUriAllowed(uri))
        {
            throw new InvalidOperationException("Sending requests to the provided location is not allowed.");
        }

        using var request = new HttpRequestMessage(method, uri) { Content = requestContent };

        try
        {
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
            return await response!.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException e)
        {
            throw new HttpOperationException(HttpStatusCode.BadRequest, null, e.Message, e);
        }
    }
}