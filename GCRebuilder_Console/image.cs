using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using sio = System.IO;
using ste = System.Text.Encoding;

namespace GCRebuilder_Console
{
    public partial class BackendClass
    {
        private int expImpIdx;
        private string expImpPath;

        private delegate string ShowMTFolderDialogCB(string path);

        private bool ReadImageTOC()
        {
            TOCItemFil tif;
            sio.FileStream fsr;
            sio.BinaryReader brr;
            sio.MemoryStream msr;
            sio.BinaryReader mbr;
            long prevPos,
                newPos;

            int namesTableEntryCount;
            int namesTableStart;
            int itemNamePtr;
            bool itemIsDir = false;
            int itemPos;
            int itemLen;
            string itemName;
            string itemGamePath = "";
            string itemPath;

            int itemNum;
            int shift;
            //int dirIdx = 0;
            //int endIdx = 999999;
            int[] dirEntry = new int[512];
            int dirEntryCount = 0;
            dirEntry[1] = 99999999;

            int mod = 1;
            bool error = false;
            string errorText = "";
            int i,
                j;

            toc = new TOCClass(resPath);
            itemNum = toc.fils.Count;
            shift = toc.fils.Count - 1;

            fsr = new sio.FileStream(
                imgPath,
                sio.FileMode.Open,
                sio.FileAccess.Read,
                sio.FileShare.Read
            );
            brr = new sio.BinaryReader(fsr, ste.Default);

            bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                OSPlatform.Windows
            );

            char folderPaths;
            if (isWindows) //Project will not build on UNIX without this check.
            {
                folderPaths = '\\';
            }
            else
            {
                folderPaths = '/';
            }

            if (fsr.Length > 0x0438)
            {
                fsr.Position = 0x0400;
                toc.fils[2].pos = 0x0;
                toc.fils[2].len = 0x2440;
                toc.fils[3].pos = 0x2440;
                toc.fils[3].len = brr.ReadInt32BE();
                fsr.Position += 0x1c;
                toc.fils[4].pos = brr.ReadInt32BE();
                toc.fils[5].pos = brr.ReadInt32BE();
                toc.fils[5].len = brr.ReadInt32BE();
                toc.fils[4].len = toc.fils[5].pos - toc.fils[4].pos;
                fsr.Position += 0x08;
                toc.dataStart = brr.ReadInt32BE();

                toc.totalLen = (int)fsr.Length;
            }
            else
            {
                errorText = "Image is too short";
                error = true;
            }

            if (fsr.Length < toc.dataStart)
            {
                errorText = "Image is too short";
                error = true;
            }

            if (!error)
            {
                fsr.Position = toc.fils[5].pos;
                msr = new sio.MemoryStream(brr.ReadBytes(toc.fils[5].len));
                mbr = new sio.BinaryReader(msr, ste.Default);

                i = mbr.ReadInt32();
                if (i != 1)
                {
                    error = true;
                    errorText = "Multiple FST image?\r\nPlease mail me info about this image";
                }

                i = mbr.ReadInt32();
                if (i != 0)
                {
                    error = true;
                    errorText = "Multiple FST image?\r\nPlease mail me info about this image";
                }

                namesTableEntryCount = mbr.ReadInt32BE() - 1;
                namesTableStart = (namesTableEntryCount * 12) + 12;

                for (int cnt = 0; cnt < namesTableEntryCount; cnt++)
                {
                    itemNamePtr = mbr.ReadInt32BE();
                    if (itemNamePtr >> 0x18 == 1)
                        itemIsDir = true;
                    itemNamePtr &= 0x00ffffff;
                    itemPos = mbr.ReadInt32BE();
                    itemLen = mbr.ReadInt32BE();
                    prevPos = msr.Position;
                    newPos = namesTableStart + itemNamePtr;
                    msr.Position = newPos;
                    itemName = mbr.ReadStringNT();
                    msr.Position = prevPos;

                    while (dirEntry[dirEntryCount + 1] <= itemNum)
                        dirEntryCount -= 2;

                    if (itemIsDir)
                    {
                        dirEntryCount += 2;
                        dirEntry[dirEntryCount] = (itemPos > 0) ? itemPos + shift : itemPos;
                        itemPos += shift;
                        itemLen += shift;
                        dirEntry[dirEntryCount + 1] = itemLen;
                        toc.dirCount += 1;
                    }
                    else
                        toc.filCount += 1;

                    itemPath = itemName;
                    j = dirEntry[dirEntryCount];
                    for (i = 0; i < 256; i++)
                    {
                        if (j == 0)
                        {
                            itemGamePath = itemPath;
                            itemPath = resPath + itemPath;
                            break;
                        }
                        else
                        {
                            itemPath = itemPath.Insert(0, toc.fils[j].name + folderPaths);
                            j = toc.fils[j].dirIdx;
                        }
                    }
                    if (itemIsDir)
                        itemPath += folderPaths;

                    if (retrieveFilesInfo)
                    {
                        if (!itemIsDir)
                            if (fsr.Length < itemPos + itemLen)
                            {
                                errorText = string.Format("File '{0}' not found", itemPath);
                                error = true;
                            }

                        if (error)
                            break;
                    }

                    tif = new TOCItemFil(
                        itemNum,
                        dirEntry[dirEntryCount],
                        itemPos,
                        itemLen,
                        itemIsDir,
                        itemName,
                        itemGamePath,
                        itemPath
                    );
                    toc.fils.Add(tif);
                    toc.fils[0].len = toc.fils.Count;

                    if (itemIsDir)
                    {
                        dirEntry[dirEntryCount] = itemNum;
                        itemIsDir = false;
                    }

                    itemNum += 1;
                }
                mbr.Close();
                msr.Close();
            }

