using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.ListView;

namespace duplif
{
    public partial class Form1 : Form
    {
        private PictureBox[] pics;
        private const int maxPic = 50;
        private ManageImageDuplicates m;
        private long current_idx = 1;

        public Form1()
        {
            InitializeComponent();
            pics = new PictureBox[maxPic];
            toolStripStatusLabel2.Text = "";
            m = new ManageImageDuplicates(toolStripProgressBar1, toolStripStatusLabel1, maxPic);
        }

        private void resetPictureBoxes()
        {
            foreach (PictureBox p in pics)
            {
                flowLayoutPanel1.Controls.Remove(p);
                flowLayoutPanel2.Controls.Remove(p);
            }
            for (int j = 0; j < maxPic; j++)
            {
                if (pics[j] != null)
                {
                    pics[j].Dispose();
                    pics[j] = null;
                }
            }
        }

        private void show()
        {
            resetPictureBoxes();
            if (m == null) return;
            m.showDuplicate(current_idx, ref pics);
            foreach (PictureBox p in pics)
            {
                if(p!=null)
                {
                    if((bool)p.Tag)
                        flowLayoutPanel2.Controls.Add(p);
                    else
                        flowLayoutPanel1.Controls.Add(p);
                }
                
            }
        }

        private void process()
        {
            if (m == null) return;
            m.readMD5File(false);
            show();
        }

        private void moveToTrash()
        {
            m.remove(Convert.ToInt64(m.temppic_.Name.TrimStart('_')), m.temppic_.ImageLocation);
            flowLayoutPanel1.Controls.Remove(m.temppic_);
            flowLayoutPanel2.Controls.Add(m.temppic_);
        }

        private void removeFromTrash()
        {
            m.restore(Convert.ToInt64(m.temppic_.Name.TrimStart('_')), m.temppic_.ImageLocation);
            flowLayoutPanel2.Controls.Remove(m.temppic_);
            flowLayoutPanel1.Controls.Add(m.temppic_);
        }

