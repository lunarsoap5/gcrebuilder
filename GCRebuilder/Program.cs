using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace GCRebuilder
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length > 1)
            {
                try
                {
                    MainForm mf = new GCRebuilder.MainForm(args);

                    if (mf.IsImagePath(args[1]))
                    {
                        if (args.Length == 3)
                        {

                            if (args[0].Equals("--extract"))
                            {
                                mf.ImageOpen(args[1]);
                                mf.Export(args[2]);
                            }
                            else if (args[0].Equals("--import"))
                            {
                                mf.ImageOpen(args[1]);
                                mf.Import(args[2]);
                            }
                            else
                                Usage();
                        }
                        else
                        {
                            Usage();
                        }
                    }
                    else if (mf.IsRootPath(args[1]))
                    {
                        if (args.Length >= 3)
                        {
                            if (args[0].Equals("--rebuild"))
                            {
                                if (args[3].Equals("--noGameTOC"))
                                {
                                    mf.RootOpen(args[1], true);
                                }
                                else
                                {
                                    mf.RootOpen(args[1], false);
                                }
                                mf.Rebuild(args[2]);
                            }
                        }
                        else
                        {
                            Usage();
                        }
                    }
                    else
                    {
                        Usage();
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);

                    return ex.HResult;
                }
            }
            else if ((args.Length == 1) && (args[0].Equals("help")))
            {
                Usage();
            }
            else
            {
                ShowWindow(GetConsoleWindow(), 0);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(args));
            }

            return 0;
        }

        static void Usage()
        {
            Console.WriteLine("--extract|import|rebuild iso_path folder_path");
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
