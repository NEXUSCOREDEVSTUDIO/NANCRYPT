using System;
using System.Runtime.InteropServices;

namespace NanCrypt.UI
{
    public static class CoreInterop
    {
        private const string DllName = "NanCrypt.Core.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EncryptFileNative(string inputPath, string password);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int DecryptFileNative(string inputPath, string password);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ScanFileNative(string inputPath);
    }
}
