using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSViewer
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Count() < 1) {
                MessageBox.Show("画像ファイルを指定してください。");
                return;
            }

            FileInfo info;
            try {
                info = new FileInfo(args[0]);
            } catch {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(info));
        }
    }
}
