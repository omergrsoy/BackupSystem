using System;

namespace BackupSystem.Server.Models
{
    public class SystemLog
    {
        public int Id { get; set; }
        public DateTime LogDate { get; set; } = DateTime.Now;
        public string? LogLevel { get; set; }
        public string? Message { get; set; }
        public string? MachineName { get; set; }
        public string? Details { get; set; }
    }
}