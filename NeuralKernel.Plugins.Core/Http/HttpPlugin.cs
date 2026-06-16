using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Net;

namespace NeuralKernel.Plugins.Core.Http;

/// <summary>
/// �ṩHTTP�����ܵĲ����֧��GET��POST��PUT��DELETE�ȳ���HTTP����������ͨ�� AllowedDomains �����������������Ŀ����������ǿ��ȫ�ԡ�
/// </summary>
[KernelPlugin]
[Description("�ṩHTTP�����ܵĲ����֧��GET��POST��PUT��DELETE�ȳ���HTTP������")]
public sealed class HttpPlugin(HttpClient client)
{
    private HashSet<string>? _allowedDomains = [];

    /// <summary>
    /// �������������������
    /// </summary>
    public IEnumerable<string>? AllowedDomains
    {
        get => this._allowedDomains;
        set => this._allowedDomains = value is null ? null : new HashSet<string>(value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// ����GET����
    /// </summary>
    [KernelFunction, Description("��ָ��URI����GET����")]
    public Task<string> GetAsync(
        [Description("�����URI��ַ")] string uri,
        CancellationToken cancellationToken = default)
        => this.SendRequestAsync(uri, HttpMethod.Get, requestContent: null, cancellationToken);

    /// <summary>
    /// ����POST����
    /// </summary>
    [KernelFunction, Description("��ָ��URI����POST����")]
    public Task<string> PostAsync(
        [Description("�����URI��ַ")] string uri,
        [Description("����������")] string body,
        CancellationToken cancellationToken = default) =>
        this.SendRequestAsync(uri, HttpMethod.Post, new StringContent(body), cancellationToken);

    /// <summary>
    /// ����PUT����
    /// </summary>
    [KernelFunction, Description("��ָ��URI����PUT����")]
    public Task<string> PutAsync(
        [Description("�����URI��ַ")] string uri,
        [Description("����������")] string body,
        CancellationToken cancellationToken = default)
        => this.SendRequestAsync(uri, HttpMethod.Put, new StringContent(body), cancellationToken);

    /// <summary>
    /// ����DELETE����
    /// </summary>
    [KernelFunction, Description("��ָ��URI����DELETE����")]
    public Task<string> DeleteAsync(
        [Description("�����URI��ַ")] string uri,
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