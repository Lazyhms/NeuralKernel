using OllamaSharp.Models;

#pragma warning disable IDE0130 // �����ռ����ļ��нṹ��ƥ��
namespace Microsoft.SemanticKernel;
#pragma warning restore IDE0130 // �����ռ����ļ��нṹ��ƥ��

public static class PromptExecutionSettingsExtensions
{
    public static PromptExecutionSettings AddOllamaOption(this PromptExecutionSettings settings, OllamaOption option, object value)
    {
        settings.ExtensionData ??= new Dictionary<string, object>();

        settings.ExtensionData[option.Name] = value;

        return settings;
    }
}
