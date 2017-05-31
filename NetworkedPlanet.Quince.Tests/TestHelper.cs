using System;
using System.IO;

namespace NetworkedPlanet.Quince.Tests
{
    public static class TestHelper
    {
        public static void DeleteDirectory(string directory)
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (Exception)
            {
                SetAttributesNormal(new DirectoryInfo(directory));
                DeleteDirectory(directory);
            }
        }

        private static void SetAttributesNormal(DirectoryInfo dir)
        {
            foreach (var subDir in dir.GetDirectories())
            {
                subDir.Attributes = FileAttributes.Normal;
                SetAttributesNormal(subDir);
            }
            foreach (var file in dir.GetFiles())
            {
                file.Attributes = FileAttributes.Normal;
            }
        }
    }
}
