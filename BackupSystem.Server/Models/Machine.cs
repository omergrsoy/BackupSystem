    using System;
using System.Collections.Generic;

namespace BackupSystem.Server.Models
{
    public class Machine
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? IpAddress { get; set; }
        public string? OSType { get; set; }
        public string? TargetDirectory { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public bool IsActive { get; set; }
        public bool IsBackupRequested { get; set; } = false;
        public string? RequestedBackupType { get; set; }
        public string? DailyBackupTime { get; set; }
        public string? ExcludedExtensions { get; set; }
        public DateTime? LastScheduledBackupDate { get; set; }
        public ICollection<BackupRecord>? Backups { get; set; }

    }
}
