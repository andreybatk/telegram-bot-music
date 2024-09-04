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
Build:
- dotnet publish --configuration Release --runtime linux-x64 --self-contained
Deploy on VPS:
- sudo apt-get update; sudo apt-get install ffmpeg
- загрузить папку в root (добавить атрибуты доступа к папке  и к файлу AATelegramBotMusic: 777)
- cd TelegramBotMusic1
- ./AATelegramBotMusic
## Технологии
- ASP NET CORE MVC (.NET 7)
- Entity Framework Core (7.0.19)
- NAudio
- FluentFTP
- Telegram.Bot

## Слои
- AATelegramBotMusic - Console App
- AATelegramBotMusic.DB - Data Access
