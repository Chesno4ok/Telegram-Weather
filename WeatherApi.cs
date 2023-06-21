using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TelegramWeather
{
    internal static class WeatherApi
    {
        public static async Task<double> GetTemp()
        {
            double t = 0;

            HttpClient httpClient = new();
            using HttpResponseMessage response = await httpClient.GetAsync("http://api.weatherapi.com/v1/forecast.json?key=c8cf4a62641d4205ba8213831230306&q=Moscow&days=1");
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Root? root = JsonConvert.DeserializeObject<Root>(jsonResponse);

            t = root.forecast.forecastday[0].day.avgtemp_c;

            return t;
        }
        public static async Task<string> MakeRequest(string lat, string lon)
        {
            HttpClient httpClient = new();

            string key = "Weather API Key";
            using HttpResponseMessage response = await httpClient.GetAsync($"http://api.weatherapi.com/v1/forecast.json?key{key}=&q={lat},{lon}&days=1&lang=ru");
            var jsonResponse = await response.Content.ReadAsStringAsync();


            Root? root = JsonConvert.DeserializeObject<Root>(jsonResponse);

            string message = "Проснись и пой!";

            message += $" Сегодня {root.forecast.forecastday[0].date}. За окном {root.forecast.forecastday[0].day.maxtemp_c} градусов. ";

            //message += GetTemp(root, lat, lon); // Определяется степень температуры.


            message += " Также сегодня будет " + root.forecast.forecastday[0].day.condition.text.ToLower() + ".";
            if (root.forecast.forecastday[0].day.daily_will_it_rain == 1)
            {
                message += GetRainingHours(root);
            }


            root = null;
            return message;
        }
        public static async Task<string> GetTemp(Root root, string lat, string lon)
        {
            string msg = "";
            DB db = new();



            Weather[] wth = db.Weathers.ToArray();

            double? t = wth[wth.Length - 1].Temp - root.forecast.forecastday[0].day.avgtemp_c;

            if (t >= -5 && t <= 5)
            {
                msg = "Температура в норме.";
            }
            else if (t >= 5 && t <= 10)
            {
                msg = "Немного потеплело.";
            }
            else if (t >= 10)
            {
                msg = "Стало жарче.";
            }
            else if (t <= -5 && t >= -10)
            {
                msg = "Немного похолодало.";
            }
            else if (t <= -10)
            {
                msg = "Стало намного холоднее.";
            }

            return msg;
        }
        public static string GetRainingHours(Root root)
        {
            string hours = "\n Дождь будет в эти часы: \n";

            bool IsPeriod = false;
            foreach (Hour hour in root.forecast.forecastday[0].hour)
            {
                if (hour.will_it_rain == 1 && !IsPeriod)
                {

                    DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, 0); //from start epoch time
                    start = start.AddSeconds(hour.time_epoch);
                    hours += start.Hour + ":00 -";
                    IsPeriod = true;
                }
                else if(hour.will_it_rain == 0 && IsPeriod)
                {
                    DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, 0); //from start epoch time
                    start = start.AddSeconds(hour.time_epoch);
                    hours += start.Hour + ":00 \n";
                    IsPeriod = false;
                }
            }


            return hours;
        }
    }
}
