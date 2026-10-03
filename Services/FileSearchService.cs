using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SearchFile.Models;

namespace SearchFile.Services
{
    public class FileSearchService
    {
        public List<string> GetAvailableDrives()
        {
            var drives = new List<string> { "Tất cả ổ đĩa (All Drives)" };
            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady)
                    {
                        drives.Add($"{drive.Name} ({drive.VolumeLabel})");
                    }
                }
            }
            catch { }
            return drives;
        }

        public async Task SearchFilesAsync(
            string searchText,
            string selectedDrive,
            string categoryFilter,
            Action<List<FileSearchResult>> onBatchFound,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 1)
            {
                return;
            }

            string term = searchText.Trim().ToLower();
            var targetRoots = new List<string>();

            if (selectedDrive.StartsWith("Tất cả"))
            {
                foreach (var d in DriveInfo.GetDrives())
                {
                    if (d.IsReady) targetRoots.Add(d.Name);
                }
            }
            else
            {
                string driveLetter = selectedDrive.Split(' ')[0];
                targetRoots.Add(driveLetter);
            }

            var enumerationOptions = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                MaxRecursionDepth = 20,
                AttributesToSkip = FileAttributes.System | FileAttributes.Hidden,
                ReturnSpecialDirectories = false
            };

            await Task.Run(() =>
            {
                var batch = new List<FileSearchResult>();
                const int batchSize = 100;
                int count = 0;

                foreach (var root in targetRoots)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var dirInfo = new DirectoryInfo(root);
                        var fileEntries = dirInfo.EnumerateFileSystemInfos("*", enumerationOptions);

                        foreach (var info in fileEntries)
                        {
                            if (cancellationToken.IsCancellationRequested) break;

                            string name = info.Name.ToLower();
                            if (!name.Contains(term)) continue;

                            bool isDir = (info.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
                            string ext = isDir ? "" : info.Extension.ToLower();

                            if (!PassesCategoryFilter(ext, isDir, categoryFilter)) continue;

                            var item = new FileSearchResult
                            {
                                FileName = info.Name,
                                FilePath = info.FullName,
                                DirectoryPath = Path.GetDirectoryName(info.FullName) ?? info.FullName,
                                Extension = ext,
                                SizeBytes = isDir ? 0 : (info is FileInfo fi ? fi.Length : 0),
                                LastModified = info.LastWriteTime,
                                IsDirectory = isDir
                            };

                            batch.Add(item);
                            count++;

                            if (batch.Count >= batchSize)
                            {
                                var temp = batch.ToList();
                                batch.Clear();
                                onBatchFound(temp);
                            }

                            if (count >= 5000) break; // Limit max 5000 items per search run
                        }

                        if (batch.Count > 0)
                        {
                            var temp = batch.ToList();
                            batch.Clear();
                            onBatchFound(temp);
                        }
                    }
                    catch { }
                }
            }, cancellationToken);
        }

        private static bool PassesCategoryFilter(string ext, bool isDir, string category)
        {
            if (category == "Tất cả") return true;

            if (category == "Thư mục" && isDir) return true;
            if (isDir) return false;

            return category switch
            {
                "Văn bản" => ext is ".pdf" or ".docx" or ".doc" or ".xlsx" or ".xls" or ".pptx" or ".txt" or ".csv",
                "Hình ảnh" => ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".svg" or ".ico",
                "Phần mềm/Script" => ext is ".exe" or ".msi" or ".bat" or ".cmd" or ".ps1" or ".apk",
                "Code/SQL" => ext is ".cs" or ".sql" or ".js" or ".ts" or ".py" or ".json" or ".html" or ".css" or ".xaml" or ".cpp" or ".h",
                _ => true
            };
        }
    }
}