        private void flowLayoutPanel2_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Bitmap))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
                e.Effect = DragDropEffects.None;
        }

        private void flowLayoutPanel1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Bitmap))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
                e.Effect = DragDropEffects.None;
        }
        private void flowLayoutPanel2_DragDrop(object sender, DragEventArgs e)
        {
            moveToTrash();
        }

        private void flowLayoutPanel1_DragDrop(object sender, DragEventArgs e)
        {
            removeFromTrash();
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (m == null) return;

            if (e.KeyCode == Keys.F10)
            {
                m.readMD5File(true);
            }

            long cnt = m.count();

            if (e.KeyCode == Keys.S) current_idx += 1;
            if (e.KeyCode == Keys.A) current_idx -= 1;
            if (e.KeyCode == Keys.E) current_idx = 1;
            if (e.KeyCode == Keys.V) current_idx = cnt;
            if (current_idx >= cnt) current_idx = cnt;
            if (current_idx < 1) current_idx = 1;
            
            toolStripStatusLabel2.Text = current_idx.ToString() + "/" + cnt.ToString();
            show();
        }

        private void toolStripDropDownButton1_Click(object sender, EventArgs e)
        {
            process();
        }

        private void toolStripDropDownButton2_Click(object sender, EventArgs e)
        {
            List<string> l = new List<string>();
            string files = "";

            m.removableFiles(ref l);

            int i;
            for(i=0; i<l.Count; i++)
            {
                if (i > 10) break;
                files += l.ElementAt(i) + (char)0x0d;
            }

            if (MessageBox.Show(((toolStripMenuItem1.Checked)?"Permanently delete ":"Move ") + (char)0x0d + files + ((i>10)? "and additional " + (l.Count - 10) + " files?" : "?"), "Question", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                string dest = "";
                if (toolStripMenuItem1.Checked == false)
                {
                    FolderBrowserDialog b = new FolderBrowserDialog();
                    b.Description = "Choose the destination folder for the duplicates to be moved";
                    if (b.ShowDialog() == DialogResult.OK)
                    {
                        dest = b.SelectedPath;
                    }
                    else
                    {
                        return;
                    }
                }
                StreamWriter f = new StreamWriter("removables.txt");
                foreach(string s in l)
                {
                    f.WriteLine(s);
                }
                f.Flush();
                f.Close();
                //System.Diagnostics.Process.Start("notepad", "removables.txt").WaitForExit();
                resetPictureBoxes();
                foreach (string s in l)
                {
                    try
                    {
                        if (toolStripMenuItem1.Checked)
                        {
                            File.Delete(s);
                        }
                        else
                        {
                            File.Move(s, dest + "\\" + s.Substring(s.LastIndexOf('\\')));
                        }
                    }
                    catch(Exception exc)
                    {
                        MessageBox.Show(exc.Message);
                    }
                }
                m.saveRemainingDuplicates();
                process();
            }
            
        }
    }
    public class Duplicated
    {
        private Dictionary<string,bool> filenames_;
        private string hashvalue_;
        private long idx_;

        public Duplicated()
        {
            filenames_ = new Dictionary<string, bool>();
            hashvalue_ = "";
        }

        public void setHash(string v)
        {
            hashvalue_ = v;
        }
        public string getHash()
        {
            return hashvalue_;
        }

        public void setIndex(long idx)
        {
            idx_ = idx;
        }
        public long getIndex()
        {
            return idx_;
        }

        public void addFilename(string f, bool d=false)
        {
            filenames_[f] = d;
        }
        public void addFilenames(string f, bool d=false)
        {
            string[] images = f.Split(';');
            foreach(string img in images)
            {
                filenames_[img] = d;
            }
        }
        public void getFilenames(ref List<string> fnames)
        {
            foreach (KeyValuePair<string, bool> kvp in filenames_)
            {
                fnames.Add(kvp.Key);
            }
        }
        public string getFilenames()
        {
            string fnames = "";
            foreach (KeyValuePair<string, bool> kvp in filenames_)
            {
                fnames += "[" + kvp.Key + "] ";
            }
            return fnames;
        }
        public void getRemovables(ref List<string> removables)
        {
            foreach (KeyValuePair<string, bool> kvp in filenames_)
            {
                if(kvp.Value)   removables.Add(kvp.Key);
            }
        }
        public bool isRemovable(string fname)
        {
            if(filenames_.ContainsKey(fname))
            {
                return filenames_[fname];
            }
            return false;
        }
        public void remove(string fname)
        {
            if (filenames_.ContainsKey(fname))
            {
                filenames_[fname] = true;
            }
        }
        public void restore(string fname)
        {
            if (filenames_.ContainsKey(fname))
            {
                filenames_[fname] = false;
            }
        }
    }

    public class ManageImageDuplicates
    {
        private ToolStripProgressBar progressbar_;
        private ToolStripStatusLabel statuslabel_;
        private string MD5file_;
        private Dictionary<long, Duplicated> entries_;
        private int maxPictures_;
        public PictureBox temppic_;
        private string imagefile_;

        // reads in the file which contains the MD5 hash values along with the image file names
        private void loadIn()
        {
            if (MD5file_ == "") return;

            var reader = new StreamReader(File.OpenRead(MD5file_), Encoding.Unicode, true);
            bool commaFound = false;
            string hash = "";
            string p = "";
            Dictionary<string, string> tmp = new Dictionary<string, string>();
            int count = 0, i;

            // calculate the number of lines within the file by counting the \r\n characters
            string all = reader.ReadToEnd();
            for (i = 0; i < all.Length; i++)
            {
                if ((i + 1) == all.Length) break;
                if ((all[i] == 0x0d) && (all[i + 1] == 0x0a))
                {
                    count++;
                }
            }
            statuslabel_.Text = "Processing file " + this.MD5file_;
            Application.DoEvents();
            progressbar_.Maximum = count;
            progressbar_.Value = 0;

            // let's go... read in the has values <KEY> and the image file names <VALUE> char by char
            // a little bit old C style but works and executes fast
            for (i = 0; i < all.Length; i++)
            {
                if ((i + 1) == all.Length) break;
                if ((all[i] == 0x00) || ((all[i] == 0x0d) && (all[i + 1] == 0x0a)))
                {
                    if (tmp.ContainsKey(hash))
                    {
                        // do not overwrite the value of an existing key instead add the file name
                        // to the end of teh value separated a semicolon
                        tmp[hash] += ";" + p;
                    }
                    else
                    {
                        tmp.Add(hash, p);
                    }

                    commaFound = false;
                    hash = p = "";
                    if ((all[i] == 0x0d) && (all[i + 1] == 0x0a))
                    {
                        progressbar_.Value++;
                    }
                    i++;
                    continue;
                }
                if (all[i] == ',')
                {
                    commaFound = true;
                    i++;
                }
                if (commaFound)
                {
                    p += all[i];
                }
                else
                {
                    hash += all[i];
                }
            }
            progressbar_.Value = 0;
            statuslabel_.Text = "Collecting duplications...";
            Application.DoEvents();
            // fill up the entries_ Dictionary map with the items containing duplicates
            // the process looks for a semicolon within the <VALUE> and if it has one 
            // it will be added to the map
            // the entries_ dictionary has <ID> => <DuplicatedImage> pairs
            long id = 1;
            foreach (KeyValuePair<string, string> kvp in tmp)
            {
                if (kvp.Value.IndexOf(';') > -1)
                {
                    Duplicated di = new Duplicated();
                    di.setIndex(id);
                    di.setHash(kvp.Key);
                    di.addFilenames(kvp.Value);
                    entries_[id] = di;
                    id++;
                }
            }
            Application.DoEvents();
            statuslabel_.Text = "Ready.";

            reader.Close();
            reader.Dispose();
            reader = null;
        }

        private void pics_paintEvent(object sender, PaintEventArgs e)
        {
            PictureBox p = sender as PictureBox;
            Font f = new Font("Arial", 10);

            string path = p.ImageLocation;
            var textPosition = new Point(3, 3);
            //Drawing logic begins here.
            var size = e.Graphics.MeasureString(path, f);
            var rect = new RectangleF(textPosition.X, textPosition.Y, size.Width, size.Height);
            //Filling a rectangle before drawing the string.
            e.Graphics.FillRectangle(Brushes.Black, rect);
            e.Graphics.DrawString(path, f, Brushes.White, textPosition);
        }

        private void pic_MouseMove(object sender, MouseEventArgs e)
        {
            temppic_ = sender as PictureBox;
            imagefile_ = temppic_.ImageLocation;
            if (e.Button == MouseButtons.Left)
                temppic_.DoDragDrop(temppic_.Image, DragDropEffects.All);
        }

        public ManageImageDuplicates(ToolStripProgressBar p, ToolStripStatusLabel s,
            int maxPics)
        {
            progressbar_ = p;
            statuslabel_ = s;
            maxPictures_ = maxPics;
            entries_ = new Dictionary<long, Duplicated>();
            MD5file_ = "";
        }

        public long count()
        {
            return entries_.Count;
        }

        public void readMD5File(bool firstread)
        {
            entries_.Clear();
            if (firstread)
            {
                OpenFileDialog of = new OpenFileDialog();
                of.DefaultExt = "*.csv";
                of.Filter = "Comma Separated Format|*.csv";
                of.InitialDirectory = "";
                of.Multiselect = false;
                if (of.ShowDialog() == DialogResult.OK)
                {
                    MD5file_ = of.FileName;
                }
            }
            loadIn();
        }

        public void showDuplicate(long idx, ref PictureBox[] pics)
        {
            if (idx < 1) idx = 1;
            if (idx >= entries_.Count) idx = entries_.Count;
            
            Duplicated di = (entries_.ContainsKey(idx)) ? entries_[idx] : null;

            if (di == null)
            {
                return;
            }

            int i = 0;
            List<string> fnames = new List<string>();
            di.getFilenames(ref fnames);

            foreach(string f in fnames)
            {
                try
                {
                    pics[i] = new PictureBox();
                    pics[i].Top = 3;
                    pics[i].Left = 3;
                    pics[i].Name = "_" + idx;
                    pics[i].Width = 380;
                    pics[i].Height = 240;
                    pics[i].ImageLocation = f;
                    pics[i].SizeMode = PictureBoxSizeMode.StretchImage;
                    pics[i].MouseMove += pic_MouseMove;
                    pics[i].Paint += pics_paintEvent;
                    pics[i].Tag = di.isRemovable(f);
                    i++;
                }
                catch(ArgumentException e)
                {
                    MessageBox.Show(e.Message + f);
                    return;
                }
            }
        }

        public void remove(long idx, string fname)
        {
            Duplicated di = (entries_.ContainsKey(idx)) ? entries_[idx] : null;

            if (di == null)
            {
                return;
            }
            di.remove(fname);
        }

        public void restore(long idx, string fname)
        {
            Duplicated di = (entries_.ContainsKey(idx)) ? entries_[idx] : null;

            if (di == null)
            {
                return;
            }
            di.restore(fname);
        }

        public void removableFiles(ref List<string> removables)
        {
            Duplicated di;

            foreach (KeyValuePair<long, Duplicated> e in entries_)
            {
                di = e.Value;
                di.getRemovables(ref removables);
            }
        }

        public void saveRemainingDuplicates()
        {

            StreamWriter ss = new StreamWriter(MD5file_, false, Encoding.UTF8);
            Duplicated di;

            foreach (KeyValuePair<long, Duplicated> e in entries_)
            {
                di = e.Value;
                List<string> l = new List<string>();
                string hash = di.getHash();

                di.getFilenames(ref l);

                foreach(string s in l)
                {
                    if (di.isRemovable(s) == false)
                    {
                        ss.WriteLine(hash + "," + s);
                    }
                }
                ss.Flush();
            }
            ss.Close();
        }
    }
}