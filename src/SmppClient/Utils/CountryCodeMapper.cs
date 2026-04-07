namespace SmppClient.Utils;

public static class CountryCodeMapper
{
    private static readonly Dictionary<string, CountryInfo> Countries = new()
    {
        // 1开头 (北美)
        { "1", new CountryInfo("1", "北美", "+1", new[] { "US", "CA" }) },

        // 7开头 (俄罗斯/哈萨克斯坦)
        { "7", new CountryInfo("7", "俄罗斯/哈萨克斯坦", "+7", new[] { "RU", "KZ" }) },

        // 20-30 非洲
        { "20", new CountryInfo("20", "埃及", "+20", new[] { "EG" }) },
        { "27", new CountryInfo("27", "南非", "+27", new[] { "ZA" }) },
        { "30", new CountryInfo("30", "希腊", "+30", new[] { "GR" }) },

        // 31-34 欧洲
        { "31", new CountryInfo("31", "荷兰", "+31", new[] { "NL" }) },
        { "32", new CountryInfo("32", "比利时", "+32", new[] { "BE" }) },
        { "33", new CountryInfo("33", "法国", "+33", new[] { "FR" }) },
        { "34", new CountryInfo("34", "西班牙", "+34", new[] { "ES" }) },
        { "36", new CountryInfo("36", "匈牙利", "+36", new[] { "HU" }) },
        { "39", new CountryInfo("39", "意大利", "+39", new[] { "IT" }) },

        // 40-49 欧洲
        { "40", new CountryInfo("40", "罗马尼亚", "+40", new[] { "RO" }) },
        { "41", new CountryInfo("41", "瑞士", "+41", new[] { "CH" }) },
        { "43", new CountryInfo("43", "奥地利", "+43", new[] { "AT" }) },
        { "44", new CountryInfo("44", "英国", "+44", new[] { "GB" }) },
        { "45", new CountryInfo("45", "丹麦", "+45", new[] { "DK" }) },
        { "46", new CountryInfo("46", "瑞典", "+46", new[] { "SE" }) },
        { "47", new CountryInfo("47", "挪威", "+47", new[] { "NO" }) },
        { "48", new CountryInfo("48", "波兰", "+48", new[] { "PL" }) },
        { "49", new CountryInfo("49", "德国", "+49", new[] { "DE" }) },

        // 51-58 南美
        { "51", new CountryInfo("51", "秘鲁", "+51", new[] { "PE" }) },
        { "52", new CountryInfo("52", "墨西哥", "+52", new[] { "MX" }) },
        { "53", new CountryInfo("53", "古巴", "+53", new[] { "CU" }) },
        { "54", new CountryInfo("54", "阿根廷", "+54", new[] { "AR" }) },
        { "55", new CountryInfo("55", "巴西", "+55", new[] { "BR" }) },
        { "56", new CountryInfo("56", "智利", "+56", new[] { "CL" }) },
        { "57", new CountryInfo("57", "哥伦比亚", "+57", new[] { "CO" }) },
        { "58", new CountryInfo("58", "委内瑞拉", "+58", new[] { "VE" }) },

        // 60-66 东南亚
        { "60", new CountryInfo("60", "马来西亚", "+60", new[] { "MY" }) },
        { "61", new CountryInfo("61", "澳大利亚", "+61", new[] { "AU" }) },
        { "62", new CountryInfo("62", "印度尼西亚", "+62", new[] { "ID" }) },
        { "63", new CountryInfo("63", "菲律宾", "+63", new[] { "PH" }) },
        { "64", new CountryInfo("64", "新西兰", "+64", new[] { "NZ" }) },
        { "65", new CountryInfo("65", "新加坡", "+65", new[] { "SG" }) },
        { "66", new CountryInfo("66", "泰国", "+66", new[] { "TH" }) },

        // 81-82 东亚
        { "81", new CountryInfo("81", "日本", "+81", new[] { "JP" }) },
        { "82", new CountryInfo("82", "韩国", "+82", new[] { "KR" }) },
        { "84", new CountryInfo("84", "越南", "+84", new[] { "VN" }) },

        // 86 中国
        { "86", new CountryInfo("86", "中国", "+86", new[] { "CN" }) },

        // 90-99 中东/南亚
        { "90", new CountryInfo("90", "土耳其", "+90", new[] { "TR" }) },
        { "91", new CountryInfo("91", "印度", "+91", new[] { "IN" }) },
        { "92", new CountryInfo("92", "巴基斯坦", "+92", new[] { "PK" }) },
        { "93", new CountryInfo("93", "阿富汗", "+93", new[] { "AF" }) },
        { "94", new CountryInfo("94", "斯里兰卡", "+94", new[] { "LK" }) },
        { "95", new CountryInfo("95", "缅甸", "+95", new[] { "MM" }) },
        { "98", new CountryInfo("98", "伊朗", "+98", new[] { "IR" }) },
    };

    public static CountryInfo? GetCountryInfo(string countryCode)
    {
        return Countries.GetValueOrDefault(countryCode);
    }

    public static string ExtractCountryCode(string mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile))
            return "86";

        mobile = mobile.Trim().Replace(" ", "").Replace("-", "");

        if (mobile.StartsWith("+"))
            mobile = mobile[1..];

        // 尝试匹配2位国家代码
        if (mobile.Length >= 2)
        {
            var twoDigit = mobile[..2];
            if (Countries.ContainsKey(twoDigit))
                return twoDigit;
        }

        // 尝试匹配3位国家代码
        if (mobile.Length >= 3)
        {
            var threeDigit = mobile[..3];
            if (Countries.ContainsKey(threeDigit))
                return threeDigit;
        }

        // 常见默认值
        if (mobile.StartsWith("0"))
            return "86";

        return "86";
    }

    public static IEnumerable<string> GetAllCountryCodes()
    {
        return Countries.Keys.OrderBy(k => k);
    }
}

public class CountryInfo
{
    public string CountryCode { get; }
    public string Name { get; }
    public string Prefix { get; }
    public string[] CountryNames { get; }

    public CountryInfo(string countryCode, string name, string prefix, string[] countryNames)
    {
        CountryCode = countryCode;
        Name = name;
        Prefix = prefix;
        CountryNames = countryNames;
    }

    public override string ToString() => $"{Name} ({CountryCode})";
}
