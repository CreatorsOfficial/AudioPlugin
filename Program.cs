using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UABEAvalonia;
using UABEAvalonia.Plugins;
using TexturePlugin;

namespace TexturePlugin
{
    public class ExportTextAssetOption : UABEAPluginOption
    {
        /// <summary>
        /// 'RIFF' ascii
        /// </summary>
        private const uint RiffFourCC = 0x46464952;
        /// <summary>
        /// 'WAVEfmt ' ascii
        /// </summary>
        private const ulong WaveEightCC = 0x20746D6645564157;
        /// <summary>
        /// 'data' ascii
        /// </summary>
        private const uint DataFourCC = 0x61746164;
        public bool SelectionValidForPlugin(AssetsManager am, UABEAPluginAction action, List<AssetContainer> selection, out string name)
        {
            name = "Export Audio (Plugin by Creators)";

            if (action != UABEAPluginAction.Export)
                return false;

            int classId = AssetHelper.FindAssetClassByName(am.classFile, "AudioClip").classId;

            foreach (AssetContainer cont in selection)
            {
                if (cont.ClassId != classId)
                    return false;
            }
            return true;
        }

        public async Task<bool> ExecutePlugin(Window win, AssetWorkspace workspace, List<AssetContainer> selection)
        {
            if (selection.Count > 1)
                return await BatchExport(win, workspace, selection);
            else
                return await SingleExport(win, workspace, selection);
        }

        public async Task<bool> BatchExport(Window win, AssetWorkspace workspace, List<AssetContainer> selection)
        {
            OpenFolderDialog ofd = new OpenFolderDialog();
            ofd.Title = "Select export directory";

            string dir = await ofd.ShowAsync(win);

            if (dir != null && dir != string.Empty)
            {
                foreach (AssetContainer cont in selection)
                {
                    AssetTypeValueField baseField = workspace.GetBaseField(cont);
                    AudioLibrary al = new AudioLibrary();
                    al.ReadAudioFields(al, baseField);
                    ResourceReader rer = null;
                    byte[] buff = null;
                    if (cont.FileInstance.parentBundle.file == null)
                    {
                        //assets files not supported yet so return null;
                    }
                    else
                    {
                        var dirInfo = cont.FileInstance.parentBundle.file.bundleInf6.dirInf;
                        foreach (var dirInfo6 in dirInfo)
                        {
                            BundleFileInstance bun = cont.FileInstance.parentBundle;
                            if (dirInfo6.name == al.m_Source.Split('/')[2])
                            {
                                MemoryStream ms = new MemoryStream();
                                AssetsFileReader bundleReader = bun.file.reader;
                                bundleReader.Position = bun.file.bundleHeader6.GetFileDataOffset() + dirInfo6.offset;
                                bundleReader.BaseStream.CopyToCompat(ms, dirInfo6.decompressedSize);
                                rer = new ResourceReader(ms, al.m_Offset, al.m_Size);
                            }
                        }
                        var m_AudioData = rer.GetData();
                        buff = ConvertToWav(m_AudioData);
                        string name = baseField.Get("m_Name").GetValue().AsString();
                        byte[] byteData = buff;

                        name = Extensions.ReplaceInvalidPathChars(name);
                        string file = Path.Combine(dir, $"{name}-{Path.GetFileName(cont.FileInstance.path)}-{cont.PathId}.wav");

                        File.WriteAllBytes(file, byteData);
                    }
                }
                return true;
            }
            return false;
        }
        public async Task<bool> SingleExport(Window win, AssetWorkspace workspace, List<AssetContainer> selection)
        {
            AssetContainer cont = selection[0];

            SaveFileDialog sfd = new SaveFileDialog();
            byte[] buff = null;
            AssetTypeValueField baseField = workspace.GetBaseField(cont);
            {
                AudioLibrary al = new AudioLibrary();
                al.ReadAudioFields(al, baseField);
                ResourceReader rer = null;
                if (cont.FileInstance.parentBundle == null)
                {
                    //assets files not supported yet so return null;
                }
                else
                {
                    var dirInfo = cont.FileInstance.parentBundle.file.bundleInf6.dirInf;
                    foreach (var dirInfo6 in dirInfo)
                    {
                        BundleFileInstance bun = cont.FileInstance.parentBundle;
                        if (dirInfo6.name == al.m_Source.Split('/')[2])
                        {
                            MemoryStream ms = new MemoryStream();
                            AssetsFileReader bundleReader = bun.file.reader;
                            bundleReader.Position = bun.file.bundleHeader6.GetFileDataOffset() + dirInfo6.offset;
                            bundleReader.BaseStream.CopyToCompat(ms, dirInfo6.decompressedSize);
                            rer = new ResourceReader(ms, al.m_Offset, al.m_Size);
                        }
                    }
                    var m_AudioData = rer.GetData();
                    buff = ConvertToWav(m_AudioData);
                }
            }
            string name = baseField.Get("m_Name").GetValue().AsString();
            name = Extensions.ReplaceInvalidPathChars(name);

            sfd.Title = "Save WAV file";
            sfd.Filters = new List<FileDialogFilter>() {
                new FileDialogFilter() { Name = "WAV file", Extensions = new List<string>() { "wav" } }
            };
            sfd.InitialFileName = $"{name}-{Path.GetFileName(cont.FileInstance.path)}-{cont.PathId}.wav";

            string file = await sfd.ShowAsync(win);

            if (file != null && file != string.Empty)
            {
                byte[] byteData = buff;
                File.WriteAllBytes(file, byteData);

                return true;
            }
            return false;
        }


