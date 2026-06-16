using System.Text;
using System.Text.Json;

#pragma warning disable IDE0130 // �����ռ����ļ��нṹ��ƥ��
namespace System.IO;
#pragma warning restore IDE0130 // �����ռ����ļ��нṹ��ƥ��

public static class StreamExtensions
{
    public static async Task WriteEventAsync(this Stream stream, string text, Encoding? encoding = null)
    {
        await stream.WriteAsync((encoding ?? Encoding.UTF8).GetBytes($"event:{text}\n\n"));
        await stream.FlushAsync();
    }

    public static async Task WriteDataAsync(this Stream stream, string? text, Encoding? encoding = null)
    {
        await stream.WriteAsync((encoding ?? Encoding.UTF8).GetBytes($"data:{text}\n\n"));
        await stream.FlushAsync();
    }

    public static async Task WriteJsonDataAsync<T>(this Stream stream, T value, Encoding? encoding = null, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        await stream.WriteAsync((encoding ?? Encoding.UTF8).GetBytes($"data:{JsonSerializer.Serialize(value, jsonSerializerOptions)}\n\n"));
        await stream.FlushAsync();
    }

    public static async Task WriteJsonDataAsync(this Stream stream, object value, Encoding? encoding = null, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        await stream.WriteAsync((encoding ?? Encoding.UTF8).GetBytes($"data:{JsonSerializer.Serialize(value, jsonSerializerOptions)}\n\n"));
        await stream.FlushAsync();
    }

    public static async Task WriteNewLineAsync(this Stream stream, Encoding? encoding = null)
    {
        await stream.WriteAsync((encoding ?? Encoding.UTF8).GetBytes("data:\n\n"));
        await stream.FlushAsync();
    }

    public static async Task WriteDoneAsync(this Stream stream, Encoding? encoding = null)
    {
        await stream.WriteAsync((encoding ?? Encoding.UTF8).GetBytes("data:[DONE]\n\n"));
        await stream.FlushAsync();
    }
}
