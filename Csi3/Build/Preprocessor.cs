using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Csi3.Build
{
    class Preprocessor
    {
        public string SourceCodePath { get; }
        public StringBuilder SourceCode { get; private set; }

        public IEnumerable<string> Loads { get; private set; } = Array.Empty<string>();
        public IEnumerable<string> References { get; private set; } = Array.Empty<string>();

        public Preprocessor(string sourceCodePath, Encoding encoding)
        {
            SourceCodePath = sourceCodePath;
            _encoding = encoding;
        }

        public void Preprocess(bool waitForUnlock = false)
        {
            SourceCode = new StringBuilder();

            var loads = new List<string>();
            var references = new List<string>();

            if (waitForUnlock)
            {
                var file = new FileInfo(SourceCodePath);
                while (file.IsReadLocked())
                {
                    Thread.Sleep(50);
                }
            }

            using (var stream = new FileStream(SourceCodePath, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream, _encoding))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    SourceCode.AppendLine(line);

                    if (line.StartsWith("//"))
                    {
                        if (line.StartsWith("//#load"))
                        {
                            var path = line.Split(' ').Skip(1).FirstOrDefault();
                            if (!string.IsNullOrEmpty(path))
                            {
                                loads.Add(path.TrimEnd().Trim('"'));
                            }
                        }
                        else if (line.StartsWith("//#r"))
                        {
                            var path = line.Split(' ').Skip(1).FirstOrDefault();
                            if (!string.IsNullOrEmpty(path))
                            {
                                loads.Add(path.TrimEnd().Trim('"'));
                            }
                        }
                    }
                }
            }

            Loads = loads;
            References = references;
        }

        private Encoding _encoding;
    }
}
