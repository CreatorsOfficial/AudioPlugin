using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssetsTools.NET;
using AssetsTools;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;
using TexturePlugin;

namespace TexturePlugin
{
    public class AudioLibrary
    {
        public int m_Format;
        public AudioType m_Type;
        public bool m_3D;
        public bool m_UseHardware;
        public int m_Stream;

        //version 5
        public int m_LoadType;
        public int m_Channels;
        public int m_Frequency;
        public int m_BitsPerSample;
        public float m_Length;
        public bool m_IsTrackerFormat;
        public int m_SubsoundIndex;
        public bool m_PreloadAudioData;
        public bool m_LoadInBackground;
        public bool m_Legacy3D;
        public AudioCompressionFormat m_CompressionFormat;

        public string m_Source;
        public long m_Offset; //ulong
        public long m_Size; //ulong
        public ResourceReader m_AudioData;

        public AudioLibrary ReadAudioFields(AudioLibrary al, AssetTypeValueField basefield)
        {
            try
            {
                ///due to some errors these fields have been disabled
                ///the plugin only works with asset bundles but you can get it working with assets files with a little modification
                ///also it might support unity versions below 5 but no guarantee
                ///for now enjoy!!!
                /*if (version < 5)
                {
                    al.m_Format = basefield.Get("m_Format").GetValue().AsInt();
                    al.m_Type = (AudioType)basefield.Get("m_Type").GetValue().AsInt();
                    al.m_3D = basefield.Get("m_3D").GetValue().AsBool();
                    al.m_UseHardware = basefield.Get("m_UseHardware").GetValue().AsBool();
                    if (version >= 4 || (version == 3 && version >= 2))
                    {
                        al.m_Stream = basefield.Get("m_Stream").GetValue().AsInt();
                        al.m_Size = basefield.Get("m_Size").GetValue().AsInt();
                        al.m_Offset = basefield.Get("m_Offset").GetValue().AsInt();
                        al.m_Source = basefield.Get("m_Source").GetValue().AsString();
                    }
                    else
                    {
                        al.m_Size = basefield.Get("m_Size").GetValue().AsInt();
                    }
                }
                else*/
                {
                    /*al.m_LoadType = basefield.Get("m_LoadType").GetValue().AsInt();
                    al.m_Channels = basefield.Get("m_Channels").GetValue().AsInt();
                    al.m_Frequency = basefield.Get("m_Frequency").GetValue().AsInt();
                    al.m_BitsPerSample = basefield.Get("m_BitsPerSample").GetValue().AsInt();
                    al.m_Length = basefield.Get("m_Length").GetValue().AsFloat();
                    al.m_IsTrackerFormat = basefield.Get("m_IsTrackerFormat").GetValue().AsBool();
                    al.m_SubsoundIndex = basefield.Get("m_SubsoundIndex").GetValue().AsInt();
                    al.m_PreloadAudioData = basefield.Get("m_PreloadAudioData").GetValue().AsBool();
                    al.m_LoadInBackground = basefield.Get("m_LoadInBackground").GetValue().AsBool();
                    al.m_Legacy3D = basefield.Get("m_Legacy3D").GetValue().AsBool();*/
                    al.m_CompressionFormat = (AudioCompressionFormat)basefield.Get("m_CompressionFormat").GetValue().AsInt();
                    al.m_Source = basefield.Get("m_Resource").Get("m_Source").GetValue().AsString();
                    al.m_Offset = basefield.Get("m_Resource").Get("m_Offset").GetValue().AsInt64();
                    al.m_Size = basefield.Get("m_Resource").Get("m_Size").GetValue().AsInt64();
                }

                return al;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public AudioLibrary ReadResource(AudioLibrary al, MemoryStream resourceStream, long offset, long size)
        {
            {
                ResourceReader reader = new ResourceReader(resourceStream, offset, size);
                al.m_AudioData = reader;
                return al;
            }
        }
    }
    public enum AudioType
    {
        UNKNOWN,
        ACC,
        AIFF,
        IT = 10,
        MOD = 12,
        MPEG,
        OGGVORBIS,
        S3M = 17,
        WAV = 20,
        XM,
        XMA,
        VAG,
        AUDIOQUEUE
    }

    public enum AudioCompressionFormat
    {
        PCM,
        Vorbis,
        ADPCM,
        MP3,
        VAG,
        HEVAG,
        XMA,
        AAC,
        GCADPCM,
        ATRAC9
    }
    public class ResourceReader
    {
        public MemoryStream resourceStream;
        private long offset;
        private long size;
        private BinaryReader reader;

        public ResourceReader(MemoryStream resourceStream, long offset, long size)
        {
            this.resourceStream = resourceStream;
            this.offset = offset;
            this.size = size;
        }

        public byte[] GetData()
        {
            byte[] buffer = new byte[size];
            resourceStream.Position = offset;
            resourceStream.ReadBuffer(buffer, 0, buffer.Length);
            return buffer;
        }

        public void WriteData(string path)
        {
            var binaryReader = reader;
            binaryReader.BaseStream.Position = offset;
            using (var writer = File.OpenWrite(path))
            {
                binaryReader.BaseStream.CopyTo(writer, (int)size);
            }
        }
    }
    public static class StreamExtensions
    {
        public static void ReadBuffer(this MemoryStream _this, byte[] buffer, int offset, int count)
        {
            do
            {
                int read = _this.Read(buffer, offset, count);
                if (read == 0)
                {
                    throw new Exception($"No data left");
                }
                offset += read;
                count -= read;
            }
            while (count > 0);
        }

        public static void CopyStream(this MemoryStream _this, Stream dstStream)
        {
            byte[] buffer = new byte[BufferSize];
            while (true)
            {
                int offset = 0;
                int count = BufferSize;
                int toWrite = 0;

                int read;
                do
                {
                    read = _this.Read(buffer, offset, count);
                    offset += read;
                    count -= read;
                    toWrite += read;
                } while (read != 0);

                dstStream.Write(buffer, 0, toWrite);
                if (toWrite != BufferSize)
                {
                    return;
                }
            }
        }

        public static void CopyStream(this MemoryStream _this, Stream dstStream, long size)
        {
            byte[] buffer = new byte[BufferSize];
            for (long left = size; left > 0; left -= BufferSize)
            {
                int toRead = BufferSize < left ? BufferSize : (int)left;
                int offset = 0;
                int count = toRead;
                while (count > 0)
                {
                    int read = _this.Read(buffer, offset, count);
                    if (read == 0)
                    {
                        throw new Exception($"No data left");
                    }
                    offset += read;
                    count -= read;
                }
                dstStream.Write(buffer, 0, toRead);
            }
        }

        private const int BufferSize = 81920;
    }
    public static class DllLoader
    {

        public static void PreloadDll(string dllName)
        {
            var dllDir = GetDirectedDllDirectory();

            // Not using OperatingSystem.Platform.
            // See: https://www.mono-project.com/docs/faq/technical/#how-to-detect-the-execution-platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Win32.LoadDll(dllDir, dllName);
            }
            else
            {
                Posix.LoadDll(dllDir, dllName);
            }
        }

        private static string GetDirectedDllDirectory()
        {
            var localPath = Process.GetCurrentProcess().MainModule.FileName;
            var localDir = Path.GetDirectoryName(localPath);

            var subDir = Environment.Is64BitProcess ? "plugins/x64" : "plugins/x86";

            var directedDllDir = Path.Combine(localDir, subDir);

            return directedDllDir;
        }

        private static class Win32
        {

            internal static void LoadDll(string dllDir, string dllName)
            {
                var dllFileName = $"{dllName}.dll";
                var directedDllPath = Path.Combine(dllDir, dllFileName);

                // Specify SEARCH_DLL_LOAD_DIR to load dependent libraries located in the same platform-specific directory.
                var hLibrary = LoadLibraryEx(directedDllPath, IntPtr.Zero, LOAD_LIBRARY_SEARCH_DEFAULT_DIRS | LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR);

                if (hLibrary == IntPtr.Zero)
                {
                    var errorCode = Marshal.GetLastWin32Error();
                    var exception = new Win32Exception(errorCode);

                    throw new DllNotFoundException(exception.Message, exception);
                }
            }

            // HMODULE LoadLibraryExA(LPCSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);
            // HMODULE LoadLibraryExW(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);
            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern IntPtr LoadLibraryEx(string lpLibFileName, IntPtr hFile, uint dwFlags);

            private const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x1000;
            private const uint LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x100;

        }

        private static class Posix
        {

            internal static void LoadDll(string dllDir, string dllName)
            {
                string dllExtension;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    dllExtension = ".so";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    dllExtension = ".dylib";
                }
                else
                {
                    throw new NotSupportedException();
                }

                var dllFileName = $"lib{dllName}{dllExtension}";
                var directedDllPath = Path.Combine(dllDir, dllFileName);

                const int ldFlags = RTLD_NOW | RTLD_GLOBAL;
                var hLibrary = DlOpen(directedDllPath, ldFlags);

                if (hLibrary == IntPtr.Zero)
                {
                    var pErrStr = DlError();
                    // `PtrToStringAnsi` always uses the specific constructor of `String` (see dotnet/core#2325),
                    // which in turn interprets the byte sequence with system default codepage. On OSX and Linux
                    // the codepage is UTF-8 so the error message should be handled correctly.
                    var errorMessage = Marshal.PtrToStringAnsi(pErrStr);

                    throw new DllNotFoundException(errorMessage);
                }
            }

            // OSX and most Linux OS use LP64 so `int` is still 32-bit even on 64-bit platforms.
            // void *dlopen(const char *filename, int flag);
            [DllImport("libdl", EntryPoint = "dlopen")]
            private static extern IntPtr DlOpen([MarshalAs(UnmanagedType.LPStr)] string fileName, int flags);

            // char *dlerror(void);
            [DllImport("libdl", EntryPoint = "dlerror")]
            private static extern IntPtr DlError();

            private const int RTLD_LAZY = 0x1;
            private const int RTLD_NOW = 0x2;
            private const int RTLD_GLOBAL = 0x100;

        }

    }
}
