using System;
using System.IO;

namespace SearchFile.Models
{
    public class FileSearchResult
    {
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string DirectoryPath { get; set; } = "";
        public string Extension { get; set; } = "";
        public long SizeBytes { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsDirectory { get; set; }

        public string FormattedSize => IsDirectory ? "<DIR>" : FormatBytes(SizeBytes);
        public string FormattedDate => LastModified == DateTime.MinValue ? "" : LastModified.ToString("yyyy-MM-dd HH:mm:ss");
        public string IconKind => IsDirectory ? "📁" : GetFileIcon(Extension);

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] suf = { "B", "KB", "MB", "GB", "TB" };
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return $"{num} {suf[place]}";
        }

        private static string GetFileIcon(string ext)
        {
            ext = ext.ToLower();
            return ext switch
            {
                ".pdf" => "📕",
                ".docx" or ".doc" => "📘",
                ".xlsx" or ".xls" or ".csv" => "📗",
                ".pptx" or ".ppt" => "📙",
                ".zip" or ".rar" or ".7z" => "📦",
                ".exe" or ".msi" or ".bat" => "⚡",
                ".png" or ".jpg" or ".jpeg" or ".gif" => "🖼️",
                ".mp4" or ".avi" or ".mkv" => "🎬",
                ".mp3" or ".wav" => "🎵",
                ".cs" or ".sql" or ".js" or ".py" or ".html" or ".json" => "💻",
                _ => "📄"
            };
        }
    }
}
