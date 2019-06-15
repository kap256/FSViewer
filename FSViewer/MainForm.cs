using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSViewer
{
    public partial class MainForm : Form
    {
        List<FileInfo> ImageFiles = new List<FileInfo>();
        int Index = -1;

        /// <summary>
        /// コンストラクタ。ディレクトリの走査と、最初の画像読み込み
        /// </summary>
        public MainForm(FileInfo info)
        {
            //IEnumだと使いにくいので、Listにするぞ。
            int index = 0;
            foreach (var file in info.Directory.EnumerateFiles()) {
                ImageFiles.Add(file);
                if(file.FullName == info.FullName) {
                    Index = index;
                } else {
                    index++;
                }
            }

            //こんな事態になる前に例外死してそうだが……。
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
                }else if(Index >= ImageFiles.Count) {
                    Index = 0;
                }

                var file = ImageFiles[Index];
                try {
                    using (var img = Image.FromFile(file.FullName)) {
                        SetImageAndSize(img);
                    }
                    return;
                } catch {
                    //画像じゃなかったっぽい。次のファイルを読みに行く。
                }

            } while (Index!=old);
        }

        /// <summary>
        /// 読み込んだ画像に対して、適切なサイズ設定を行う。
        /// </summary>
        void SetImageAndSize(Image img)
        {
            //アスペクト比計算
            double win_ratio = (double)(this.Width) / (double)(this.Height);
            double img_ratio = (double)(img.Width) / (double)(img.Height);

            //拡大率計算
            double zoom;
            if(win_ratio > img_ratio) {
                //ディスプレイのほうが横長
                zoom = (double)(this.Height) / (double)(img.Height);
            } else {
                //ディスプレイのほうが縦長
                zoom = (double)(this.Width) / (double)(img.Width);
            }

            //拡大描画対象を作成
            Bitmap canvas = new Bitmap(this.Width, this.Height);
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
            pictureBox.Location= new Point((this.Width- pictureBox.Width)/2, (this.Height - pictureBox.Height) / 2);
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
            }
        }
        /// <summary>
        /// 初回表示時の処理。コンストラクタではまだウインドウが最大化していないらしいため。
        /// </summary>
        private void MainForm_Shown(object sender, EventArgs e)
        {
            ReadImage(0);
        }
    }
}
