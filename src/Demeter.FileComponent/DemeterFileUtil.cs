using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Demeter.FileComponent
{
    internal static class DemeterFileUtil
    {
        private static string EnsureFolder(string baseFolderPath, string filename)
        {
            var currentFileFolderPath = $@"{baseFolderPath}\{filename.Substring(0, 2)}";
            var currentFileFolder = new DirectoryInfo(currentFileFolderPath);

            if (currentFileFolder.Exists)
            {
                return currentFileFolderPath;
            }

            currentFileFolder.Create();

            return currentFileFolderPath;
        }

        public static async Task WriteAsync(string baseFolderPath, string filename, byte[] content)
        {
            var currentFilePath = $@"{EnsureFolder(baseFolderPath, filename)}\{filename}";

            if (File.Exists(currentFilePath))
            {
                File.Delete(currentFilePath);
            }

            using (MemoryStream memoryStream = new MemoryStream(content))
            {
                using (FileStream fileStream = File.Create(currentFilePath))
                {
                    using (GZipStream compress = new GZipStream(fileStream, CompressionMode.Compress))
                    {
                        await memoryStream.CopyToAsync(compress);
                    }
                }
            }
        }

        public static Task DeleteAsync(string baseFolderPath, string filename)
        {
            var currentFilePath = $@"{EnsureFolder(baseFolderPath, filename)}\{filename}";

            if (File.Exists(currentFilePath))
            {
                File.Delete(currentFilePath);
            }

            return Task.FromResult(0);
        }

        public static async Task<byte[]> ReadAsync(string baseFolderPath, string filename)
        {
            var currentFilePath = $@"{EnsureFolder(baseFolderPath, filename)}\{filename}";

            if (File.Exists(currentFilePath) == false)
            {
                return null;
            }

            MemoryStream memoryStream = new MemoryStream();

            using (FileStream fileStream = File.Open(currentFilePath, FileMode.Open))
            {
                using (GZipStream decompress = new GZipStream(fileStream, CompressionMode.Decompress))
                {
                    await decompress.CopyToAsync(memoryStream);
                }
            }

            return memoryStream.GetBuffer();
        }
    }
}