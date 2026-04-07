namespace SmppClient.Utils;

public static class CountryCodeMapper
{
    private static readonly Dictionary<string, CountryInfo> Countries = new()
    {
        // ============== 北美 (North America) ==============
        { "1", new CountryInfo("1", "北美", "+1", new[] { "US", "CA", "AG", "AI", "BB", "BM", "BS", "CU", "DM", "DO", "GD", "JM", "KN", "KY", "LC", "MS", "PR", "TC", "TT", "VC", "VG", "VI" }) },

        // ============== 南美 (South America) ==============
        { "51", new CountryInfo("51", "秘鲁", "+51", new[] { "PE" }) },
        { "54", new CountryInfo("54", "阿根廷", "+54", new[] { "AR" }) },
        { "55", new CountryInfo("55", "巴西", "+55", new[] { "BR" }) },
        { "56", new CountryInfo("56", "智利", "+56", new[] { "CL" }) },
        { "57", new CountryInfo("57", "哥伦比亚", "+57", new[] { "CO" }) },
        { "58", new CountryInfo("58", "委内瑞拉", "+58", new[] { "VE" }) },
        { "591", new CountryInfo("591", "玻利维亚", "+591", new[] { "BO" }) },
        { "593", new CountryInfo("593", "厄瓜多尔", "+593", new[] { "EC" }) },
        { "595", new CountryInfo("595", "巴拉圭", "+595", new[] { "PY" }) },
        { "597", new CountryInfo("597", "苏里南", "+597", new[] { "SR" }) },
        { "598", new CountryInfo("598", "乌拉圭", "+598", new[] { "UY" }) },

        // ============== 欧洲 (Europe) ==============
        { "30", new CountryInfo("30", "希腊", "+30", new[] { "GR" }) },
        { "31", new CountryInfo("31", "荷兰", "+31", new[] { "NL" }) },
        { "32", new CountryInfo("32", "比利时", "+32", new[] { "BE" }) },
        { "33", new CountryInfo("33", "法国", "+33", new[] { "FR" }) },
        { "34", new CountryInfo("34", "西班牙", "+34", new[] { "ES" }) },
        { "36", new CountryInfo("36", "匈牙利", "+36", new[] { "HU" }) },
        { "39", new CountryInfo("39", "意大利", "+39", new[] { "IT" }) },
        { "40", new CountryInfo("40", "罗马尼亚", "+40", new[] { "RO" }) },
        { "41", new CountryInfo("41", "瑞士", "+41", new[] { "CH" }) },
        { "43", new CountryInfo("43", "奥地利", "+43", new[] { "AT" }) },
        { "44", new CountryInfo("44", "英国", "+44", new[] { "GB", "UK" }) },
        { "45", new CountryInfo("45", "丹麦", "+45", new[] { "DK" }) },
        { "46", new CountryInfo("46", "瑞典", "+46", new[] { "SE" }) },
        { "47", new CountryInfo("47", "挪威", "+47", new[] { "NO" }) },
        { "48", new CountryInfo("48", "波兰", "+48", new[] { "PL" }) },
        { "49", new CountryInfo("49", "德国", "+49", new[] { "DE" }) },
        { "351", new CountryInfo("351", "葡萄牙", "+351", new[] { "PT" }) },
        { "352", new CountryInfo("352", "卢森堡", "+352", new[] { "LU" }) },
        { "353", new CountryInfo("353", "爱尔兰", "+353", new[] { "IE" }) },
        { "354", new CountryInfo("354", "冰岛", "+354", new[] { "IS" }) },
        { "355", new CountryInfo("355", "阿尔巴尼亚", "+355", new[] { "AL" }) },
        { "356", new CountryInfo("356", "马耳他", "+356", new[] { "MT" }) },
        { "357", new CountryInfo("357", "塞浦路斯", "+357", new[] { "CY" }) },
        { "358", new CountryInfo("358", "芬兰", "+358", new[] { "FI" }) },
        { "359", new CountryInfo("359", "保加利亚", "+359", new[] { "BG" }) },
        { "370", new CountryInfo("370", "立陶宛", "+370", new[] { "LT" }) },
        { "371", new CountryInfo("371", "拉脱维亚", "+371", new[] { "LV" }) },
        { "372", new CountryInfo("372", "爱沙尼亚", "+372", new[] { "EE" }) },
        { "373", new CountryInfo("373", "摩尔多瓦", "+373", new[] { "MD" }) },
        { "374", new CountryInfo("374", "亚美尼亚", "+374", new[] { "AM" }) },
        { "375", new CountryInfo("375", "白俄罗斯", "+375", new[] { "BY" }) },
        { "376", new CountryInfo("376", "安道尔", "+376", new[] { "AD" }) },
        { "377", new CountryInfo("377", "摩纳哥", "+377", new[] { "MC" }) },
        { "378", new CountryInfo("378", "圣马力诺", "+378", new[] { "SM" }) },
        { "380", new CountryInfo("380", "乌克兰", "+380", new[] { "UA" }) },
        { "381", new CountryInfo("381", "塞尔维亚", "+381", new[] { "RS" }) },
        { "382", new CountryInfo("382", "黑山", "+382", new[] { "ME" }) },
        { "383", new CountryInfo("383", "科索沃", "+383", new[] { "XK" }) },
        { "385", new CountryInfo("385", "克罗地亚", "+385", new[] { "HR" }) },
        { "386", new CountryInfo("386", "斯洛文尼亚", "+386", new[] { "SI" }) },
        { "387", new CountryInfo("387", "波黑", "+387", new[] { "BA" }) },
        { "389", new CountryInfo("389", "北马其顿", "+389", new[] { "MK" }) },
        { "420", new CountryInfo("420", "捷克", "+420", new[] { "CZ" }) },
        { "421", new CountryInfo("421", "斯洛伐克", "+421", new[] { "SK" }) },

        // ============== 俄罗斯/独联体 ==============
        { "7", new CountryInfo("7", "俄罗斯/哈萨克斯坦", "+7", new[] { "RU", "KZ" }) },

        // ============== 中东 (Middle East) ==============
        { "90", new CountryInfo("90", "土耳其", "+90", new[] { "TR" }) },
        { "90", new CountryInfo("90", "北塞浦路斯", "+90", new[] { "CY" }) },
        { "92", new CountryInfo("92", "巴基斯坦", "+92", new[] { "PK" }) },
        { "93", new CountryInfo("93", "阿富汗", "+93", new[] { "AF" }) },
        { "94", new CountryInfo("94", "斯里兰卡", "+94", new[] { "LK" }) },
        { "95", new CountryInfo("95", "缅甸", "+95", new[] { "MM" }) },
        { "98", new CountryInfo("98", "伊朗", "+98", new[] { "IR" }) },
        { "962", new CountryInfo("962", "约旦", "+962", new[] { "JO" }) },
        { "963", new CountryInfo("963", "叙利亚", "+963", new[] { "SY" }) },
        { "964", new CountryInfo("964", "伊拉克", "+964", new[] { "IQ" }) },
        { "965", new CountryInfo("965", "科威特", "+965", new[] { "KW" }) },
        { "966", new CountryInfo("966", "沙特阿拉伯", "+966", new[] { "SA" }) },
        { "967", new CountryInfo("967", "也门", "+967", new[] { "YE" }) },
        { "968", new CountryInfo("968", "阿曼", "+968", new[] { "OM" }) },
        { "970", new CountryInfo("970", "巴勒斯坦", "+970", new[] { "PS" }) },
        { "971", new CountryInfo("971", "阿联酋", "+971", new[] { "AE" }) },
        { "972", new CountryInfo("972", "以色列", "+972", new[] { "IL" }) },
        { "973", new CountryInfo("973", "巴林", "+973", new[] { "BH" }) },
        { "974", new CountryInfo("974", "卡塔尔", "+974", new[] { "QA" }) },
        { "975", new CountryInfo("975", "不丹", "+975", new[] { "BT" }) },
        { "976", new CountryInfo("976", "蒙古", "+976", new[] { "MN" }) },
        { "977", new CountryInfo("977", "尼泊尔", "+977", new[] { "NP" }) },
        { "981", new CountryInfo("981", "塔吉克斯坦", "+981", new[] { "TJ" }) },
        { "982", new CountryInfo("982", "吉尔吉斯斯坦", "+982", new[] { "KG" }) },
        { "992", new CountryInfo("992", "土库曼斯坦", "+992", new[] { "TM" }) },
        { "993", new CountryInfo("993", "Turkmenistan", "+993", new[] { "TM" }) },
        { "994", new CountryInfo("994", "阿塞拜疆", "+994", new[] { "AZ" }) },
        { "995", new CountryInfo("995", "格鲁吉亚", "+995", new[] { "GE" }) },
        { "996", new CountryInfo("996", "吉尔吉斯", "+996", new[] { "KG" }) },
        { "998", new CountryInfo("998", "乌兹别克斯坦", "+998", new[] { "UZ" }) },

        // ============== 亚洲 (Asia) ==============
        // 东亚
        { "81", new CountryInfo("81", "日本", "+81", new[] { "JP" }) },
        { "82", new CountryInfo("82", "韩国", "+82", new[] { "KR" }) },
        { "84", new CountryInfo("84", "越南", "+84", new[] { "VN" }) },
        { "86", new CountryInfo("86", "中国", "+86", new[] { "CN" }) },
        { "852", new CountryInfo("852", "香港", "+852", new[] { "HK" }) },
        { "853", new CountryInfo("853", "澳门", "+853", new[] { "MO" }) },
        { "886", new CountryInfo("886", "台湾", "+886", new[] { "TW" }) },

        // 东南亚
        { "60", new CountryInfo("60", "马来西亚", "+60", new[] { "MY" }) },
        { "61", new CountryInfo("61", "澳大利亚", "+61", new[] { "AU", "CC", "CX", "HM", "NF" }) },
        { "62", new CountryInfo("62", "印度尼西亚", "+62", new[] { "ID" }) },
        { "63", new CountryInfo("63", "菲律宾", "+63", new[] { "PH" }) },
        { "64", new CountryInfo("64", "新西兰", "+64", new[] { "NZ", "NU", "TK" }) },
        { "65", new CountryInfo("65", "新加坡", "+65", new[] { "SG" }) },
        { "66", new CountryInfo("66", "泰国", "+66", new[] { "TH" }) },
        { "670", new CountryInfo("670", "东帝汶", "+670", new[] { "TL" }) },
        { "673", new CountryInfo("673", "文莱", "+673", new[] { "BN" }) },
        { "675", new CountryInfo("675", "巴布亚新几内亚", "+675", new[] { "PG" }) },
        { "676", new CountryInfo("676", "汤加", "+676", new[] { "TO" }) },
        { "677", new CountryInfo("677", "所罗门群岛", "+677", new[] { "SB" }) },
        { "678", new CountryInfo("678", "瓦努阿图", "+678", new[] { "VU" }) },
        { "679", new CountryInfo("679", "斐济", "+679", new[] { "FJ" }) },
        { "680", new CountryInfo("680", "帕劳", "+680", new[] { "PW" }) },
        { "681", new CountryInfo("681", "瓦利斯和富图纳", "+681", new[] { "WF" }) },
        { "682", new CountryInfo("682", "库克群岛", "+682", new[] { "CK" }) },
        { "683", new CountryInfo("683", "纽埃", "+683", new[] { "NU" }) },
        { "685", new CountryInfo("685", "萨摩亚", "+685", new[] { "WS" }) },
        { "686", new CountryInfo("686", "基里巴斯", "+686", new[] { "KI" }) },
        { "687", new CountryInfo("687", "新喀里多尼亚", "+687", new[] { "NC" }) },
        { "688", new CountryInfo("688", "图瓦卢", "+688", new[] { "TV" }) },
        { "689", new CountryInfo("689", "法属波利尼西亚", "+689", new[] { "PF" }) },
        { "850", new CountryInfo("850", "朝鲜", "+850", new[] { "KP" }) },
        { "856", new CountryInfo("856", "老挝", "+856", new[] { "LA" }) },

        // 南亚
        { "91", new CountryInfo("91", "印度", "+91", new[] { "IN" }) },
        { "880", new CountryInfo("880", "孟加拉", "+880", new[] { "BD" }) },
        { "960", new CountryInfo("960", "马尔代夫", "+960", new[] { "MV" }) },

        // ============== 非洲 (Africa) ==============
        { "20", new CountryInfo("20", "埃及", "+20", new[] { "EG" }) },
        { "211", new CountryInfo("211", "南苏丹", "+211", new[] { "SS" }) },
        { "212", new CountryInfo("212", "摩洛哥", "+212", new[] { "MA" }) },
        { "213", new CountryInfo("213", "阿尔及利亚", "+213", new[] { "DZ" }) },
        { "216", new CountryInfo("216", "突尼斯", "+216", new[] { "TN" }) },
        { "218", new CountryInfo("218", "利比亚", "+218", new[] { "LY" }) },
        { "220", new CountryInfo("220", "冈比亚", "+220", new[] { "GM" }) },
        { "221", new CountryInfo("221", "塞内加尔", "+221", new[] { "SN" }) },
        { "222", new CountryInfo("222", "毛里塔尼亚", "+222", new[] { "MR" }) },
        { "223", new CountryInfo("223", "马里", "+223", new[] { "ML" }) },
        { "224", new CountryInfo("224", "几内亚", "+224", new[] { "GN" }) },
        { "225", new CountryInfo("225", "科特迪瓦", "+225", new[] { "CI" }) },
        { "226", new CountryInfo("226", "布基纳法索", "+226", new[] { "BF" }) },
        { "227", new CountryInfo("227", "尼日尔", "+227", new[] { "NE" }) },
        { "228", new CountryInfo("228", "多哥", "+228", new[] { "TG" }) },
        { "229", new CountryInfo("229", "贝宁", "+229", new[] { "BJ" }) },
        { "230", new CountryInfo("230", "毛里求斯", "+230", new[] { "MU" }) },
        { "231", new CountryInfo("231", "利比里亚", "+231", new[] { "LR" }) },
        { "232", new CountryInfo("232", "塞拉利昂", "+232", new[] { "SL" }) },
        { "233", new CountryInfo("233", "加纳", "+233", new[] { "GH" }) },
        { "234", new CountryInfo("234", "尼日利亚", "+234", new[] { "NG" }) },
        { "235", new CountryInfo("235", "乍得", "+235", new[] { "TD" }) },
        { "236", new CountryInfo("236", "中非", "+236", new[] { "CF" }) },
        { "237", new CountryInfo("237", "喀麦隆", "+237", new[] { "CM" }) },
        { "238", new CountryInfo("238", "佛得角", "+238", new[] { "CV" }) },
        { "239", new CountryInfo("239", "圣多美和普林西比", "+239", new[] { "ST" }) },
        { "240", new CountryInfo("240", "赤道几内亚", "+240", new[] { "GQ" }) },
        { "241", new CountryInfo("241", "加蓬", "+241", new[] { "GA" }) },
        { "242", new CountryInfo("242", "刚果共和国", "+242", new[] { "CG" }) },
        { "243", new CountryInfo("243", "刚果民主共和国", "+243", new[] { "CD" }) },
        { "244", new CountryInfo("244", "安哥拉", "+244", new[] { "AO" }) },
        { "245", new CountryInfo("245", "几内亚比绍", "+245", new[] { "GW" }) },
        { "246", new CountryInfo("246", "英属印度洋领地", "+246", new[] { "IO" }) },
        { "247", new CountryInfo("247", "阿森松岛", "+247", new[] { "AC" }) },
        { "248", new CountryInfo("248", "塞舌尔", "+248", new[] { "SC" }) },
        { "249", new CountryInfo("249", "苏丹", "+249", new[] { "SD" }) },
        { "250", new CountryInfo("250", "卢旺达", "+250", new[] { "RW" }) },
        { "251", new CountryInfo("251", "埃塞俄比亚", "+251", new[] { "ET" }) },
        { "252", new CountryInfo("252", "索马里", "+252", new[] { "SO" }) },
        { "253", new CountryInfo("253", "吉布提", "+253", new[] { "DJ" }) },
        { "254", new CountryInfo("254", "肯尼亚", "+254", new[] { "KE" }) },
        { "255", new CountryInfo("255", "坦桑尼亚", "+255", new[] { "TZ" }) },
        { "256", new CountryInfo("256", "乌干达", "+256", new[] { "UG" }) },
        { "257", new CountryInfo("257", "布隆迪", "+257", new[] { "BI" }) },
        { "258", new CountryInfo("258", "莫桑比克", "+258", new[] { "MZ" }) },
        { "260", new CountryInfo("260", "赞比亚", "+260", new[] { "ZM" }) },
        { "261", new CountryInfo("261", "马达加斯加", "+261", new[] { "MG" }) },
        { "262", new CountryInfo("262", "留尼汪/马约特", "+262", new[] { "RE", "YT" }) },
        { "263", new CountryInfo("263", "津巴布韦", "+263", new[] { "ZW" }) },
        { "264", new CountryInfo("264", "纳米比亚", "+264", new[] { "NA" }) },
        { "265", new CountryInfo("265", "马拉维", "+265", new[] { "MW" }) },
        { "266", new CountryInfo("266", "莱索托", "+266", new[] { "LS" }) },
        { "267", new CountryInfo("267", "博茨瓦纳", "+267", new[] { "BW" }) },
        { "268", new CountryInfo("268", "斯威士兰", "+268", new[] { "SZ" }) },
        { "269", new CountryInfo("269", "科摩罗", "+269", new[] { "KM" }) },
        { "27", new CountryInfo("27", "南非", "+27", new[] { "ZA" }) },
        { "290", new CountryInfo("290", "圣赫勒拿", "+290", new[] { "SH" }) },
        { "291", new CountryInfo("291", "厄立特里亚", "+291", new[] { "ER" }) },
        { "297", new CountryInfo("297", "阿鲁巴", "+297", new[] { "AW" }) },
        { "298", new CountryInfo("298", "法罗群岛", "+298", new[] { "FO" }) },
        { "299", new CountryInfo("299", "格陵兰", "+299", new[] { "GL" }) },

        // ============== 加勒比/中美 (Caribbean/Central America) ==============
        { "1242", new CountryInfo("1242", "巴哈马", "+1242", new[] { "BS" }) },
        { "1246", new CountryInfo("1246", "巴巴多斯", "+1246", new[] { "BB" }) },
        { "1264", new CountryInfo("1264", "安圭拉", "+1264", new[] { "AI" }) },
        { "1268", new CountryInfo("1268", "安提瓜和巴布达", "+1268", new[] { "AG" }) },
        { "1284", new CountryInfo("1284", "英属维尔京群岛", "+1284", new[] { "VG" }) },
        { "1340", new CountryInfo("1340", "美属维尔京群岛", "+1340", new[] { "VI" }) },
        { "1345", new CountryInfo("1345", "开曼群岛", "+1345", new[] { "KY" }) },
        { "1441", new CountryInfo("1441", "百慕大", "+1441", new[] { "BM" }) },
        { "1473", new CountryInfo("1473", "格林纳达", "+1473", new[] { "GD" }) },
        { "1506", new CountryInfo("1506", "蒙特塞拉特", "+1506", new[] { "MS" }) },
        { "1571", new CountryInfo("1571", "牙买加", "+1571", new[] { "JM" }) },
        { "1649", new CountryInfo("1649", "特克斯和凯科斯", "+1649", new[] { "TC" }) },
        { "1661", new CountryInfo("1661", "特立尼达和多巴哥", "+1661", new[] { "TT" }) },
        { "1670", new CountryInfo("1670", "波多黎各", "+1670", new[] { "PR" }) },
        { "1671", new CountryInfo("1671", "塞浦路斯", "+1671", new[] { "CY" }) },
        { "1675", new CountryInfo("1675", "马提尼克", "+1675", new[] { "MQ" }) },
        { "1676", new CountryInfo("1676", "瓜德罗普", "+1676", new[] { "GP" }) },
        { "1684", new CountryInfo("1684", "美属萨摩亚", "+1684", new[] { "AS" }) },
        { "1689", new CountryInfo("1689", "萨摩亚", "+685", new[] { "WS" }) },
        { "1758", new CountryInfo("1758", "圣卢西亚", "+1758", new[] { "LC" }) },
        { "1767", new CountryInfo("1767", "多米尼克", "+1767", new[] { "DM" }) },
        { "1784", new CountryInfo("1784", "圣文森特和格林纳丁斯", "+1784", new[] { "VC" }) },
        { "1787", new CountryInfo("1787", "波多黎各", "+1787", new[] { "PR" }) },
        { "1799", new CountryInfo("1799", "多米尼加", "+1799", new[] { "DO" }) },
        { "1809", new CountryInfo("1809", "海地", "+1809", new[] { "HT" }) },
        { "1868", new CountryInfo("1868", "特立尼达和多巴哥", "+1868", new[] { "TT" }) },
        { "1869", new CountryInfo("1869", "圣基茨和尼维斯", "+1869", new[] { "KN" }) },
        { "1876", new CountryInfo("1876", "牙买加", "+1876", new[] { "JM" }) },

        // ============== 其他 ==============
        { "500", new CountryInfo("500", "福克兰群岛", "+500", new[] { "FK" }) },
        { "501", new CountryInfo("501", "伯利兹", "+501", new[] { "BZ" }) },
        { "502", new CountryInfo("502", "危地马拉", "+502", new[] { "GT" }) },
        { "503", new CountryInfo("503", "萨尔瓦多", "+503", new[] { "SV" }) },
        { "504", new CountryInfo("504", "洪都拉斯", "+504", new[] { "HN" }) },
        { "505", new CountryInfo("505", "尼加拉瓜", "+505", new[] { "NI" }) },
        { "506", new CountryInfo("506", "哥斯达黎加", "+506", new[] { "CR" }) },
        { "507", new CountryInfo("507", "巴拿马", "+507", new[] { "PA" }) },
        { "508", new CountryInfo("508", "圣皮埃尔和密克隆", "+508", new[] { "PM" }) },
        { "509", new CountryInfo("509", "海地", "+509", new[] { "HT" }) },
        { "590", new CountryInfo("590", "瓜德罗普/圣马丁", "+590", new[] { "GP", "MF" }) },
        { "594", new CountryInfo("594", "法属圭亚那", "+594", new[] { "GF" }) },
        { "596", new CountryInfo("596", "马提尼克", "+596", new[] { "MQ" }) },
        { "599", new CountryInfo("599", "荷属安的列斯", "+599", new[] { "AN" }) },
        { "672", new CountryInfo("672", "南极洲/诺福克", "+672", new[] { "AQ", "NF" }) },
        { "800", new CountryInfo("800", "国际免费电话", "+800", new[] { "ITU" }) },
        { "808", new CountryInfo("808", "增强型特殊服务", "+808", new[] { "PC" }) },
        { "850", new CountryInfo("850", "朝鲜", "+850", new[] { "KP" }) },
        { "852", new CountryInfo("852", "香港", "+852", new[] { "HK" }) },
        { "853", new CountryInfo("853", "澳门", "+853", new[] { "MO" }) },
        { "855", new CountryInfo("855", "柬埔寨", "+855", new[] { "KH" }) },
        { "856", new CountryInfo("856", "老挝", "+856", new[] { "LA" }) },
        { "870", new CountryInfo("870", "卫星电话", "+870", new[] { "XN" }) },
        { "878", new CountryInfo("878", "卫星电话", "+878", new[] { "UC" }) },
        { "880", new CountryInfo("880", "孟加拉", "+880", new[] { "BD" }) },
        { "881", new CountryInfo("881", "卫星电话", "+881", new[] { "XG" }) },
        { "882", new CountryInfo("882", "卫星电话", "+882", new[] { "XG" }) },
        { "883", new CountryInfo("883", "卫星电话", "+883", new[] { "XI" }) },
        { "886", new CountryInfo("886", "台湾", "+886", new[] { "TW" }) },
        { "888", new CountryInfo("888", "卫星电话", "+888", new[] { "XC" }) },
        { "960", new CountryInfo("960", "马尔代夫", "+960", new[] { "MV" }) },
        { "961", new CountryInfo("961", "黎巴嫩", "+961", new[] { "LB" }) },
        { "962", new CountryInfo("962", "约旦", "+962", new[] { "JO" }) },
        { "963", new CountryInfo("963", "叙利亚", "+963", new[] { "SY" }) },
        { "964", new CountryInfo("964", "伊拉克", "+964", new[] { "IQ" }) },
        { "965", new CountryInfo("965", "科威特", "+965", new[] { "KW" }) },
        { "966", new CountryInfo("966", "沙特阿拉伯", "+966", new[] { "SA" }) },
        { "967", new CountryInfo("967", "也门", "+967", new[] { "YE" }) },
        { "968", new CountryInfo("968", "阿曼", "+968", new[] { "OM" }) },
        { "970", new CountryInfo("970", "巴勒斯坦", "+970", new[] { "PS" }) },
        { "971", new CountryInfo("971", "阿联酋", "+971", new[] { "AE" }) },
        { "972", new CountryInfo("972", "以色列", "+972", new[] { "IL" }) },
        { "973", new CountryInfo("973", "巴林", "+973", new[] { "BH" }) },
        { "974", new CountryInfo("974", "卡塔尔", "+974", new[] { "QA" }) },
        { "975", new CountryInfo("975", "不丹", "+975", new[] { "BT" }) },
        { "976", new CountryInfo("976", "蒙古", "+976", new[] { "MN" }) },
        { "977", new CountryInfo("977", "尼泊尔", "+977", new[] { "NP" }) },
        { "992", new CountryInfo("992", "塔吉克斯坦", "+992", new[] { "TJ" }) },
        { "993", new CountryInfo("993", "土库曼斯坦", "+993", new[] { "TM" }) },
        { "994", new CountryInfo("994", "阿塞拜疆", "+994", new[] { "AZ" }) },
        { "995", new CountryInfo("995", "格鲁吉亚", "+995", new[] { "GE" }) },
        { "996", new CountryInfo("996", "吉尔吉斯斯坦", "+996", new[] { "KG" }) },
        { "998", new CountryInfo("998", "乌兹别克斯坦", "+998", new[] { "UZ" }) },
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

        // 尝试匹配3位国家代码(优先)
        if (mobile.Length >= 3)
        {
            var threeDigit = mobile[..3];
            if (Countries.ContainsKey(threeDigit))
                return threeDigit;
        }

        // 尝试匹配2位国家代码
        if (mobile.Length >= 2)
        {
            var twoDigit = mobile[..2];
            if (Countries.ContainsKey(twoDigit))
                return twoDigit;
        }

        // 尝试匹配1位代码
        if (mobile.Length >= 1)
        {
            var oneDigit = mobile[..1];
            if (Countries.ContainsKey(oneDigit))
                return oneDigit;
        }

        return "86";
    }

    public static IEnumerable<string> GetAllCountryCodes()
    {
        return Countries.Keys.OrderBy(k => k);
    }

    public static IEnumerable<CountryInfo> GetAllCountries()
    {
        return Countries.Values.OrderBy(c => c.CountryCode).DistinctBy(c => c.CountryCode);
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
