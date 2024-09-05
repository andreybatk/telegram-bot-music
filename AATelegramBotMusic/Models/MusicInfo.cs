namespace AATelegramBotMusic.Models
{
    public class MusicInfo
    {
        /// <summary>
        /// Название песни
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Файл в формате MP3
        /// </summary>
        public string InPath { get; set; } = string.Empty;
        /// <summary>
        /// Файл в формате WAV
        /// </summary>
        public string OutPath { get; set; } = string.Empty;
        /// <summary>
        /// Одобрено админом
        /// </summary>
        public bool IsApproved { get; set; }
        /// <summary>
        /// Telegram UserName
        /// </summary>
        public string TgUserName { get; set; } = string.Empty;
        /// <summary>
        /// Telegram Message Id
        /// </summary>
        public int TgMessageId { get; set; }
        /// <summary>
        /// Media Group Id
        /// </summary>
        public string? MediaGroupId { get; set; }
    }
}