            brr.Close();
            fsr.Close();

            if (error)
            {
                //sslblAction.Text = "Ready";
                return false;
            }

            CalcNextFileIds();

            ////sslblAction.Text = "Building Structure…";
            ////sslblAction.Text = "Ready";

            rootOpened = false;
            LoadInfo(!rootOpened);

            //toc.fils.Sort((IComparer<TOCItemFil>)toc);

            return error;
        }

        public void Export(string expPath)
        {
            expImpPath = expPath;
            //expImpIdx = Convert.ToInt32(selNode.Name);
            if (toc.fils[expImpIdx].isDir)
                ExportDir();
            else
                Export(expImpIdx, expPath);
        }

        private void ExportDir()
        {
            //FolderBrowserDialog fbd;
            sio.DirectoryInfo di;
            int idx = expImpIdx;
            string expPath = expImpPath;
            string excPath;
            int[] dirLens = new int[256];
            string[] dirNames = new string[256];
            int dirIdx = 1;
            bool showMsg = false;
            bool error = false;
            string errorText = "";
            int i;

            bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                OSPlatform.Windows
            );

            char folderPaths;
            if (isWindows) // Project will not build on UNIX without this check
            {
                folderPaths = '\\';
            }
            else
            {
                folderPaths = '/';
            }

            if (expPath.Length == 0)
            {
                error = true;
                errorText = "";
            }

            if (!error)
            {
                expPath =
                    (expPath[expPath.Length - 1] == folderPaths) ? expPath : expPath + folderPaths;
                
                Console.WriteLine(expPath);
                Console.WriteLine(folderPaths);

                if (idx == 0)
                {
                    expPath += "root";
                    excPath = "root:";
                }
                else
                {
                    i = toc.fils[idx].gamePath.LastIndexOf(
                        folderPaths,
                        toc.fils[idx].gamePath.Length - 2
                    );
                    if (i < 0)
                        excPath = "root:";
                    else
                        excPath = "root:" + folderPaths + toc.fils[idx].gamePath.Substring(0, i);
                }

                dirLens[dirIdx] = -1;

                for (i = idx; i < toc.fils[idx].len; i++)
                {
                    while (i == dirLens[dirIdx])
                        dirIdx -= 1;

                    if (toc.fils[i].isDir)
                    {
                        di = new sio.DirectoryInfo(
                            expPath
                                + (
                                    ("root:" + folderPaths + toc.fils[i].gamePath).Replace(
                                        excPath,
                                        ""
                                    )
                                )
                        );
                        if (!di.Exists)
                            di.Create();
                        dirIdx += 1;
                        dirLens[dirIdx] = toc.fils[i].len;
                        dirNames[dirIdx] =
                            (di.FullName[di.FullName.Length - 1] == folderPaths)
                                ? di.FullName
                                : di.FullName + folderPaths;
                    }
                    else
                        Export(i, dirNames[dirIdx] + toc.fils[i].name);

                    if (stopCurrProc)
                        break;
                }
            }

            if (!showMsg)
                errorText = null;

