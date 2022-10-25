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
        TOCClass toc;

        private delegate void ResetProgressBarCB(int min, int max, int val);
        private delegate void UpdateProgressBarCB(int val);
        private delegate void UpdateActionLabelCB(string text);
        private delegate void ResetControlsCB(bool error, string errorText);
        private delegate int ModCB(int val);

        #region TOC class

        private class TOCClass : IComparer<TOCItemFil>, ICloneable
        {
            public List<TOCItemFil> fils;
            public int totalLen;
            public int dataStart;
            public int startIdx;
            public int endIdx;
            public int lastIdx;
            public int dirCount = 1;
            public int filCount = 4;

            public TOCClass(string resPath)
            {
                bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    OSPlatform.Windows
                );

                char folderPaths;
                if (isWindows) // Project will not build on UNIX without this
                {
                    folderPaths = '\\';
                }
                else
                {
                    folderPaths = '/';
                }
                fils = new List<TOCItemFil>();
                fils.Add(new TOCItemFil(0, 0, 0, 99999, true, "root", "", resPath));
                fils.Add(
                    new TOCItemFil(
                        1,
                        0,
                        0,
                        6,
                        true,
                        "&&SystemData",
                        "&&systemdata" + folderPaths,
                        resPath + "&&systemdata" + folderPaths
                    )
                );
                fils.Add(
                    new TOCItemFil(
                        2,
                        1,
                        0,
                        99999,
                        false,
                        "ISO.hdr",
                        "&&SystemData" + folderPaths + "iso.hdr",
                        resPath + "&&systemdata" + folderPaths + "ISO.hdr"
                    )
                );
                fils.Add(
                    new TOCItemFil(
                        3,
                        1,
                        9280,
                        99999,
                        false,
                        "AppLoader.ldr",
                        "&&SystemData" + folderPaths + "apploader.ldr",
                        resPath + "&&systemdata" + folderPaths + "AppLoader.ldr"
                    )
                );
                fils.Add(
                    new TOCItemFil(
                        4,
                        1,
                        0,
                        99999,
                        false,
                        "Start.dol",
                        "&&SystemData" + folderPaths + "start.dol",
                        resPath + "&&systemdata" + folderPaths + "Start.dol"
                    )
                );
                fils.Add(
                    new TOCItemFil(
                        5,
                        1,
                        0,
                        99999,
                        false,
                        "Game.toc",
                        "&&SystemData" + folderPaths + "game.toc",
                        resPath + "&&systemdata" + folderPaths + "Game.toc"
                    )
                );

                totalLen = 0;
                dataStart = totalLen;
                startIdx = totalLen;
            }

            #region IComparer<TOCItemFil> Members

            public int Compare(TOCItemFil x, TOCItemFil y)
            {
                if (x.pos > y.pos)
                    return 1;
                else if (x.pos < y.pos)
                    return -1;
                else
                    return 0;
            }

            #endregion

            #region ICloneable Members

            public object Clone()
            {
                TOCClass res;

                res = new TOCClass(this.fils[0].path);
                res.fils.Clear();
                res.dirCount = this.dirCount;
                res.filCount = this.filCount;
                for (int i = 0; i < this.fils.Count; i++)
                    res.fils.Add((TOCItemFil)this.fils[i].Clone());

                return res;
            }

            #endregion
        }

        private class TOCItemFil : ICloneable
        {
            public int TOCIdx;
            public int dirIdx;
            public int nextIdx;
            public int prevIdx;
            public int pos;
            public int len;
            public bool isDir;
            public string name;
            public string path;
            public string gamePath;

            public TOCItemFil() { }

            public TOCItemFil(
                int TOCIdx,
                int dirIdx,
                int pos,
                int len,
                bool isDir,
                string name,
                string gamePath,
                string path
            )
            {
                this.TOCIdx = TOCIdx;
                this.dirIdx = dirIdx;
                this.pos = pos;
                this.len = len;
                this.isDir = isDir;
                this.name = name;
                this.gamePath = gamePath;
                this.path = path;
            }

            public override string ToString()
            {
                return string.Format("pos: {0:d10} , path: '{1}'", pos, path);
            }

            #region ICloneable Members

            public object Clone()
            {
                return new TOCItemFil(
                    this.TOCIdx,
                    this.dirIdx,
                    this.pos,
                    this.len,
                    this.isDir,
                    this.name,
                    this.gamePath,
                    this.path
                );
            }

            #endregion
        }

        #endregion

        private bool ReadTOC()
        {
            TOCItemFil tif;
            sio.FileStream fsr;
            sio.BinaryReader brr;
            long prevPos,
                newPos;
            string tocName = "Game.toc";
            sio.DirectoryInfo di;
            sio.FileInfo fi;

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

            bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                OSPlatform.Windows
            );

            char folderPaths = '/';

            for (i = 2; i < 6; i++)
            {
                fi = new sio.FileInfo(toc.fils[i].path);
                if (!fi.Exists)
                {
                    Console.WriteLine(string.Format("File '{0}' not found", toc.fils[i].path));
                    error = true;
                }
                else
                {
                    toc.fils[i].len = (int)fi.Length;
                    toc.totalLen += toc.fils[i].len;
                }
            }
            toc.dataStart = toc.totalLen;

            if (!error)
            {
                fsr = new sio.FileStream(toc.fils[2].path, sio.FileMode.Open, sio.FileAccess.Read);
                brr = new sio.BinaryReader(fsr, ste.Default);

                fsr.Position = 0x1c;
                if (brr.ReadInt32() != 0x3d9f33c2)
                {
                    Console.WriteLine("Not a GameCube image");
                    error = true;
                }

                fsr.Position = 0x0400;
                toc.fils[2].pos = 0x0;
                toc.fils[3].pos = 0x2440;
                brr.ReadInt32BE();
                fsr.Position += 0x1c;
                toc.fils[4].pos = brr.ReadInt32BE();
                toc.fils[5].pos = brr.ReadInt32BE();
                brr.ReadInt32BE();
                fsr.Position += 0x08;
                toc.dataStart = brr.ReadInt32BE();

                brr.Close();
                fsr.Close();
            }

            if (!error)
            {
                fsr = new sio.FileStream(
                    resPath + "&&systemdata" + folderPaths + tocName,
                    sio.FileMode.Open,
                    sio.FileAccess.Read
                );
                brr = new sio.BinaryReader(fsr, ste.Default);

                i = brr.ReadInt32();
                if (i != 1)
                {
                    error = true;
                    Console.WriteLine(
                        "Multiple FST image?\r\nPlease mail me info about this image"
                    );
                }

                i = brr.ReadInt32();
                if (i != 0)
                {
                    error = true;
                    Console.WriteLine(
                        "Multiple FST image?\r\nPlease mail me info about this image"
                    );
                }

                namesTableEntryCount = brr.ReadInt32BE() - 1;
                namesTableStart = (namesTableEntryCount * 12) + 12;

                if (BackendClass.retrieveFilesInfo)
                {
                    mod = (int)Math.Floor((float)(namesTableEntryCount + itemNum));
                    if (mod == 0)
                    {
                        mod = 1;
                    }
                }

                for (int cnt = 0; cnt < namesTableEntryCount; cnt++)
                {
                    itemNamePtr = brr.ReadInt32BE();
                    if (itemNamePtr >> 0x18 == 1)
                        itemIsDir = true;
                    itemNamePtr &= 0x00ffffff;
                    itemPos = brr.ReadInt32BE();
                    itemLen = brr.ReadInt32BE();
                    prevPos = fsr.Position;
                    newPos = namesTableStart + itemNamePtr;
                    fsr.Position = newPos;
                    itemName = brr.ReadStringNT();
                    fsr.Position = prevPos;

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
                            itemPath = BackendClass.resPath + itemPath;
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

                    if (BackendClass.retrieveFilesInfo)
                    {
                        if (itemIsDir)
                        {
                            di = new sio.DirectoryInfo(itemPath);
                            if (!di.Exists)
                            {
                                errorText = string.Format("Directory '{0}' not found", itemPath);
                                error = true;
                            }
                        }
                        else
                        {
                            fi = new sio.FileInfo(itemPath);
                            if (!fi.Exists)
                            {
                                errorText = string.Format("File '{0}' not found", itemPath);
                                error = true;
                            }
                            else
                            {
                                itemLen = (int)fi.Length;
                                toc.totalLen += itemLen;
                            }
                        }
                        if (error)
                            break;

                        if (itemNum % mod == 0)
                        {
                            //sslblAction.Text = string.Format("Check: '{0}'…", itemPath.Replace(resPath, ""));
                        }
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
                brr.Close();
                fsr.Close();
            }

            if (error)
            {
                //sslblAction.Text = "Ready";
                return false;
                ;
            }

            CalcNextFileIds();

            //sslblAction.Text = "Building Structure…";
            //sslblAction.Text = "Ready";

            BackendClass.rootOpened = true;
            LoadInfo(!BackendClass.rootOpened);

            return error;
        }

        private bool GenerateTOC()
        {
            int itemNum = 0;
            int filePosDef = 1000000;
            int filePos = filePosDef;
            TOCItemFil tif;

            toc = new TOCClass(resPath);
            toc.fils.RemoveRange(1, 5);
            itemNum = toc.fils.Count;

            GetFilDirInfo(new sio.DirectoryInfo(resPath), ref itemNum, ref filePos);
            filePos = filePosDef;

            toc.fils[0].len -= 5;
            toc.fils[1].len -= 5;
            tif = toc.fils[2];
            toc.fils[2] = toc.fils[4];
            toc.fils[4] = tif;
            toc.fils[2].TOCIdx = 2;
            toc.fils[2].pos = filePos;
            tif = toc.fils[3];
            toc.fils[3] = toc.fils[4];
            toc.fils[4] = tif;
            toc.fils[3].TOCIdx = 3;
            toc.fils[3].pos = filePos + 2;
            tif = toc.fils[4];
            toc.fils[4] = toc.fils[5];
            toc.fils[5] = tif;
            toc.fils[4].TOCIdx = 4;
            toc.fils[4].pos = filePos + 4;
            toc.fils[5].TOCIdx = 5;
            toc.fils[5].pos = filePos + 6;

            CalcNextFileIds();

            toc.fils[2].pos = filePos;

            rootOpened = true;
            LoadInfo(!rootOpened);

            return true;
        }

        private void GetFilDirInfo(sio.DirectoryInfo pDir, ref int itemNum, ref int filePos)
        {
            sio.DirectoryInfo di;
            sio.DirectoryInfo[] dirs;
            sio.FileInfo[] fils;
            TOCItemFil tif;
            int tocDirIdx = itemNum - 1;

            dirs = pDir.GetDirectories();

            Array.Sort(
                dirs,
                delegate(sio.DirectoryInfo x, sio.DirectoryInfo y)
                {
                    return x.Name.CompareTo(y.Name);
                }
            );

            //IEnumerable<sio.DirectoryInfo> query = dirs.Where(dir => dir.Name.ToLower() == "&&systemdata");
            for (int cnt = 0; cnt < dirs.Length; cnt++)
                if (dirs[cnt].Name.ToLower() == "&&systemdata")
                {
                    di = dirs[0];
                    dirs[0] = dirs[cnt];
                    dirs[cnt] = di;
                    break;
                }

            for (int cnt = 0; cnt < dirs.Length; cnt++)
            {
                tif = new TOCItemFil(
                    itemNum,
                    tocDirIdx,
                    tocDirIdx,
                    0,
                    true,
                    dirs[cnt].Name,
                    dirs[cnt].FullName.Replace(resPath, ""),
                    dirs[cnt].FullName
                );
                toc.fils.Add(tif);
                itemNum += 1;
                toc.dirCount += 1;
                GetFilDirInfo(dirs[cnt], ref itemNum, ref filePos);
            }

            fils = pDir.GetFiles();
            Array.Sort(
                fils,
                delegate(sio.FileInfo x, sio.FileInfo y)
                {
                    return x.Name.CompareTo(y.Name);
                }
            );
            for (int cnt = 0; cnt < fils.Length; cnt++)
            {
                tif = new TOCItemFil(
                    itemNum,
                    tocDirIdx,
                    filePos,
                    (int)fils[cnt].Length,
                    false,
                    fils[cnt].Name,
                    fils[cnt].FullName.Replace(resPath, ""),
                    fils[cnt].FullName
                );
                toc.fils.Add(tif);
                toc.fils[0].len = toc.fils.Count;
                filePos += 2;
                itemNum += 1;
                toc.filCount += 1;
            }

            toc.fils[tocDirIdx].len = itemNum;
        }

        private void CalcNextFileIds()
        {
            TOCClass tocCopy;
            int[] idxs;
            int idx;
            int i,
                j;

            toc.fils[2].pos = toc.fils.Count + 1; //needed for sorting

            toc.fils[0].nextIdx = 1;
            toc.fils[1].nextIdx = 2;
            toc.fils[2].nextIdx = 3;
            toc.fils[3].nextIdx = 4;
            toc.fils[4].nextIdx = 5;
            toc.fils[5].nextIdx = 6;
            ////idx = toc.fils[5].nextIdx;
            //bgr = 0;
            //idx = 0;

            tocCopy = (TOCClass)toc.Clone();
            tocCopy.fils.Sort((IComparer<TOCItemFil>)tocCopy);
            toc.fils[2].pos = 0;

            idxs = new int[tocCopy.filCount];
            idx = 0;
            for (i = 0; i < tocCopy.fils.Count - 1; i++)
                if (!tocCopy.fils[i].isDir)
                {
                    idxs[idx] = tocCopy.fils[i + 1].TOCIdx;
                    idx += 1;
                }

            idx = 0;
            for (i = 0; i < toc.fils.Count; i++)
                if (!toc.fils[i].isDir)
                {
                    toc.fils[i].nextIdx = idxs[idx];
                    idx += 1;
                    if (idx == tocCopy.filCount)
                    {
                        toc.endIdx = idxs[idx - 2];
                        toc.lastIdx = i;
                    }
                }

            idx = toc.fils[2].nextIdx;
            for (i = 0; i < toc.fils.Count - 1; i++)
                if (!toc.fils[i].isDir)
                {
                    toc.fils[idx].prevIdx = i;
                    for (j = i + 1; j < toc.fils.Count - 1; j++)
                        if (!toc.fils[j].isDir)
                            break;
                        else
                            i += 1;
                    idx = toc.fils[i + 1].nextIdx;
                }

            toc.fils[toc.fils.Count - 1].nextIdx = toc.fils.Count - 1;

            //for (i = 5; i < toc.fils.Count; i++)
            //    if (!toc.fils[i].isDir)
            //    {
            //        if (startIdxNotFound)
            //            if (i > 5)
            //            {
            //                toc.startIdx = i;
            //                startIdxNotFound = false;
            //            }
            //        egr = int.MaxValue;
            //        for (j = 6; j < toc.fils.Count; j++)
            //            if (!toc.fils[j].isDir)
            //            {
            //                if (toc.fils[j].pos >= bgr)
            //                    if (toc.fils[j].pos <= egr)
            //                    {
            //                        idx = j;
            //                        egr = toc.fils[j].pos;
            //                    }
            //            }
            //        toc.fils[i].nextIdx = idx;
            //        //bgr = egr + toc.fils[idx].len;
            //        bgr = egr + 1;
            //    }
        }

        public void Rebuild(string path)
        {
            imgPath = path;
            Rebuild();
        }

        private void Rebuild()
        {
            sio.FileStream fsw = null;
            sio.BinaryWriter bww = null;
            sio.FileStream fsr = null;
            sio.BinaryReader brr = null;
            sio.BinaryWriter brw = null;
            sio.MemoryStream ms = null;
            sio.BinaryReader br = null;
            sio.BinaryWriter bw = null;
            sio.FileInfo fi;
            int dataLen;
            bool dataStartPositioned = true;
            byte[] tb,
                b;
            string tocPath;
            int mod,
                modAct;
            int bytesWritten;
            int maxBR,
                curBR,
                temBR;
            bool error = false;
            string errorText = "";
            int idx;
            int i,
                j;

            int m;
            ModCB Mod = delegate(int val)
            {
                m = val % filesMod;
                if (m == 0)
                    return val;
                else
                    return val + (filesMod - m);
            };

            try
            {
                fsw = new sio.FileStream(
                    imgPath,
                    sio.FileMode.Create,
                    sio.FileAccess.Write,
                    sio.FileShare.None
                );
                bww = new sio.BinaryWriter(fsw, ste.Default);

                fsr = new sio.FileStream(
                    toc.fils[2].path,
                    sio.FileMode.Open,
                    sio.FileAccess.ReadWrite,
                    sio.FileShare.None
                );
                brr = new sio.BinaryReader(fsr, ste.Default);
                ms = new sio.MemoryStream(brr.ReadBytes((int)fsr.Length), true);
                br = new sio.BinaryReader(ms, ste.Default);
                bw = new sio.BinaryWriter(ms, ste.Default);

                ms.Position = 0x400;
                toc.fils[2].pos = 0;
                toc.fils[3].pos = 0x2440;
                bw.WriteInt32BE(toc.fils[3].len); //ldr len
                ms.Position += 0x1c;
                i = Mod(0x2440 + toc.fils[3].len);
                bw.WriteInt32BE(i); //dol start
                toc.fils[4].pos = i;
                i = Mod(i + toc.fils[4].len);
                bw.WriteInt32BE(i); //toc start
                tocPath = toc.fils[5].path;
                toc.fils[5].path = sio.Path.GetTempPath() + "game.toc";
                toc.fils[5].pos = i;
                toc.dataStart = 0;
                tb = ReGenTOC(
                    toc.fils[5].pos,
                    out toc.fils[5].len,
                    ref toc.dataStart,
                    out toc.totalLen
                );
                if (appendImage)
                {
                    dataLen = toc.totalLen - toc.dataStart;
                    toc.dataStart = maxImageSize - dataLen;
                    tb = ReGenTOC(
                        toc.fils[5].pos,
                        out toc.fils[5].len,
                        ref toc.dataStart,
                        out toc.totalLen
                    );
                }
                bw.WriteInt32BE(toc.fils[5].len); //toc len
                bw.WriteInt32BE(toc.fils[5].len); //total toc len
                ms.Position += 0x04;
                bw.WriteInt32BE(toc.dataStart); //data start
                ms.WriteTo(fsw);
                brr.Close();
                fsr.Close();
                br.Close();
                bw.Close();
                ms.Close();
                brr.Close();
                fsr.Close();

                bytesWritten = (int)fsw.Position;
                //if (resPath.Substring(0, 2).ToLower() == imgPath.Substring(0, 2).ToLower())
                //    maxBR = 0x0800000;
                //else
                maxBR = 0x8000;
                curBR = maxBR - bytesWritten;

                if (toc.totalLen > maxImageSize || toc.totalLen < 0)
                {
                    errorText = "The resulting image is too large";
                    error = true;
                }

                idx = 3;

                if (!error)
                    for (i = 3; i < toc.fils.Count; i++)
                    {
                        if (!addressRebuild)
                            idx = i;

                        //Console.WriteLine(toc.fils[i].path);
                        if (!toc.fils[i].isDir)
                        {
                            fi = new sio.FileInfo(toc.fils[idx].path);
                            if (!fi.Exists)
                            {
                                errorText = string.Format("File '{0}' not found", fi.FullName);
                                error = true;
                                break;
                            }
                            fsr = new sio.FileStream(
                                toc.fils[idx].path,
                                sio.FileMode.Open,
                                sio.FileAccess.Read,
                                sio.FileShare.Read
                            );
                            brr = new sio.BinaryReader(fsr, ste.Default);

                            if (!dataStartPositioned)
                            {
                                m = filesMod - ((int)fsw.Position % filesMod);
                                for (j = 0; j < m; j++)
                                    bww.Write((byte)0);
                                bytesWritten += maxBR - (bytesWritten % maxBR);
                                m = (int)fsw.Position;
                                b = new byte[maxBR];
                                for (j = m; j < toc.dataStart; j += maxBR)
                                {
                                    fsw.Write(b, 0, maxBR);
                                    bytesWritten += maxBR;

                                    if (escapePressed)
                                        stopCurrProc = true;

                                    if (stopCurrProc)
                                        break;
                                }
                                if (!stopCurrProc)
                                {
                                    fsw.Write(b, 0, toc.dataStart % maxBR);
                                    curBR = maxBR;
                                    bytesWritten =
                                        toc.dataStart + (maxBR - (toc.dataStart % maxBR)) - maxBR;
                                    fsw.Position = toc.dataStart;
                                    dataStartPositioned = true;
                                }
                            }

                            if (stopCurrProc)
                                break;

                            if (fsw.Position != toc.fils[idx].pos)
                            {
                                m = toc.fils[idx].pos - (int)fsw.Position;
                                for (j = 0; j < m; j++)
                                    bww.Write((byte)0);
                                curBR -= m;
                                bytesWritten += m;
                            }

                            if (curBR < 0)
                            {
                                errorText = "Oooopps)\r\nPlease mail me info about this image";
                                error = true;
                                break;
                            }

                            while (fsr.Position < fsr.Length)
                            {
                                b = brr.ReadBytes(curBR);
                                temBR = b.Length;
                                bytesWritten += temBR;
                                if (temBR == curBR)
                                {
                                    curBR = maxBR;
                                }
                                else
                                    curBR -= temBR;
                                bww.Write(b);

                                if (escapePressed)
                                    stopCurrProc = true;
                            }

                            brr.Close();
                            fsr.Close();

                            if (stopCurrProc)
                                break;

                            if (addressRebuild)
                                idx = toc.fils[i].nextIdx;

                            if (i == 5)
                                dataStartPositioned = false;
                        }
                    }

                m = filesMod - ((int)fsw.Position % filesMod);
                for (i = 0; i < m; i++)
                    bww.Write((byte)0);

                fi = new sio.FileInfo(toc.fils[5].path);
                if (fi.Exists)
                {
                    sio.File.Delete(toc.fils[5].path);
                    toc.fils[5].path = tocPath;
                }
            }
            catch (Exception ex)
            {
                error = true;
                errorText = ex.Message;
                Console.WriteLine(errorText);
            }
            if (!error && !stopCurrProc)
                if (appendImage)
                    fsw.SetLength(maxImageSize); //1459978240

            if (bww != null)
                bww.Close();
            if (fsw != null)
                fsw.Close();
            if (brr != null)
                brr.Close();
            if (brw != null)
                brw.Close();
            if (fsr != null)
                fsr.Close();
            if (br != null)
                br.Close();
            if (bw != null)
                bw.Close();
            if (ms != null)
                ms.Close();

            isRebuilding = false;
            stopCurrProc = false;
        }

        private byte[] ReGenTOC(int tocStart, out int tocLen, ref int dataStart, out int totalLen)
        {
            int m;
            ModCB Mod = delegate(int val)
            {
                m = val % filesMod;
                if (m == 0)
                    return val;
                else
                    return val + (filesMod - m);
            };

            sio.MemoryStream ms;
            sio.BinaryReader br;
            sio.BinaryWriter bw;
            sio.FileStream fs;
            byte[] tb = new byte[0x40000];
            byte[] res;
            long pos,
                newPos;
            int rawPos;
            int itemNum,
                shift;
            int idx;
            int[] poses = new int[0x8000];

            int namesTableEntryCount;
            int namesTableStart;
            int itemNamePtr;
            int itemPos;
            int itemLen;
            string itemName;

            ms = new sio.MemoryStream(tb, true);
            br = new sio.BinaryReader(ms, ste.Default);
            bw = new sio.BinaryWriter(ms, ste.Default);

            itemLen = 0;
            for (int i = 6; i < toc.fils.Count; i++)
                itemLen += toc.fils[i].name.Length + 1;

            itemNum = 6;
            shift = itemNum - 1;

            namesTableEntryCount = toc.fils.Count - shift;
            namesTableStart = (namesTableEntryCount * 12);
            tocLen = namesTableStart + itemLen;
            if (dataStart == 0)
                dataStart = Mod(tocStart + tocLen);
            itemNamePtr = 0;

            bw.Write(1);
            bw.Write(0);
            bw.WriteInt32BE(namesTableEntryCount);
            pos = ms.Position;
            rawPos = dataStart;

            idx = toc.fils[5].nextIdx;

            if (!addressRebuild)
            {
                for (int i = 6; i < toc.fils.Count; i++)
                    if (!toc.fils[i].isDir)
                    {
                        itemLen = toc.fils[i].len;
                        poses[i] = rawPos;
                        rawPos = Mod(rawPos + itemLen);
                    }
            }
            else
                for (int i = 6; i < toc.fils.Count; i++)
                    if (!toc.fils[i].isDir)
                    {
                        itemLen = toc.fils[idx].len;
                        poses[idx] = rawPos;
                        rawPos = Mod(rawPos + itemLen);
                        idx = toc.fils[i].nextIdx;
                    }

            for (int i = 6; i < toc.fils.Count; i++)
            {
                ms.Position = pos;
                if (toc.fils[i].isDir)
                {
                    itemNamePtr = (itemNamePtr & 0xffffff) | 0x01000000;
                    itemPos = (toc.fils[i].pos > 0) ? toc.fils[i].pos - shift : toc.fils[i].pos;
                    itemLen = toc.fils[i].len - shift;
                    itemName = toc.fils[i].name;
                }
                else
                {
                    itemNamePtr = (itemNamePtr & 0xffffff);
                    itemPos = poses[i];
                    toc.fils[i].pos = itemPos;
                    itemLen = toc.fils[i].len;
                    itemName = toc.fils[i].name;
                }
                bw.WriteInt32BE(itemNamePtr);
                bw.WriteInt32BE(itemPos);
                bw.WriteInt32BE(itemLen);
                pos = ms.Position;
                newPos = (itemNamePtr & 0xffffff) + namesTableStart;
                ms.Position = newPos;
                bw.WriteStringNT(itemName);
                itemNamePtr += itemName.Length + 1;
            }

            totalLen = rawPos;
            res = new byte[tocLen];
            Array.Copy(tb, res, tocLen);
            fs = new sio.FileStream(
                toc.fils[5].path,
                sio.FileMode.Create,
                sio.FileAccess.Write,
                sio.FileShare.None
            );
            fs.Write(res, 0, tocLen);
            fs.Close();
            return res;
        }
    }

    #region SIOExtensions

    public static class SIOExtensions
    {
        private static int resI;
        private static int resH;
        private static string resS;
        private static int i;
        private static byte b;
        private static byte[] bb;

        public static int ReadInt32BE(this sio.BinaryReader br)
        {
            i = br.ReadByte();
            resI = i << 0x18;
            i = br.ReadByte();
            resI += i << 0x10;
            i = br.ReadByte();
            resI += i << 0x08;
            i = br.ReadByte();
            resI += i;

            return resI;
        }

        public static void WriteInt32BE(this sio.BinaryWriter bw, int val)
        {
            b = (byte)((val >> 0x18) & 0xff);
            bw.Write(b);
            b = (byte)((val >> 0x10) & 0xff);
            bw.Write(b);
            b = (byte)((val >> 0x08) & 0xff);
            bw.Write(b);
            b = (byte)(val & 0xff);
            bw.Write(b);
        }

        public static int ReadInt16BE(this sio.BinaryReader br)
        {
            i = br.ReadByte();
            resH = i << 0x08;
            i = br.ReadByte();
            resH += i;

            return resH;
        }

        public static void WriteInt16BE(this sio.BinaryWriter bw, int val)
        {
            b = (byte)((val >> 0x08) & 0xff);
            bw.Write(b);
            b = (byte)(val & 0xff);
            bw.Write(b);
        }

        public static string ReadStringNT(this sio.BinaryReader br)
        {
            resS = "";
            b = br.ReadByte();

            while (b != 0)
            {
                resS += ste.Default.GetChars(new byte[] { b })[0];
                b = br.ReadByte();
            }

            return resS;
        }

        public static void WriteStringNT(this sio.BinaryWriter bw, string s)
        {
            resI = s.Length;

            for (i = 0; i < resI; i++)
            {
                b = ste.Default.GetBytes(new char[] { s[i] })[0];
                bw.Write(b);
            }
            bw.Write((byte)0);
        }

        public static void WriteStringNT(
            this sio.BinaryWriter bw,
            Encoding enc,
            string s,
            int maxLen
        )
        {
            bb = enc.GetBytes(s.Replace("\r\n", "\n"));
            resI = bb.Length;

            for (i = 0; i < resI; i++)
            {
                bw.Write(bb[i]);
                if (i == maxLen - 1)
                {
                    resI = i;
                    break;
                }
            }

            for (i = resI; i < maxLen; i++)
                bw.Write((byte)0);
        }

        public static string ToStringC(char[] chars)
        {
            resS = "";
            resH = chars.Length;

            for (int resI = 0; resI < resH; resI++)
            {
                if (chars[resI] == '\n')
                    resS += '\r';
                resS += chars[resI];
            }

            return resS;
        }
    }
}
#endregion
