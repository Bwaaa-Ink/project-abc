using System;
using System.IO;

namespace TrixxInjection.Framework.FileHandling
{
    public static class StaticFileHandler
    {
        private static string path;
        private static bool _isReady = false;
        private static FileStream fs;
        private static StreamWriter sw;

        private static bool isReady
        {
            get
            {
                if (_isReady)
                    return true;
                if (string.IsNullOrWhiteSpace(path) || fs == null || sw == null)
                    return false;
                _isReady = true;
                return true;
            }
        }

        public static void Configure(string path)
        {
            if (isReady)
                return;
            StaticFileHandler.path = path;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (!File.Exists(path))
                File.Create(path);
            fs = new FileStream(
                path,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.ReadWrite
            );
            sw = new StreamWriter(fs);
            sw.AutoFlush = true;
        }

        /// <summary>
        /// Writes a blank line
        /// </summary>
        public static void Blank()
            => sw?.WriteLine();

        /// <summary>
        /// Writes a message with no timestamp
        /// </summary>
        /// <param name="content"></param>
        public static void WriteNoTime(string content)
            => sw?.WriteLine(content);

        /// <summary>
        /// Writes a message with timestamp.
        /// </summary>
        /// <param name="content"></param>
        public static void Write(string content)
            => sw?.WriteLine($"[{DateTime.Now}] {content}");

        private static void Cleanup(bool disposing)
        {
            if (!disposing || !isReady) return;
            fs.Dispose();
            sw.Dispose();
        }
    }
}
