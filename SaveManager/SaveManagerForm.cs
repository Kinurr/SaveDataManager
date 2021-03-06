﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Newtonsoft.Json;

namespace SaveManager
{
    public partial class SaveManagerForm : Form
    {
        private static int saveIDCounter = 0;
        private List<SaveFile> saveFiles;

        public SaveManagerForm()
        {
            InitializeComponent();

            string _localBackupDirectory = Properties.Settings.Default.LocalBackupDirectory;
            saveFiles = new List<SaveFile>();

            if (_localBackupDirectory == "")
                BackupDirectoryText.Text = Directory.GetCurrentDirectory() + Properties.Settings.Default.DefaultBakcupDirectory;
            else
                BackupDirectoryText.Text = Properties.Settings.Default.LocalBackupDirectory;

            CreateManifestFile(BackupDirectoryText.Text);
            GetSavesFromManifest();

        }

        private void BackupButton_Click(object sender, EventArgs e)
        {
            BackupDirectory();
        }

        private void GetSavesFromManifest()
        {
            try
            {
                List<SaveFile> _list = JsonConvert.DeserializeObject<List<SaveFile>>(File.ReadAllText(BackupDirectoryText.Text + "\\savemanifest.json"));
                saveFiles.Clear();

                if( _list != null)
                    foreach (SaveFile sf in _list)
                        saveFiles.Add(sf);

                RefreshUIList();
            }
            catch (IOException e)
            {
                MessageBox.Show(e.Message + "\n A new file will now be created.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void RefreshUIList()
        {
            SaveDirectoryList.Rows.Clear();

            foreach (SaveFile sf in saveFiles) {

                var date = (DateTime.Compare(sf.LastBackupDate, DateTime.Parse("0001-01-01T00:00:00")) == 0) ? "Not Backed Up" : sf.LastBackupDate.ToString();
                SaveDirectoryList.Rows.Add(sf.Title, date, sf.Platform, sf.OriginalPath);
            }
        }

        private void BackupDirectory()
        {
            List<string> titles = new List<string>();

            foreach (SaveFile sf in saveFiles)
            {

                //Copy the save folder into the backup directory
                string fullPath = Path.GetFullPath(sf.OriginalPath);
                string folderName = Path.GetFileName(fullPath);

                string targetPath = Path.Combine(BackupDirectoryText.Text, folderName);

                Directory.CreateDirectory(targetPath);

                Debug.WriteLine(targetPath + " " + Directory.Exists(targetPath));

                //Now Create all of the directories
                foreach (string dirPath in Directory.GetDirectories(fullPath, "*",
                    SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(fullPath, targetPath));
                    SetInfoText("Created directory " + folderName);
                }

                //Copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(fullPath, "*.*",
                    SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Copy(newPath, newPath.Replace(fullPath, targetPath), true);
                        SetInfoText("Copied file " + fullPath);
                    }
                    catch (IOException exception)
                    {
                        MessageBox.Show(exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                //Add to list of backed up titles
                titles.Add(sf.Title);

                //Update last backup date
                sf.LastBackupDate = DateTime.Now;
            }

            //TODO: Put this in a function and class of its own called Utils
            string formattedList = "";

            foreach (var title in titles)
            {
                formattedList += "  - " + title + "\n";
            }

            MessageBox.Show("Sucessfully backed up save files: \n \n" + formattedList, "Backup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Utils.SerializeAndSaveToJSON(BackupDirectoryText.Text + "\\savemanifest.json", saveFiles);
            RefreshUIList();
        }

        private void BackupDirButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog backupDirectoryBrowser = new FolderBrowserDialog();


            if (backupDirectoryBrowser.ShowDialog() == DialogResult.OK)
            {
                BackupDirectoryText.Text = backupDirectoryBrowser.SelectedPath;
                SetInfoText("Backup directory set to " + backupDirectoryBrowser.SelectedPath);
                CreateManifestFile(BackupDirectoryText.Text);
                Properties.Settings.Default.LocalBackupDirectory = backupDirectoryBrowser.SelectedPath;
                Properties.Settings.Default.Save();
            }

            backupDirectoryBrowser.Dispose();
        } 

        private void AddButton_Click(object sender, EventArgs e)
        {
            AddSaveFileForm addSaveFileForm = new AddSaveFileForm(saveIDCounter, saveFiles, BackupDirectoryText.Text);
            addSaveFileForm.ShowDialog();
            RefreshUIList();
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if(SaveDirectoryList.SelectedRows.Count >= 0)
            {
                foreach (DataGridViewRow row in SaveDirectoryList.SelectedRows)
                {
                    SaveDirectoryList.Rows.Remove(row);
                    SetInfoText("Removed " + row.Cells[2]);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetInfoText("Application started");
            SaveDirectoryList.AllowUserToAddRows = false;
        }

        private void SetInfoText(string s)
        {
            DebugLabel.Text = s;
        }

        private void CreateManifestFile(string path)
        {
            if (!File.Exists(path + "\\savemanifest.json"))
                File.Create(path + "\\savemanifest.json");
        }

    }
}
