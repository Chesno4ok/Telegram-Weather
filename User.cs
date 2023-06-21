using System;
using System.Collections.Generic;

namespace TelegramWeather;

public partial class User
{
    public long Id { get; set; }

    public string? Username { get; set; }

    public long? ChatId { get; set; }

    public double? Lat { get; set; }

    public double? Lon { get; set; }

    public string? AlarmTime { get; set; }
}
