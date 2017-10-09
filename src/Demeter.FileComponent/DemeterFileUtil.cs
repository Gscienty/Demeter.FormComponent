using System.IO;
using System.IO.Compression;

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

        public static void Write(string baseFolderPath, string filename, byte[] content)
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
                        memoryStream.CopyTo(compress);
                    }
                }
            }
        }

        public static byte[] Read(string baseFolderPath, string filename)
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
                    decompress.CopyTo(memoryStream);
                }
            }

            return memoryStream.GetBuffer();
        }
    }
}