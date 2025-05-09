using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TrixxInjection.FileHandling
{
    public class FileH : IDisposable
    {
        private readonly FileStream fs;
        private readonly StreamWriter sw;

        public static FileH WriterFor(string path)
            => new FileH(path);

        private FileH(string path)
        {
            fs = new FileStream(
                path,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite
            );
            sw = new StreamWriter( fs );
        }

        /// <summary>
        /// Writes a blank line
        /// </summary>
        public void Blank()
            => sw.WriteLine();

        /// <summary>
        /// Writes a message with no timestamp
        /// </summary>
        /// <param name="content"></param>
        public void WriteNoTime(string content)
            => sw.WriteLine(content);

        /// <summary>
        /// Writes a message with timestamp.
        /// </summary>
        /// <param name="content"></param>
        public void Write(string content)
            => sw.WriteLine($"[{DateTime.Now}] {content}");

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            fs.Dispose();
            sw.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~FileH()
        {
            Dispose(false);
        }
    }
}
