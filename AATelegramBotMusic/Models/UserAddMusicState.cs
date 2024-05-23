namespace AATelegramBotMusic.Models
{
    public class UserAddMusicState
    {
        public long UserId { get; set; }
        public string State { get; set; }
        public MusicInfo Music { get; set; }
    }
}