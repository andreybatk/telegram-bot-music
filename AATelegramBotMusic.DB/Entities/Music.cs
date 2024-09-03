namespace AATelegramBotMusic.DB.Entities
{
    public class Music
    {
        /// <summary>
        /// Уникальный идентификатор
        /// </summary>
        public int Id { get; set; }
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
        /// Загружено на сервер
        /// </summary>
        public bool IsLoadedOnServer { get; set; }
    }
}