using System;
using System.Collections.Generic;

namespace BackupSystem.Server.Models
{
    public class Machine
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string OSType { get; set; }
        public string TargetDirectory { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public bool IsActive { get; set; }
        public ICollection<BackupRecord> Backups { get; set; }

    }
}
