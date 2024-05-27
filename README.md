# AATelegramBotMusic
Telegram Bot Music Manager. Для взаимодействия с плагином BMSurf (amxx). 
Настройки подключениий в файле appsettings.json проекта AATelegramBotMusic.
Бот позволяет загружать музыку на сервер ftp, при этом обрабатывая ее, преобразуя из mp3 в wav.
## Настройка
Если бот будет расположен на Linux (Ubuntu):
- Используйте FFMpegMusicConverter
- Установите пакет FFMpeg: sudo apt-get update; sudo apt-get install ffmpeg

Если бот будет расположен на Windows:
- Используйте NAudioMusicConverter
## Технологии
- ASP NET CORE MVC (.NET 7)
- Entity Framework Core (7.0.19)
- NAudio
- FluentFTP
- Telegram.Bot

## Слои
- AATelegramBotMusic - Console App
