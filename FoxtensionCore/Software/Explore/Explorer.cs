using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Foxtension.Software.Explore
{
    public class Explorer : IDisposable
    {
        private readonly ExploreInitialize _entries;
        private bool _disposed = false;

        public Explorer(ExploreInitialize entries)
        {
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        public async Task ItemRequest(ExploreTarget type, ExploreRequest request)
        {
            ValidatePathAndName();

            string path = Path.Combine(_entries.Path!, _entries.Name!);
            string newName = Path.Combine(_entries.Path!, _entries.NewName!);

            switch (type)
            {
                case ExploreTarget.File:
                    await HandleFileRequest(request, path, newName);
                    break;

                case ExploreTarget.Folder:
                    await HandleFolderRequest(request, path, newName);
                    break;

                default:
                    throw new NotSupportedException($"Unknown target type: {type}");
            }
        }

        #region File Handling

        private async Task HandleFileRequest(ExploreRequest request, string path, string newName)
        {
            if (!IsValidFileRequest(request))
                throw new NotSupportedException($"Unsupported file request: {request}");

            FileAttributes attr = File.Exists(path) ? File.GetAttributes(path) : default;

            switch (request)
            {
                case ExploreRequest.Create:
                    await HandleFileCreate(path);
                    break;

                case ExploreRequest.Delete:
                    RequireFileExists(path);
                    File.Delete(path);
                    break;

                case ExploreRequest.Rename:
                    RequireFileExists(path);
                    File.Move(path, newName);
                    break;

                case ExploreRequest.Open:
                    RequireFileExists(path);
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    break;

                case ExploreRequest.Hide:
                    RequireFileExists(path);
                    File.SetAttributes(path, attr | FileAttributes.Hidden);
                    break;

                case ExploreRequest.Unhide:
                    RequireFileExists(path);
                    File.SetAttributes(path, attr & ~FileAttributes.Hidden);
                    break;

                case ExploreRequest.SetReadonly:
                    RequireFileExists(path);
                    File.SetAttributes(path, attr | FileAttributes.ReadOnly);
                    break;

                case ExploreRequest.UnsetReadonly:
                    RequireFileExists(path);
                    File.SetAttributes(path, attr & ~FileAttributes.ReadOnly);
                    break;

                case ExploreRequest.Copy:
                    RequireFileExists(path);
                    File.Copy(path, _entries.NewPath!, overwrite: _entries.Override);
                    break;

                case ExploreRequest.Cut:
                    RequireFileExists(path);
                    RequireDifferentDestination(path, _entries.NewPath!, newName);
                    File.Move(path, _entries.NewPath!);
                    break;

                case ExploreRequest.Compress:
                    RequireFileExists(path);
                    await HandleFileCompress(path);
                    break;

                case ExploreRequest.Extract:
                    RequireFileExists(path);
                    await HandleFileExtract(path);
                    break;

                default:
                    throw new NotSupportedException($"Unknown file request: {request}");
            }
        }

        private async Task HandleFileCreate(string path)
        {
            if (!_entries.Override && File.Exists(path))
                throw new Exception($"File '{_entries.Name}' already exists.");
            if (_entries.Override && File.Exists(path))
                File.Delete(path);

            using (File.Create(path)) { }
            await Task.CompletedTask;
        }

        private async Task HandleFileCompress(string path)
        {
            string zipPath = Path.Combine(_entries.NewPath!, _entries.NewName!);

            if (!_entries.Override && File.Exists(zipPath))
                throw new Exception($"File '{_entries.NewName}' already exists.");
            if (_entries.Override && File.Exists(zipPath))
                File.Delete(zipPath);

            try
            {
                using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(path, Path.GetFileName(path)!);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to compress file: " + ex.Message, ex);
            }
            await Task.CompletedTask;
        }

        private async Task HandleFileExtract(string path)
        {
            string extractPath = Path.Combine(_entries.NewPath!, _entries.NewName!);

            if (!_entries.Override && Directory.Exists(extractPath))
                throw new Exception($"Folder '{_entries.NewName}' already exists.");
            if (_entries.Override && Directory.Exists(extractPath))
                Directory.Delete(extractPath, true);

            try
            {
                ZipFile.ExtractToDirectory(path, extractPath);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to extract file: " + ex.Message, ex);
            }
            await Task.CompletedTask;
        }

        #endregion

        #region Folder Handling

        private async Task HandleFolderRequest(ExploreRequest request, string path, string newName)
        {
            DirectoryInfo folder = new(path);

            switch (request)
            {
                case ExploreRequest.Create:
                    await HandleFolderCreate(path);
                    break;

                case ExploreRequest.Delete:
                    RequireDirectoryExists(path);
                    Directory.Delete(path, true);
                    break;

                case ExploreRequest.Rename:
                    RequireDirectoryExists(path);
                    Directory.Move(path, newName);
                    break;

                case ExploreRequest.Open:
                    RequireDirectoryExists(path);
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    break;

                case ExploreRequest.Hide:
                    RequireDirectoryExists(path);
                    folder.Attributes |= FileAttributes.Hidden;
                    break;

                case ExploreRequest.Unhide:
                    RequireDirectoryExists(path);
                    folder.Attributes &= ~FileAttributes.Hidden;
                    break;

                case ExploreRequest.SetReadonly:
                    RequireDirectoryExists(path);
                    folder.Attributes |= FileAttributes.ReadOnly;
                    break;

                case ExploreRequest.UnsetReadonly:
                    RequireDirectoryExists(path);
                    folder.Attributes &= ~FileAttributes.ReadOnly;
                    break;

                case ExploreRequest.Copy:
                    RequireDirectoryExists(path);
                    DirectoryCopy(path, _entries.NewPath!, _entries.Override);
                    break;

                case ExploreRequest.Cut:
                    RequireDirectoryExists(path);
                    RequireDifferentDestination(path, _entries.NewPath!, newName);
                    Directory.Move(path, _entries.NewPath!);
                    break;

                case ExploreRequest.Compress:
                    await HandleFolderCompress(path);
                    break;

                case ExploreRequest.Extract:
                    await HandleFolderExtract(path);
                    break;

                default:
                    throw new NotSupportedException($"Unknown folder request: {request}");
            }
        }

        private async Task HandleFolderCreate(string path)
        {
            if (Directory.Exists(path))
            {
                if (_entries.Override)
                {
                    Directory.Delete(path, true);
                    Directory.CreateDirectory(path);
                }
                else
                {
                    throw new Exception($"Folder '{_entries.Name}' already exists.");
                }
            }
            else
            {
                Directory.CreateDirectory(path);
            }
            await Task.CompletedTask;
        }

        private async Task HandleFolderCompress(string path)
        {
            string zipPath = Path.Combine(_entries.NewPath!, _entries.NewName!);

            if (!_entries.Override && File.Exists(zipPath))
                throw new Exception($"Archive '{_entries.NewName}' already exists.");
            if (_entries.Override && File.Exists(zipPath))
                File.Delete(zipPath);

            try
            {
                ZipFile.CreateFromDirectory(path, zipPath);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to compress folder: " + ex.Message, ex);
            }
            await Task.CompletedTask;
        }

        private async Task HandleFolderExtract(string path)
        {
            string extractPath = Path.Combine(_entries.NewPath!, _entries.NewName!);

            if (!_entries.Override && Directory.Exists(extractPath))
                throw new Exception($"Folder '{_entries.NewName}' already exists.");
            if (_entries.Override && Directory.Exists(extractPath))
                Directory.Delete(extractPath, true);

            try
            {
                ZipFile.ExtractToDirectory(path, extractPath);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to extract archive: " + ex.Message, ex);
            }
            await Task.CompletedTask;
        }

        private static void DirectoryCopy(string sourceDir, string destDir, bool overwrite)
        {
            DirectoryInfo dir = new(sourceDir);

            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDir}");

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, overwrite);
            }
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestDir = Path.Combine(destDir, subDir.Name);
                DirectoryCopy(subDir.FullName, newDestDir, overwrite);
            }
        }

        #endregion

        #region Validation Helpers

        private void ValidatePathAndName()
        {
            RequireNotNullOrWhiteSpace(_entries.Path, "Path is required.");
            RequireNotNullOrWhiteSpace(_entries.Name, "Name is required.");
        }

        private static void RequireNotNullOrWhiteSpace(string? value, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(message);
        }

        private static void RequireFileExists(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(path);
        }

        private static void RequireDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException(path);
        }

        private static void RequireDifferentDestination(string path, string? newPath, string? newName)
        {
            if (path == newPath || path == newName)
                throw new ArgumentException("Source and destination cannot be the same.");
        }

        private static bool IsValidFileRequest(ExploreRequest request)
        {
            return Enum.IsDefined(typeof(ExploreRequest), request);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        #endregion
    }
}