            isImpExping = false;
            stopCurrProc = false;
        }

        private void Export(int idx, string expPath)
        {
            sio.FileStream fsr;
            sio.BinaryReader brr;
            sio.FileStream fsw;
            sio.BinaryWriter bww;
            long endPos;
            int maxBR,
                curBR,
                temBR;
            bool showMsg = false;
            byte[] b;

            escapePressed = false;

            if (expPath.Length == 0)
                return;

            fsr = new sio.FileStream(
                imgPath,
                sio.FileMode.Open,
                sio.FileAccess.Read,
                sio.FileShare.Read
            );
            brr = new sio.BinaryReader(fsr, ste.Default);
            fsw = new sio.FileStream(
                expPath,
                sio.FileMode.Create,
                sio.FileAccess.Write,
                sio.FileShare.None
            );
            bww = new sio.BinaryWriter(fsw, ste.Default);

            fsr.Position = toc.fils[idx].pos;
            endPos = toc.fils[idx].pos + toc.fils[idx].len;

            maxBR = 0x8000;
            temBR = toc.fils[idx].len;

            while (fsr.Position < endPos)
            {
                temBR -= maxBR;
                if (temBR < 0)
                    curBR = maxBR + temBR;
                else
                    curBR = maxBR;
                b = brr.ReadBytes(curBR);
                bww.Write(b);

                if (stopCurrProc)
                    break;
            }

            bww.Close();
            fsw.Close();
            brr.Close();
            fsr.Close();
        }

        private void ImportDir(int idx, string impPath) { }

        public void Import(string impPath)
        {
            Import(0, impPath);
        }

        private void Import(int idx, string impPath)
        {
            sio.FileInfo fi;
            sio.FileStream fsr;
            sio.BinaryReader brr;
            sio.FileStream fsw;
            sio.BinaryWriter bww;
            int oidx,
                nidx;
            int maxLen;
            long endPos;
            int maxBR,
                curBR,
                temBR;
            bool showMsg = false;
            byte[] b;

            escapePressed = false;

            if (impPath.Length == 0)
                return;

            fi = new sio.FileInfo(impPath);
            oidx = toc.fils[idx].prevIdx;
            for (nidx = oidx + 1; nidx < toc.fils.Count - 1; nidx++)
                if (!toc.fils[nidx].isDir)
                    break;
            maxLen = toc.fils[toc.fils[nidx].nextIdx].pos;
            maxLen =
                (nidx == toc.lastIdx)
                    ? toc.totalLen - toc.fils[idx].pos
                    : maxLen - toc.fils[idx].pos;
            endPos = toc.fils[idx].pos + maxLen;
            if (fi.Length > maxLen)
            {
                return;
            }

            fsr = new sio.FileStream(
                impPath,
                sio.FileMode.Open,
                sio.FileAccess.Read,
                sio.FileShare.Read
            );
            brr = new sio.BinaryReader(fsr, ste.Default);
            fsw = new sio.FileStream(
                imgPath,
                sio.FileMode.Open,
                sio.FileAccess.Write,
                sio.FileShare.None
            );
            bww = new sio.BinaryWriter(fsw, ste.Default);

            fsw.Position = toc.fils[idx].pos;

            maxBR = 0x8000;
            temBR = (int)fi.Length;

            while (true)
            {
                temBR -= maxBR;
                if (temBR < 0)
                    curBR = maxBR + temBR;
                else
                    curBR = maxBR;
                if (curBR < 0)
                    break;
                b = brr.ReadBytes(curBR);
                bww.Write(b);

                if (escapePressed)
                    stopCurrProc = true;

                if (stopCurrProc)
                    break;
            }

            if (!stopCurrProc)
            {
                while (fsw.Position < endPos)
                    bww.Write((byte)0);

                toc.fils[idx].len = (int)fi.Length;

                if (updateImageTOC)
                {
                    idx -= 5;
                    fsw.Position = toc.fils[5].pos + (idx << 3) + (idx << 2) + 8;
                    bww.WriteInt32BE((int)fsr.Length);
                }
            }

            bww.Close();
            fsw.Close();
            brr.Close();
            fsr.Close();
        }

        //private void SaveTOC()
        //{
        //    sio.FileStream fs;
        //    sio.StreamWriter sw;

        //    fs = new sio.FileStream(@"c:\temp.txt", sio.FileMode.Create, sio.FileAccess.Write);
        //    sw = new sio.StreamWriter(fs, ste.Default);

        //    for (int i = 0; i < toc.fils.Count; i++)
        //        if (!toc.fils[i].isDir)
        //            sw.WriteLine("{0:d10} ; {1:d10} ; '{2}'", toc.fils[i].pos, toc.fils[i].nextIdx, toc.fils[i].path);

        //    sw.Close();
        //    fs.Close();
        //}
    }
}
