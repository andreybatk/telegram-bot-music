using AATelegramBotMusic.Models;
using FluentFTP;
using FluentFTP.Helpers;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace AATelegramBotMusic.Ftp
{
    public class FtpService : IFtpService
    {
        private static string _host;
        private static string _username;
        private static string _password;
        private static string _remotePath;
        private static string _localPath;
        private static string _musicPath;

        private static SemaphoreSlim semaphore;

        static FtpService()
        {
            var builder = new ConfigurationBuilder()
                 .AddJsonFile($"appsettings.json", true, true);
            var config = builder.Build();

            _host = config["FtpConnection:Host"] ?? throw new InvalidOperationException("FtpConnection:Host is null!");
            _username = config["FtpConnection:UserName"] ?? throw new InvalidOperationException("FtpConnection:UserName is null!");
            _password = config["FtpConnection:Password"] ?? throw new InvalidOperationException("FtpConnection:Password is null!");
            _musicPath = config["MusicPath"] ?? throw new InvalidOperationException("MusicPath is null!");

            _remotePath = config["FtpRemoteFilePath"] ?? throw new InvalidOperationException("FtpRemoteFilePath is null!");
            _localPath = Path.GetFileName(_remotePath);

            semaphore = new SemaphoreSlim(1);
        }

        public async Task AddMusicInfoInFile(MusicInfo? info)
        {
            if (info is null)
            {
                throw new ArgumentNullException($"{nameof(info)} is null!");
            }

            await DownloadFileAsync();

            semaphore.Wait();
            await AddMusic(info);
            semaphore.Release();

            await UploadInfoFileAsync();
        }
        public async Task AddMusicFile(MusicInfo? info)
        {
            if (info is null)
            {
                throw new ArgumentNullException($"{nameof(info)} is null!");
            }

            await UploadMusicFileAsync(info);
        }
        private async Task DownloadFileAsync()
        {
            using var ftp = new AsyncFtpClient(_host, _username, _password);
            await ftp.Connect();

            var status = await ftp.DownloadFile(_localPath, _remotePath, FtpLocalExists.Overwrite);
            if (status.IsFailure()) throw new InvalidOperationException("DownloadFileAsync is failure!");
        }
        private async Task DownloadFileAsync(CancellationToken token)
        {
            using var ftp = new AsyncFtpClient(_host, _username, _password);
            await ftp.Connect(token);

            var status = await ftp.DownloadFile(_localPath, _remotePath, FtpLocalExists.Overwrite, token: token);
            if (status.IsFailure()) throw new InvalidOperationException("DownloadFileAsync is failure!");
        }
        private async Task UploadInfoFileAsync()
        {
            using var ftp = new AsyncFtpClient(_host, _username, _password);
            await ftp.Connect();

            var status = await ftp.UploadFile(_localPath, _remotePath, FtpRemoteExists.Overwrite, true);
            if (status.IsFailure()) throw new InvalidOperationException("UploadFileAsync is failure!");
        }
        private async Task UploadMusicFileAsync(MusicInfo? info)
        {
            using var ftp = new AsyncFtpClient(_host, _username, _password);
            await ftp.Connect();

            var status = await ftp.UploadFile(info.OutPath, _musicPath + info.OutPath, FtpRemoteExists.Overwrite, true);
            if (status.IsFailure()) throw new InvalidOperationException("UploadFileAsync is failure!");
        }
        private async Task UploadFileAsync(CancellationToken token)
        {
            using var ftp = new AsyncFtpClient(_host, _username, _password);
            await ftp.Connect(token);

            var status = await ftp.UploadFile(_localPath, _remotePath, FtpRemoteExists.Overwrite, true, token: token);
            if (status.IsFailure()) throw new InvalidOperationException("UploadFileAsync is failure!");
        }
        private async Task AddMusic(MusicInfo? info)
        {
            var count = await GetCountMusicFromFile();
            if(count >= 30)
            {
                await DeleteLineFromFileAsync(0);
            }
            var result = CreateResultString(info);

            using var writer = new StreamWriter(_localPath, true, Encoding.UTF8);
            await writer.WriteLineAsync(result);
        }
        private async Task<int> GetCountMusicFromFile()
        {
            using var reader = new StreamReader(_localPath, Encoding.UTF8);
            int countMusic = 0;
            string line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                countMusic++;
            }

            return countMusic;
        }
        private async Task DeleteLineFromFileAsync(int lineIndex)
        {
            string tempFilePath = _localPath + ".tmp";

            try
            {
                using (StreamReader reader = new StreamReader(_localPath, Encoding.UTF8))
                using (StreamWriter writer = new StreamWriter(tempFilePath, false, Encoding.UTF8))
                {
                    string line;
                    int currentLineIndex = 0;

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (currentLineIndex != lineIndex)
                        {
                            await writer.WriteLineAsync(line);
                        }
                        currentLineIndex++;
                    }

                    if (lineIndex >= currentLineIndex)
                    {
                        throw new ArgumentOutOfRangeException(nameof(lineIndex), "Указанный индекс строки выходит за пределы диапазона.");
                    }
                }

                File.Delete(_localPath);
                File.Move(tempFilePath, _localPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
        private string CreateResultString(MusicInfo info)
        {
            return $"\"TgMusic/{info.OutPath}\" \"{info.Name}\"";
        }
        public void DeleteMusicFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                else
                {
                    Console.WriteLine($"Не удалось удалить файл, {filePath} не найден.");
                }
            }
            catch (Exception ex)
            {
                // Обрабатываем возможные ошибки
                Console.WriteLine($"Произошла ошибка при удалении файла: {ex.Message}");
            }
        }
    }
}