Телеграм бот, присылающий каждый день прогноз работы.



API - https://www.weatherapi.com/

Возможности:

/timeset - установка времени
/start - Информация о боте
/test - Текщущая погода

Сохранение геолокации пютем прикрепления GPS Локации.

Возможности админа:

/getall - Показывает всех пользователей и информвцию о них
/sendall [текс] - Присылает всем пользователям текст
/send - [chat_id] [текст] - Присылает определенному человеку текст

Настройка бота самостоятельно:
1. 22я строка в Program.cs добавляем Token бота
2. 20 строка там же свой ID для доступа к командам админа
3. 30я строка в WeatherApi.cs вставляете свой токен от https://www.weatherapi.com/
