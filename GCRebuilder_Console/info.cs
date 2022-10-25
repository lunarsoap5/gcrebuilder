using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;

using sio = System.IO;
using ste = System.Text.Encoding;

namespace GCRebuilder_Console
{

    public partial class BackendClass
    {
        sio.MemoryStream bnr = null;
        sio.BinaryReader bnrr = null;
        sio.BinaryWriter bnrw = null;
        Encoding bannerEnc;


        private void LoadInfo(bool image)
        {
            LoadISOInfo(image);

            GetBanners(image);
        }

        private void LoadISOInfo(bool image)
        {
            sio.FileStream fs;
            sio.BinaryReader br;
            string loadPath;
            byte b;
            byte[] bb;

            loadPath = (image) ? imgPath : toc.fils[2].path;

            fs = new sio.FileStream(loadPath, sio.FileMode.Open, sio.FileAccess.Read, sio.FileShare.Read);
            br = new sio.BinaryReader(fs, ste.Default);

            bb = br.ReadBytes(4);
            switch (Convert.ToString(ste.Default.GetChars(new byte[] { bb[3] })[0]).ToLower())
            {
                case "e":
                    region = 'u';
                    break;
                case "j":
                    region = 'j';
                    break;
                case "p":
                    region = 'e';
                    break;
                default:
                    region = 'n';
                    break;
            }
            bb = br.ReadBytes(2);
            b = br.ReadByte();
            fs.Position += 0x19;

            br.Close();
            fs.Close();

            loadPath = (image) ? imgPath : toc.fils[3].path;

            fs = new sio.FileStream(loadPath, sio.FileMode.Open, sio.FileAccess.Read, sio.FileShare.Read);
            br = new sio.BinaryReader(fs, ste.Default);
            if (image) fs.Position = toc.fils[3].pos;


            br.Close();
            fs.Close();

        }

        private void GetBanners(bool image)
        {
            int bnrC = 0;
            string sPat1 = "opening";
            string sPat2 = ".bnr";
            string tag = "";


            for (int i = 0; i < toc.fils.Count; i++)
            {
                if (!toc.fils[i].isDir)
                    if (toc.fils[i].name.IndexOf(sPat1) == 0)
                        if (toc.fils[i].name.LastIndexOf(sPat2) == toc.fils[i].name.Length - 4)
                        {
                            tag += string.Format("x{0:d2}{1:d6}", bnrC, i);
                            bnrC += 1;
                        }
            }

           
        }

 

    }

}
