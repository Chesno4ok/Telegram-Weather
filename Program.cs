using Newtonsoft.Json;
using System.Linq.Expressions;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WeatherNet.Model;


namespace TelegramWeather
{ 


    internal class Program
    {
        static long AdmId = 0; // Id of an admin

        static TelegramBotClient botClient = new("TELGRAM TOKEN");
        static CancellationTokenSource cts = new();
        static CancellationToken ct = cts.Token;

        delegate void SendMsg(string chtId, string msg);


        private static ReceiverOptions? receiverOptions;



        static void Main(string[] args)
        {

            botClient.StartReceiving(
                    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
                );

            while(true)
            {
                Cycle();
                GC.Collect();
                Thread.Sleep(60000);
            }
        }

        static async void Cycle()
        {

            string cur = DateTime.Now.ToString("H:mm");
            Console.Title = "Telegram Weather Bot - " + DateTime.Now.ToString("H:mm:ss");
            Console.WriteLine(cur + "  " +  (GC.GetTotalMemory(true) / 1024));

            DB db = new();

            User[] usrs = db.Users.Where(p => p.AlarmTime == cur).ToArray();
            if (usrs != null)
            {
                AllSendMessage(usrs);
                usrs = null;
            }

            if(cur == "0:00")
            {
                
                db.Weathers.Add(new Weather() { Temp = WeatherApi.GetTemp().Result, Date = DateTime.Now.ToString("dd.MM.yyyy") });
                db.SaveChanges();
            }

            db = null;
        }

        static async void AllSendMessage(User[] usrs)
        {
            DB db = new();

            foreach(User a in usrs)
            {
                Task.Run(() => SendWeatherAsync(a));
            }
        }
        static async Task SendWeatherAsync(User a)
        {
            Console.WriteLine(a.Username + " sent");
            string msg = await WeatherApi.MakeRequest(a.Lat.ToString().Replace(",", "."), a.Lon.ToString().Replace(",", ".")) as string;
            await SendMessage(a.ChatId.ToString(), msg);
            a = null;
        }
        static async Task SendMessageToAll(string msg)
        {
            DB db = new();

            foreach(User usr in db.Users.ToArray())
            {
                Task.Run(() => SendMessage(usr.ChatId.ToString(), msg));
            }

            db = null;
        }

        static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        private static void AddToDb(string usrn, long id, long idc)
        {
            DB db = new DB();

            Console.WriteLine($"New User:{usrn}");
            db.Users.Add(new User() { ChatId = idc, Id = id, Username = usrn, Lat = 55.755864, Lon = 37.617698, AlarmTime = "7:00" });
            db.SaveChanges();

            db = null;
            GC.Collect();
        }


        static async Task HandleRequest(User usr)
        {
            string msg = await WeatherApi.MakeRequest(usr.Lat.ToString().Replace(",", "."), usr.Lon.ToString().Replace(",", ".")) as string;
            SendMessage(usr.Id.ToString(), msg);

            usr = null;
            GC.Collect();
        }
        static async Task<Task> HandleTimeset(long usrId, string msg)
        {
            DB db = new();
            User? usr = db.Users.Find(usrId);


            string time = msg.Replace("/timeset ", "");

            DateTime dt = new DateTime();

            if (DateTime.TryParseExact(time, "H:mm", null, System.Globalization.DateTimeStyles.None, out dt))
            {
                usr.AlarmTime = dt.ToString("H:mm");
                await SendMessage(usr.ChatId.ToString(), "Время сохранено!");
            }
            else
            {
                await SendMessage(usr.ChatId.ToString(), "Неправильный формат!");
            }

            db.SaveChanges();
            
            return Task.CompletedTask;
        }
        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            if (update.Message is not { } message)
                return;

            DB db = new();
            
            if (db.Users.Find(message.From.Id) == null)
            {
                AddToDb(message.From.Username, message.From.Id, message.Chat.Id);
            }

            User? usr = db.Users.Find(message.From.Id);

            if (message.Text != null)
            {
                Console.Write(message.From.Username + " " + message.Text);
                Regex rx = new Regex(@"/timeset");
                Regex rx1 = new Regex(@"/sendall");
                Regex rx2 = new Regex(@"/send");

                if (message.Text == "/start") // Get StartingInfo
                {
                    Task.Run(() => SendMessage(message.Chat.Id.ToString(), "Добро пожаловать в погодного бота! \n Каждый день в указанное время тебе будет приходить прогноз погоды. Прикрепи геолокацию чтобы установить место для прогноза. Также используй /timeset чтобы указать время уведомления.\n \n Команды: \n /test - проверить текущий прогноз \n /timeset [ЧАС:МИНУТА] (Пример:/timeset 6:30) - Выставляет время уведомления. (По МСК)"));
                }
                else if (message.Text == "/test") // Get Forecast
                {
                    Task.Run(() => HandleRequest(usr));
                }
                else if(message.Text == "/getall" && usr.Id == AdmId)
                {
                    string msg = "";
                    foreach(User u in db.Users)
                    {
                        msg += $"{u.Username} - {u.Id} - {u.AlarmTime} - {u.Lat} - {u.Lon} \n";
                    }

                    SendMessage(usr.Id.ToString(), msg);
                }
                else if (rx.IsMatch(message.Text)) // Timeset
                {
                    Task.Run(() => HandleTimeset(usr.Id, message.Text));
                }
                else if(rx1.IsMatch(message.Text) && usr.Id ==  AdmId)
                {
                    SendMessageToAll(message.Text.Replace("/sendall", ""));
                }
                else if(rx2.IsMatch(message.Text) && usr.Id == AdmId)
                {
                    try
                    {
                        SendMessage(message.Text.Split(' ')[1], message.Text.Split(' ')[2]);
                    }
                    catch
                    {
                        SendMessage(message.From.Id.ToString(), "Ошибка!");
                    }
                }

            }
            else if (message.Location != null) // Set Location
            {
                usr.Lat = message.Location.Latitude;
                usr.Lon = message.Location.Longitude;

                Task.Run(() => SendMessage(message.Chat.Id.ToString(), "Данные геолокации сохранены!"));
            }

            
            db.SaveChanges();
        }

        static async Task<Task> SendMessage(string chtId, string msg)
        {
            ChatId? cht = new ChatId(chtId);

            try
            {
                Message message = await botClient.SendTextMessageAsync(
    chatId: cht,
            text: msg,
    cancellationToken: ct);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine($"- sent \n");

            cht = null;
            GC.Collect();
            return Task.CompletedTask;
        }
    }
}