        public class TextAssetPlugin : UABEAPlugin
        {
            public PluginInfo Init()
            {
                PluginInfo info = new PluginInfo();
                info.name = "AudioClip Import/Export";

                info.options = new List<UABEAPluginOption>();
                info.options.Add(new ExportTextAssetOption());
                return info;
            }
        }
        private byte[] ConvertToWav(byte[] fmodData)
        {
            RESULT result = Factory.System_Create(out TexturePlugin.System system);
            if (result != RESULT.OK)
            {
                return null;
            }

            try
            {
                result = system.init(1, INITFLAGS.NORMAL, IntPtr.Zero);
                if (result != RESULT.OK)
                {
                    return null;
                }

                CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO();
                exinfo.cbsize = Marshal.SizeOf(exinfo);
                exinfo.length = (uint)fmodData.Length;
                result = system.createSound(fmodData, MODE.OPENMEMORY, ref exinfo, out Sound sound);
                if (result != RESULT.OK)
                {
                    return null;
                }

                try
                {
                    result = sound.getSubSound(0, out Sound subsound);
                    if (result != RESULT.OK)
                    {
                        return null;
                    }

                    try
                    {
                        result = subsound.getFormat(out SOUND_TYPE type, out SOUND_FORMAT format, out int numChannels, out int bitsPerSample);
                        if (result != RESULT.OK)
                        {
                            return null;
                        }

                        result = subsound.getDefaults(out float frequency, out int priority);
                        if (result != RESULT.OK)
                        {
                            return null;
                        }

                        int sampleRate = (int)frequency;
                        result = subsound.getLength(out uint length, TIMEUNIT.PCMBYTES);
                        if (result != RESULT.OK)
                        {
                            return null;
                        }

                        result = subsound.@lock(0, length, out IntPtr ptr1, out IntPtr ptr2, out uint len1, out uint len2);
                        if (result != RESULT.OK)
                        {
                            return null;
                        }

                        const int WavHeaderLength = 44;
                        int bufferLen = (int)(WavHeaderLength + len1);
                        byte[] buffer = new byte[bufferLen];
                        using (MemoryStream stream = new MemoryStream(buffer))
                        {
                            using BinaryWriter writer = new BinaryWriter(stream);
                            writer.Write(RiffFourCC);
                            writer.Write(36 + len1);
                            writer.Write(WaveEightCC);
                            writer.Write(16);
                            writer.Write((short)1);
                            writer.Write((short)numChannels);
                            writer.Write(sampleRate);
                            writer.Write(sampleRate * numChannels * bitsPerSample / 8);
                            writer.Write((short)(numChannels * bitsPerSample / 8));
                            writer.Write((short)bitsPerSample);
                            writer.Write(DataFourCC);
                            writer.Write(len1);
                        }
                        Marshal.Copy(ptr1, buffer, WavHeaderLength, (int)len1);
                        subsound.unlock(ptr1, ptr2, len1, len2);
                        return buffer;
                    }
                    finally
                    {
                        subsound.release();
                    }
                }
                finally
                {
                    sound.release();
                }
            }
            finally
            {
                system.release();
            }
        }
    }

}
