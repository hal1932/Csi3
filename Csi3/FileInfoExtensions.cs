using System.IO;

namespace Csi3
{
    static class FileInfoExtensions
    {
        public static bool IsReadLocked(this FileInfo file)
        {
            FileStream stream = default;
            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read);
            }
            catch
            {
                return true;
            }
            finally
            {
                stream?.Dispose();
            }
            return false;
        }
    }
}
