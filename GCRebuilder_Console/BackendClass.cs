using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using sio = System.IO;
using ste = System.Text.Encoding;

namespace GCRebuilder_Console
{
    public partial class BackendClass
    {
        public static string resPath = "";
        public string imgPath = "";

        public static bool resChecked = false;
        public bool imgChecked = false;
        public static bool rootOpened = true;

        //debug options----------------------------------------------------------------------
        public static bool retrieveFilesInfo = true;
        public bool appendImage = true;
        public bool addressRebuild = true;
        public bool ignoreBannerAlpha = true;
        public bool updateImageTOC = true;
        public bool canEditTOC = false;
        public int filesMod = 2048;
        public int maxImageSize = 1459978240;

        //debug options----------------------------------------------------------------------

        public bool isRebuilding = false;
        public bool isWipeing = false;
        public bool isImpExping = false;
        public bool stopCurrProc = false;
        public bool escapePressed = false;
        public bool loading = true;
        public bool bannerLoaded = false;
        public bool fileNameSort = true;
        public char region = 'n';

        System.Threading.ThreadStart ths;
        System.Threading.Thread th;

        private string[] args;
        private bool showLastDialog;

        public BackendClass(string[] args)
        {
            this.args = args;

            loading = false;
        }

        public static bool IsImagePath(string arg)
        {
            sio.FileInfo fi;

            fi = new sio.FileInfo(arg);

            if (fi.Exists)
                return true;
            return false;
        }

        public void ImageOpen(string path)
        {

            if (path.Length == 0)
                return;

            imgPath = path;

            if (CheckImage())
                if (ReadImageTOC())
                {
                    rootOpened = false;
                }
        }

        private bool CheckImage()
        {
            sio.FileStream fs = null;
            sio.BinaryReader br = null;
            bool error = false;

            try
            {
                fs = new sio.FileStream(
                    imgPath,
                    sio.FileMode.Open,
                    sio.FileAccess.Read,
                    sio.FileShare.Read
                );
                br = new sio.BinaryReader(fs, ste.Default);

                fs.Position = 0x1c;
                if (br.ReadInt32() != 0x3d9f33c2)
                {
                    Console.WriteLine("Not a GameCube image");
                    error = true;
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                error = true;
            }
            finally
            {
                if (br != null)
                    br.Close();
                if (fs != null)
                    fs.Close();
            }

            return !error;
        }

        
        public bool IsRootPath(string arg)
        {
            sio.DirectoryInfo di;

            di = new sio.DirectoryInfo(arg);

            if (di.Exists)
                return true;
            return false;
        }

        public void RootOpen(string path, bool useTOC)
        {
            string PrevPath = resPath;
            bool success = false;
            bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                OSPlatform.Windows
            );

            char folderPaths;
            if (isWindows) // Project cannot find files without this check.
            {
                folderPaths = '\\';
            }
            else
            {
                folderPaths = '/';
            }


            if (path.Length == 0)
                return;

            resPath = path;

            if (resPath.LastIndexOf(folderPaths) != resPath.Length - 1)
                resPath += folderPaths;

            success = CheckResPath(useTOC);
            if (success)
                if (useTOC)
                    success = GenerateTOC();
                else
                    if (success)
                    success = ReadTOC();

            if (success)
            {
                rootOpened = true;
                //miRename.Visible = true;
                resChecked = true;
            }
            else
                resPath = PrevPath;
        }

        private bool CheckResPath( bool useTOC)
        {
            sio.DirectoryInfo di;
            sio.FileInfo[] fis;
            string sysDir = "&&systemdata";
            string[] sysFiles = new string[] { "apploader.ldr", "game.toc", "iso.hdr", "start.dol" };
            int i, j;

            try
            {
                di = new sio.DirectoryInfo(resPath + sysDir);
                if (!di.Exists)
                {
                    Console.WriteLine(string.Format("Folder '{0}' not found", sysDir));
                    return false;
                }

                fis = di.GetFiles();
                for (i = 0; i < sysFiles.Length; i++)
                {
                    for (j = 0; j < fis.Length; j++)
                        if (sysFiles[i] == fis[j].Name.ToLower())
                            break;
                    if (j == fis.Length)
                    {
                        Console.WriteLine(string.Format("File '{0}' not found in '{1}' folder", sysFiles[i], sysDir));
                        return false;
                    }
                }

                if (useTOC)
                {
                    if (fis.Length > 4)
                    {
                        Console.WriteLine(string.Format("Misc files are not allowed in '{0}' folder", sysDir));
                        return false;
                    }
                    if (di.GetDirectories().Length > 0)
                    {
                        Console.WriteLine(string.Format("Subfolders are not allowed in '{0}' folder", sysDir));
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
    }
}
