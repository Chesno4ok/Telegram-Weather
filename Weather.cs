using System;
using System.Collections.Generic;

namespace TelegramWeather;

public partial class Weather
{
    public long Id { get; set; }

    public double? Temp { get; set; }

    public string? Date { get; set; }


    private bool disposed = false;
    
}
