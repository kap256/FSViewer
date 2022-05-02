using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSViewer
{

    /// <summary>
    /// フォームクラス
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// 画像ファイル情報クラス
        /// </summary>
        class ImageFile
        {
            public string Path;
            public bool IsDelete;
            public ImageFile(string path)
            {
                Path = path;
                IsDelete = false;
            }
            public string Label => $"{(IsDelete?"[×] ":"")}{System.IO.Path.GetFileName(Path)}";
        }

        /// <summary>
        /// 並び順を自然にするための比較クラス
        /// </summary>
        class StrCmpLogical : IComparer<string>
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            public static extern int StrCmpLogicalW(string str1, string str2);

            public int Compare(string x, string y)
            {
                return StrCmpLogicalW(x, y);
            }
        }

        //メンバのみなさん
        List<ImageFile> ImageFiles = new List<ImageFile>();
        int Index = -1;
        Image CurrentImg = null;

        /// <summary>
        /// コンストラクタ。ディレクトリの走査と、最初の画像読み込み
        /// </summary>
        public MainForm(FileInfo info)
        {
            //IEnumだと使いにくいので、Listにするぞ。
            //並び順も自然にしよう。
            int index = 0;
            foreach (var file in info.Directory.EnumerateFiles().OrderBy(value => value.FullName, new StrCmpLogical())) {
                ImageFiles.Add(new ImageFile(file.FullName));
                if (file.FullName == info.FullName) {
                    Index = index;
                } else {
                    index++;
                }
            }

            //こんな事態になる前に例外死してそうだが……。
            Index = ImageFiles.FindIndex(var => var.Path == info.FullName);
            if (Index < 0) {
                return;
            }

            InitializeComponent();
        }

        /// <summary>
        /// 画像読み込み。
        /// </summary>
        /// <param name="move">Indexをどの方向にずらすか。±1の範囲内で指定。</param>
        void ReadImage(int move)
        {
            Debug.Assert(Math.Abs(move) <= 1);
            int old = Index;
            do {
                Index += move;
                if (Index < 0) {
                    Index = ImageFiles.Count - 1;
                } else if (Index >= ImageFiles.Count) {
                    Index = 0;
                }

                var file = ImageFiles[Index];
                label_filename.Text = file.Label;
                try {
                    using (var img = Image.FromFile(file.Path)) {
                        SetImageAndSize(img);
                        if (CurrentImg != null) {
                            CurrentImg.Dispose();
                            CurrentImg = null;
                        }
                        CurrentImg = (Image)(img.Clone());
                    }
                    return;
                } catch {
                    //画像じゃなかったっぽい。次のファイルを読みに行く。
                }

            } while (Index != old);
        }

        /// <summary>
        /// 読み込んだ画像に対して、適切なサイズ設定を行う。
        /// </summary>
        void SetImageAndSize(Image img)
        {
            //アスペクト比計算
            double win_ratio = (double)(ClientSize.Width) / (double)(ClientSize.Height);
            double img_ratio = (double)(img.Width) / (double)(img.Height);

            //拡大率計算
            double zoom;
            if (win_ratio > img_ratio) {
                //ディスプレイのほうが横長
                zoom = (double)(ClientSize.Height) / (double)(img.Height);
            } else {
                //ディスプレイのほうが縦長
                zoom = (double)(ClientSize.Width) / (double)(img.Width);
            }

            //拡大描画対象を作成
            Bitmap canvas = new Bitmap(ClientSize.Width, ClientSize.Height);
            using (Graphics g = Graphics.FromImage(canvas)) {
                if (IsDotImage(zoom, img)) {
                    //ドット絵なら二アレストネイバーで整数倍
                    zoom = (int)(zoom);
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                } else {
                    //そうでなければ普通に拡縮
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                }

                g.DrawImage(img, 0, 0, (int)(img.Width * zoom), (int)(img.Height * zoom));
            }

            //ピクチャーボックスに設定
            pictureBox.Width = (int)(img.Width * zoom);
            pictureBox.Height = (int)(img.Height * zoom);
            pictureBox.Location = new Point((ClientSize.Width - pictureBox.Width) / 2, (ClientSize.Height - pictureBox.Height) / 2);
            pictureBox.Image = canvas;

        }

        /// <summary>
        /// この画像はドット絵であると思われるかどうか？
        /// とりあえず3倍超の拡大だったらドット絵と見なす雑な判定にしておく。
        /// </summary>
        bool IsDotImage(double zoom, Image img)
        {
            return (zoom > 3);
        }

        /// <summary>
        /// キーイベント。
        /// </summary>
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode) {
                case Keys.Escape:
                case Keys.Enter:
                    this.Close();
                    break;

                case Keys.Right:
                case Keys.Down:
                    ReadImage(1);
                    break;

                case Keys.Left:
                case Keys.Up:
                    ReadImage(-1);
                    break;
                case Keys.F11:
                    ToggleWindow();
                    break;
                case Keys.F12:
                    ToggleFilename();
                    break;
                case Keys.Delete:
                    SetDeleteFlag();
                    break;
            }
        }
        /// <summary>
        /// 初回表示時の処理。コンストラクタではまだウインドウが最大化していないらしいため。
        /// </summary>
        private void MainForm_Shown(object sender, EventArgs e)
        {
            ReadImage(0);
        }

        /// <summary>
        /// クリックで閉じる。->却下
        /// </summary>
        private void MainForm_Click(object sender, EventArgs e)
        {
            //this.Close();
        }

        /// <summary>
        /// ウインドウモード切替
        /// </summary>
        private void ToggleWindow()
        {
            if (this.FormBorderStyle == FormBorderStyle.None) {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
            } else {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }
        }

        /// <summary>
        //  ファイル名表示切替
        /// </summary>
        private void ToggleFilename()
        {
            label_filename.Visible = !label_filename.Visible;
        }

        /// <summary>
        //  ファイルを削除フォルダに移動させるフラグを立てる
        /// </summary>
        private void SetDeleteFlag()
        {

            var file = ImageFiles[Index];
            file.IsDelete = !file.IsDelete;
            label_filename.Text = file.Label;
        }

        /// <summary>
        //  サイズ変更
        /// </summary>
        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            if (CurrentImg != null) {
                SetImageAndSize(CurrentImg);
            }
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            CurrentImg.Dispose();
            CurrentImg = null;

            DeleteFlagged();
        }
        /// <summary>
        //  フラグ済みファイルを削除フォルダに移動する。
        /// 終了処理以外で呼ぶこととか考えてないからな？
        /// </summary>
        private void DeleteFlagged()
        {
            string dir_path = null;
            foreach (var file in ImageFiles) {
                if (file.IsDelete) {
                    var file_info = new FileInfo(file.Path);
                    if (dir_path == null) {
                        dir_path = file_info.DirectoryName + @"\__delete\";
                        if (!Directory.Exists(dir_path)) {
                            Directory.CreateDirectory(dir_path);
                        }
                    }
                    file_info.MoveTo(dir_path + file_info.Name);
                }
            }
        }
    }
}
