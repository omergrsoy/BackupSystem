namespace BackupSystem.Server.Models
{
    public class SystemSetting
    {
        public int Id { get; set; }
        public int ChunkSizeMB { get; set; } = 10;
        public int MaxUploadSpeedMB { get; set; } = 5;
    }
}