﻿using System.IO;

namespace SynclessUI.Helper
{
    public static class FileHelper
    {
        public static bool IsZipFile(string i)
        {
            FileInfo fi = new FileInfo(i);
            if (fi.Exists && fi.Extension.ToLower() == "zip")
                return true;

            return false;
        }
    }
}