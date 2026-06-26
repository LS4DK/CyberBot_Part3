using System;
using System.Windows.Forms;
using CyberBot_Part3.Forms;

namespace CyberBot_Part3
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}