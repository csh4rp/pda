using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.Infrastructure
{
    public static class FileHelper
    {
        public static bool DirectoryExists(string path)
        {
            bool exists = false;
            try
            {
                exists = Directory.Exists(path);
            }
            catch (Exception) { }
            return exists;
        }

        public static string GetDirectoryName(string path)
        {
            string name = null;

            try
            {
                name = Path.GetDirectoryName(path);
            }
            catch (Exception) { }

            return name;
        }

        public static bool IsValidPath(string path)
        {
            bool valid = false;
            try
            {
                if (Directory.Exists(Path.GetDirectoryName(path)))
                    valid = true;
            }
            catch (Exception) { }
            return valid;
        }

    }
}
