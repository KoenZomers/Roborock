namespace KoenZomers.RoboRock.Library.Enums;

/// <summary>
/// Describes how a cleaning run was started.
/// </summary>
public enum RoborockCleanStartType
{
    /// <summary>The cleaning run was started with the physical button.</summary>
    Button = 1,

    /// <summary>The cleaning run was started from the app.</summary>
    App = 2,

    /// <summary>The cleaning run was started by a schedule.</summary>
    Schedule = 3,

    /// <summary>The cleaning run was started from Mi Home.</summary>
    MiHome = 4,

    /// <summary>The cleaning run was started through quick start.</summary>
    QuickStart = 5,

    /// <summary>The cleaning run was started by voice control.</summary>
    VoiceControl = 13,

    /// <summary>The cleaning run was started by a routine.</summary>
    Routines = 101,

    /// <summary>The cleaning run was started by Amazon Alexa.</summary>
    Alexa = 801,

    /// <summary>The cleaning run was started by Google Assistant.</summary>
    Google = 802,

    /// <summary>The cleaning run was started by IFTTT.</summary>
    Ifttt = 803,

    /// <summary>The cleaning run was started by Yandex.</summary>
    Yandex = 804,

    /// <summary>The cleaning run was started by HomeKit.</summary>
    HomeKit = 805,

    /// <summary>The cleaning run was started by XiaoAI.</summary>
    XiaoAi = 806,

    /// <summary>The cleaning run was started by Tmall Genie.</summary>
    TmallGenie = 807,

    /// <summary>The cleaning run was started by DuerOS.</summary>
    Duer = 808,

    /// <summary>The cleaning run was started by DingDong.</summary>
    DingDong = 809,

    /// <summary>The cleaning run was started by Siri.</summary>
    Siri = 810,

    /// <summary>The cleaning run was started by Clova.</summary>
    Clova = 811,

    /// <summary>The cleaning run was started by a widget.</summary>
    WidgetLaunch = 820,

    /// <summary>The cleaning run was started by a smart watch.</summary>
    SmartWatch = 821,

    /// <summary>The cleaning run was started by WeChat.</summary>
    WeChat = 901,

    /// <summary>The cleaning run was started by Alipay.</summary>
    Alipay = 902,

    /// <summary>The cleaning run was started by Aqara.</summary>
    Aqara = 903,

    /// <summary>The cleaning run was started by Hisense.</summary>
    Hisense = 904,

    /// <summary>The cleaning run was started by Huawei.</summary>
    Huawei = 905
}
