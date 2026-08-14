using System;

namespace BackupSystem.Server.Models
{
    public class BackupRecord
    {
        public int Id { get; set; }
        public int MachineId { get; set; }
        public Machine Machine { get; set; }
        public DateTime BackupDate { get; set; }
        public string FileName { get; set; }
        public double FileSize { get; set; }
        public string Status { get; set; }
    }
}
