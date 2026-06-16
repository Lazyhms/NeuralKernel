using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace NeuralKernel.Plugins.Core.Time;

/// <summary>
/// ʱ�����ṩһ�����ڻ�ȡ��ǰʱ������ڵĺ�����
/// </summary>
[KernelPlugin]
[Description("ʱ�����ṩһ�����ڻ�ȡ��ǰʱ������ڵĺ�����")]
public sealed class TimePlugin
{
    /// <summary>
    /// ��ȡ��ǰ����
    /// </summary>
    /// <example>
    /// {{time.date}} => 2031��1��12�գ�������
    /// </example>
    /// <returns> ��ǰ���� </returns>
    [KernelFunction, Description("��ȡ��ǰ����")]
    public static string Date(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("D", formatProvider);

    /// <summary>
    /// ��ȡ��ǰ����
    /// </summary>
    /// <example>
    /// {{time.today}} => 2031��1��12�գ�������
    /// </example>
    /// <returns> ��ǰ���� </returns>
    [KernelFunction, Description("��ȡ��ǰ����")]
    public static string Today(IFormatProvider? formatProvider = null) => Date(formatProvider);

    /// <summary>
    /// ��ȡ����ʱ���ĵ�ǰ���ں�ʱ��
    /// </summary>
    /// <example>
    /// {{time.now}} => 2025��1��12�գ������� ����9:15
    /// </example>
    /// <returns> ����ʱ���ĵ�ǰ���ں�ʱ�� </returns>
    [KernelFunction, Description("��ȡ����ʱ���ĵ�ǰ���ں�ʱ��")]
    public static string Now(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("f", formatProvider);

    /// <summary>
    /// ��ȡ��ǰUTC���ں�ʱ��
    /// </summary>
    /// <example>
    /// {{time.utcNow}} => 2025��1��13�գ������� ����5:15
    /// </example>
    /// <returns> ��ǰUTC���ں�ʱ�� </returns>
    [KernelFunction, Description("��ȡ��ǰUTC���ں�ʱ��")]
    public static string UtcNow(IFormatProvider? formatProvider = null) => DateTimeOffset.UtcNow.ToString("f", formatProvider);

    /// <summary>
    /// ��ȡ��ǰʱ��
    /// </summary>
    /// <example>
    /// {{time.time}} => ����09:15:07
    /// </example>
    /// <returns> ��ǰʱ�� </returns>
    [KernelFunction, Description("��ȡ��ǰʱ��")]
    public static string Time(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("hh:mm:ss tt", formatProvider);

    /// <summary>
    /// ��ȡ��ǰ���
    /// </summary>
    /// <example>
    /// {{time.year}} => 2025
    /// </example>
    /// <returns> ��ǰ��� </returns>
    [KernelFunction, Description("��ȡ��ǰ���")]
    public static string Year(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("yyyy", formatProvider);

    /// <summary>
    /// ��ȡ��ǰ�·�����
    /// </summary>
    /// <example>
    /// {time.month}} => һ��
    /// </example>
    /// <returns> ��ǰ�·����� </returns>
    [KernelFunction, Description("��ȡ��ǰ�·�����")]
    public static string Month(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("MMMM", formatProvider);

    /// <summary>
    /// ��ȡ��ǰ�·�����
    /// </summary>
    /// <example>
    /// {{time.monthNumber}} => 01
    /// </example>
    /// <returns> ��ǰ�·����� </returns>
    [KernelFunction, Description("��ȡ��ǰ�·�����")]
    public static string MonthNumber(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("MM", formatProvider);

    /// <summary>
    /// ��ȡ���µĵڼ���
    /// </summary>
    /// <example>
    /// {{time.day}} => 12
    /// </example>
    /// <returns> ���µĵڼ��� </returns>
    [KernelFunction, Description("��ȡ���µĵڼ���")]
    public static string Day(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("dd", formatProvider);

    /// <summary>
    /// ��ȡָ������֮ǰ������
    /// </summary>
    /// <returns> �ӽ�����ǰ����ָ������������� </returns>
    [KernelFunction]
    [Description("��ȡ����ڽ���ƫ��ָ������������")]
    public static string DaysAgo([Description("�ӽ��쿪ʼƫ�Ƶ�����")] double input, IFormatProvider? formatProvider = null)
        => DateTimeOffset.Now.AddDays(-input).ToString("D", formatProvider);

    /// <summary>
    /// ��ȡ��ǰ�����ڼ�
    /// </summary>
    /// <example>
    /// {{time.dayOfWeek}} => ������
    /// </example>
    /// <returns> ��ǰ�����ڼ� </returns>
    [KernelFunction, Description("��ȡ��ǰ�����ڼ�")]
    public static string DayOfWeek(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("dddd", formatProvider);

    /// <summary>
    /// ��ȡ��ǰʱ��Сʱ��12Сʱ�ƣ�
    /// </summary>
    /// <example>
    /// {{time.hour}} => ����9��
    /// </example>
    /// <returns> ��ǰʱ��Сʱ </returns>
    [KernelFunction, Description("��ȡ��ǰʱ��Сʱ��12Сʱ�ƣ�")]
    public static string Hour(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("h tt", formatProvider);

    /// <summary>
    /// ��ȡ��ǰ24Сʱ��Сʱ��
    /// </summary>
    /// <example>
    /// {{time.hourNumber}} => 21
    /// </example>
    /// <returns> ��ǰ24Сʱ��Сʱ�� </returns>
    [KernelFunction, Description("��ȡ��ǰ24Сʱ��Сʱ��")]
    public static string HourNumber(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("HH", formatProvider);

    /// <summary>
    /// ��ȡ��һ��ƥ��ָ���������Ƶ�����
    /// </summary>
    /// <example>
    /// {{time.lastMatchingDay $dayName}} => 2023��5��7�գ�������
    /// </example>
    /// <returns> ��һ��ƥ����������Ƶ����� </returns>
    /// <exception cref="ArgumentOutOfRangeException">�������Ʋ�����Ч�����ڼ�</exception>
    [KernelFunction]
    [Description("��ȡ��һ��ƥ��Ӣ���������Ƶ����ڡ�ʾ�������ܶ��Ǽ��� -> dateMatchingLastDayName 'Tuesday' => 2023��5��16�գ����ڶ�")]
    public static string DateMatchingLastDayName(
        [Description("Ҫƥ�����������")] DayOfWeek input,
        IFormatProvider? formatProvider = null)
    {
        DateTimeOffset dateTime = DateTimeOffset.Now;

        for (int i = 1; i <= 7; ++i)
        {
            dateTime = dateTime.AddDays(-1);
            if (dateTime.DayOfWeek == input)
            {
                break;
            }
        }

        return dateTime.ToString("D", formatProvider);
    }

    /// <summary>
    /// ��ȡ��ǰСʱ�ķ�����
    /// </summary>
    /// <example>
    /// {{time.minute}} => 15
    /// </example>
    /// <returns> ��ǰСʱ�ķ����� </returns>
    [KernelFunction, Description("��ȡ��ǰСʱ�ķ�����")]
    public static string Minute(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("mm", formatProvider);

    /// <summary>
    /// ��ȡ��ǰ���ӵ�����
    /// </summary>
    /// <example>
    /// {{time.second}} => 7
    /// </example>
    /// <returns> ��ǰ���ӵ����� </returns>
    [KernelFunction, Description("��ȡ��ǰ���ӵ�����")]
    public static string Second(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("ss", formatProvider);

    /// <summary>
    /// ��ȡ����ʱ�������UTC��ƫ����
    /// </summary>
    /// <example>
    /// {{time.timeZoneOffset}} => +08:00
    /// </example>
    /// <returns> ����ʱ�������UTC��ƫ���� </returns>
    [KernelFunction, Description("��ȡ����ʱ�������UTC��ƫ����")]
    public static string TimeZoneOffset(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("%K", formatProvider);

    /// <summary>
    /// ��ȡ����ʱ������
    /// </summary>
    /// <example>
    /// {{time.timeZoneName}} => �й���׼ʱ��
    /// </example>
    /// <remark>
    /// ע�⣺����"��ǰ"ʱ����������ݱ仯������Ӷ���ʱ��Ϊ����ʱ
    /// </remark>
    /// <returns> ����ʱ������ </returns>
    [KernelFunction, Description("��ȡ����ʱ������")]
    public static string TimeZoneName() => TimeZoneInfo.Local.DisplayName;
}