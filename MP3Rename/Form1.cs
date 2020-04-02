using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MP3Rename
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// Путь к папке, в которой хранятся подпапки и файлы с файлами, которые нужно переименовать
        /// </summary>
        private string sFolderPath;
        /// <summary>
        /// Путь к папке в которой будут сохраняться уже обработанные файлы
        /// </summary>
        private string sNewFolderPath;
        /// <summary>
        /// Название файла в системе
        /// </summary>
        private string sSongTitle;
        /// <summary>
        /// Тег исполнителя, хранимый в метаинформации файла
        /// </summary>
        private string sAuthor;
        /// <summary>
        /// Тег названия песни, который хранится в метаинформации файла
        /// </summary>
        private string sSongName;
        /// <summary>
        /// Сведения о битрейте, которые находятся в метаинформации файла
        /// </summary>
        private uint uiBitRate;
        private string sSongPath;
        private static string pattern = @"[\*\\\;\?\:\\""\/\<\>\|]";
        private Regex rgx = new Regex(pattern);

        public Form1()
        {
            InitializeComponent();
        }

        private void btnOpenDir_Click(object sender, EventArgs e)
        {
            btnRename.Enabled = false;
            FolderBrowserDialog fbDialog = new FolderBrowserDialog(); //Диалог выбора папки
            dgvProcessing();
            if (fbDialog.ShowDialog() == DialogResult.OK) //Если папка выбрана, то сохраняем путь к ней
            {
                sFolderPath = fbDialog.SelectedPath;
            }

            if(sFolderPath != null)
            {
                DirectoryInfo di = new DirectoryInfo(sFolderPath);

                if (di.GetDirectories() != null)
                {
                    //Выбираем все файлы в папке и во всех подпапках, считываем их метаинформацию 
                    //и заносим их в табличку.
                    foreach (FileInfo file in di.GetFiles())
                    {
                        GetMP3Files(file);
                    }

                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        foreach (FileInfo file in dir.GetFiles())
                        {
                            GetMP3Files(file);
                        }
                    }
                }
                else
                {
                    foreach (FileInfo file in di.GetFiles())
                    {
                        GetMP3Files(file);
                    }
                }
                
            }
            btnRename.Enabled = true;
            fbDialog.Dispose(); //Освобождаем ресурсы 
        }

        private void GetMP3Files(FileInfo f)
        {
            try
            {
                TagLib.File file = TagLib.File.Create(f.FullName); //Открытие файла
                Encoding win1252 = Encoding.GetEncoding("Windows-1252"); //Западно-Европейская кодировка
                Encoding win1251 = Encoding.GetEncoding("Windows-1251"); //Кириллица
                byte[] win1252Bytes;
                byte[] win1251Bytes;
                
                win1252Bytes = win1252.GetBytes(f.Name.ToString());
                win1251Bytes = Encoding.Convert(win1252, win1251, win1252Bytes);
                sSongTitle = win1251Bytes.ToString();

                sAuthor = file.Tag.FirstPerformer;

                win1252Bytes = win1252.GetBytes(file.Tag.Title);
                win1251Bytes = Encoding.Convert(win1252, win1251, win1252Bytes);
                sSongName = win1251Bytes.ToString();
                uiBitRate = file.Tag.BeatsPerMinute;
                sSongPath = f.FullName.ToString();
                dataGridView1.Rows.Add(sSongTitle, sAuthor, sSongName, uiBitRate, sSongPath); //Добавляем в список
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message + "During processing mp3-file error occurs: \n" + ex.StackTrace, "Error occurs!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void dgvProcessing()
        {
            var column0 = new DataGridViewColumn();
            column0.HeaderText = "File Name";
            column0.Width = 100;
            column0.ReadOnly = true; //Может ли пользователь менять значение в ячейках столбца
            column0.Name = "filename";
            column0.Frozen = true; //Может ли пользователь менять положение столбца
            column0.CellTemplate = new DataGridViewTextBoxCell(); //Тип ячеек - текстовое поле

            var column1 = new DataGridViewColumn();
            column1.HeaderText = "Artist";
            column1.ReadOnly = false;
            column1.Name = "artistname";
            column1.Frozen = true;
            column1.CellTemplate = new DataGridViewTextBoxCell();

            var column2 = new DataGridViewColumn();
            column2.HeaderText = "Song";
            column2.ReadOnly = false;
            column2.Name = "songname";
            column2.Frozen = true;
            column2.CellTemplate = new DataGridViewTextBoxCell();

            var column3 = new DataGridViewColumn();
            column3.HeaderText = "Bit rate";
            column3.ReadOnly = true;
            column3.Name = "bitratevalue";
            column3.Frozen = true;
            column3.CellTemplate = new DataGridViewTextBoxCell();

            var column4 = new DataGridViewColumn();
            column4.HeaderText = "Fullpath";
            column4.ReadOnly = true;
            column4.Name = "path";
            column4.Frozen = true;
            column4.CellTemplate = new DataGridViewTextBoxCell();

            //Добавляем столбцы в дата грид
            dataGridView1.Columns.Add(column0);
            dataGridView1.Columns.Add(column1);
            dataGridView1.Columns.Add(column2);
            dataGridView1.Columns.Add(column3);
            dataGridView1.Columns.Add(column4);
            dataGridView1.AllowUserToAddRows = false; //Запретить пользователю добавлять строки.
        }

        private void btnRename_Click(object sender, EventArgs e)
        {
            if(dataGridView1.Rows != null)
            {
                //Диалог для выбора папки сохранения
                FolderBrowserDialog fbDialog = new FolderBrowserDialog();
                fbDialog.ShowNewFolderButton = true;

                if (fbDialog.ShowDialog() == DialogResult.OK)
                {
                    sNewFolderPath = fbDialog.SelectedPath;
                }

                if (sNewFolderPath == null)
                {
                    sNewFolderPath = @"C:\Music";
                }

                MP3FileProcessing();
                fbDialog.Dispose(); //Освобождаем ресурсы
            }
        }

        private void MP3FileProcessing()
        {
            try
            {
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if ((dataGridView1["artistname", i].Value != null) && (dataGridView1["songname", i].Value != null))
                    {
                        sSongTitle = dataGridView1["artistname", i].Value.ToString() + @" - " + dataGridView1["songname", i].Value.ToString();
                    }
                    else
                    {
                        sSongTitle = dataGridView1["filename", i].Value.ToString();
                    }
                    sSongTitle = rgx.Replace(sSongTitle, "");
                    File.Move(dataGridView1["path", i].Value.ToString(), sNewFolderPath + @"\" + i + ". " + sSongTitle + ".mp3");
                    dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.Green;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("During processing mp3-file error occurs: \n" + ex.StackTrace, "Error occurs!" + ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }
    }
}
