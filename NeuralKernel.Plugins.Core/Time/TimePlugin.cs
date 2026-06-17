using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace NeuralKernel.Plugins.Core.Time;

/// <summary>
/// 时间插件提供一系列用于获取当前时间和日期的函数。
/// </summary>
[KernelPlugin]
[Description("时间插件提供一系列用于获取当前时间和日期的函数。")]
public sealed class TimePlugin
{
    /// <summary>
    /// 获取当前日期
    /// </summary>
    /// <example>
    /// {{time.date}} => 2031年1月12日，星期一
    /// </example>
    /// <returns> 当前日期 </returns>
    [KernelFunction, Description("获取当前日期")]
    public static string Date(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("D", formatProvider);

    /// <summary>
    /// 获取当前日期
    /// </summary>
    /// <example>
    /// {{time.today}} => 2031年1月12日，星期一
    /// </example>
    /// <returns> 当前日期 </returns>
    [KernelFunction, Description("获取当前日期")]
    public static string Today(IFormatProvider? formatProvider = null) => Date(formatProvider);

    /// <summary>
    /// 获取本地时区的当前日期和时间
    /// </summary>
    /// <example>
    /// {{time.now}} => 2025年1月12日，星期日 下午9:15
    /// </example>
    /// <returns> 本地时区的当前日期和时间 </returns>
    [KernelFunction, Description("获取本地时区的当前日期和时间")]
    public static string Now(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("f", formatProvider);

    /// <summary>
    /// 获取当前UTC日期和时间
    /// </summary>
    /// <example>
    /// {{time.utcNow}} => 2025年1月13日，星期一 上午5:15
    /// </example>
    /// <returns> 当前UTC日期和时间 </returns>
    [KernelFunction, Description("获取当前UTC日期和时间")]
    public static string UtcNow(IFormatProvider? formatProvider = null) => DateTimeOffset.UtcNow.ToString("f", formatProvider);

    /// <summary>
    /// 获取当前时间
    /// </summary>
    /// <example>
    /// {{time.time}} => 下午09:15:07
    /// </example>
    /// <returns> 当前时间 </returns>
    [KernelFunction, Description("获取当前时间")]
    public static string Time(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("hh:mm:ss tt", formatProvider);

    /// <summary>
    /// 获取当前年份
    /// </summary>
    /// <example>
    /// {{time.year}} => 2025
    /// </example>
    /// <returns> 当前年份 </returns>
    [KernelFunction, Description("获取当前年份")]
    public static string Year(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("yyyy", formatProvider);

    /// <summary>
    /// 获取当前月份名称
    /// </summary>
    /// <example>
    /// {time.month}} => 一月
    /// </example>
    /// <returns> 当前月份名称 </returns>
    [KernelFunction, Description("获取当前月份名称")]
    public static string Month(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("MMMM", formatProvider);

    /// <summary>
    /// 获取当前月份编号
    /// </summary>
    /// <example>
    /// {{time.monthNumber}} => 01
    /// </example>
    /// <returns> 当前月份编号 </returns>
    [KernelFunction, Description("获取当前月份编号")]
    public static string MonthNumber(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("MM", formatProvider);

    /// <summary>
    /// 获取月份的日期数
    /// </summary>
    /// <example>
    /// {{time.day}} => 12
    /// </example>
    /// <returns> 月份的日期数 </returns>
    [KernelFunction, Description("获取月份的日期数")]
    public static string Day(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("dd", formatProvider);

    /// <summary>
    /// 获取指定天数之前的日期
    /// </summary>
    /// <returns> 从今天起往前推指定天数后的日期 </returns>
    [KernelFunction]
    [Description("获取从今天起往前推指定天数后的日期")]
    public static string DaysAgo([Description("从今天开始往前推的天数")] double input, IFormatProvider? formatProvider = null)
        => DateTimeOffset.Now.AddDays(-input).ToString("D", formatProvider);

    /// <summary>
    /// 获取当前日期是周几
    /// </summary>
    /// <example>
    /// {{time.dayOfWeek}} => 星期一
    /// </example>
    /// <returns> 当前日期是周几 </returns>
    [KernelFunction, Description("获取当前日期是周几")]
    public static string DayOfWeek(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("dddd", formatProvider);

    /// <summary>
    /// 获取当前时间的小时（12小时制）
    /// </summary>
    /// <example>
    /// {{time.hour}} => 下午9点
    /// </example>
    /// <returns> 当前时间的小时 </returns>
    [KernelFunction, Description("获取当前时间的小时（12小时制）")]
    public static string Hour(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("h tt", formatProvider);

    /// <summary>
    /// 获取当前24小时制的小时数
    /// </summary>
    /// <example>
    /// {{time.hourNumber}} => 21
    /// </example>
    /// <returns> 当前24小时制的小时数 </returns>
    [KernelFunction, Description("获取当前24小时制的小时数")]
    public static string HourNumber(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("HH", formatProvider);

    /// <summary>
    /// 获取上一个匹配指定星期名称的日期
    /// </summary>
    /// <example>
    /// {{time.lastMatchingDay $dayName}} => 2023年5月7日，星期日
    /// </example>
    /// <returns> 上一个匹配指定星期名称的日期 </returns>
    /// <exception cref="ArgumentOutOfRangeException">星期名称不是有效的星期几</exception>
    [KernelFunction]
    [Description("获取上一个匹配英文星期名称的日期。示例：如果你今天是星期三 -> dateMatchingLastDayName 'Tuesday' => 2023年5月16日，星期二")]
    public static string DateMatchingLastDayName(
        [Description("要匹配的星期名称")] DayOfWeek input,
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
    /// 获取当前小时的分钟数
    /// </summary>
    /// <example>
    /// {{time.minute}} => 15
    /// </example>
    /// <returns> 当前小时的分钟数 </returns>
    [KernelFunction, Description("获取当前小时的分钟数")]
    public static string Minute(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("mm", formatProvider);

    /// <summary>
    /// 获取当前秒级的秒数
    /// </summary>
    /// <example>
    /// {{time.second}} => 7
    /// </example>
    /// <returns> 当前秒级的秒数 </returns>
    [KernelFunction, Description("获取当前秒级的秒数")]
    public static string Second(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("ss", formatProvider);

    /// <summary>
    /// 获取本地时区相对于UTC的偏移量
    /// </summary>
    /// <example>
    /// {{time.timeZoneOffset}} => +08:00
    /// </example>
    /// <returns> 本地时区相对于UTC的偏移量 </returns>
    [KernelFunction, Description("获取本地时区相对于UTC的偏移量")]
    public static string TimeZoneOffset(IFormatProvider? formatProvider = null) => DateTimeOffset.Now.ToString("%K", formatProvider);

    /// <summary>
    /// 获取本地时区名称
    /// </summary>
    /// <example>
    /// {{time.timeZoneName}} => 中国标准时间
    /// </example>
    /// <remark>
    /// 注意：使用"当前"时间相关的数据可能变化，因此测试时间为当前时间
    /// </remark>
    /// <returns> 本地时区名称 </returns>
    [KernelFunction, Description("获取本地时区名称")]
    public static string TimeZoneName() => TimeZoneInfo.Local.DisplayName;
}
