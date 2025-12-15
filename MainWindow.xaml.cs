using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace NanCrypt.UI
{
    public partial class MainWindow : Window
    {
        private enum AppMode { Encrypt, Decrypt, Scan }
        private AppMode _currentMode = AppMode.Encrypt;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Mode_Checked(object sender, RoutedEventArgs e)
        {
            if (RadioEncrypt == null || RadioDecrypt == null || RadioScan == null || PanelPassword == null || BtnAction == null || LblStatus == null) return;

            if (RadioEncrypt.IsChecked == true)
            {
                _currentMode = AppMode.Encrypt;
                PanelPassword.Visibility = Visibility.Visible;
                BtnAction.Content = "INITIATE ENCRYPTION";
                BtnAction.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF41"));
            }
            else if (RadioDecrypt.IsChecked == true)
            {
                _currentMode = AppMode.Decrypt;
                PanelPassword.Visibility = Visibility.Visible;
                BtnAction.Content = "INITIATE DECRYPTION";
                BtnAction.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF41"));
            }
            else if (RadioScan.IsChecked == true)
            {
                _currentMode = AppMode.Scan;
                PanelPassword.Visibility = Visibility.Collapsed;
                BtnAction.Content = "SCAN FOR THREATS";
                BtnAction.Foreground = Brushes.Red;
            }
        }

        private void ChkBatchMode_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (PanelFileTypes == null || ChkArchive == null) return;
            bool isBatch = ChkBatchMode.IsChecked == true;
            
            // Show Archive option only in Batch Mode
            ChkArchive.Visibility = isBatch ? Visibility.Visible : Visibility.Collapsed;
            
            // Show File Types only if Batch is ON and Archive is OFF
            bool isArchive = ChkArchive.IsChecked == true;
            PanelFileTypes.Visibility = (isBatch && !isArchive) ? Visibility.Visible : Visibility.Collapsed;
            
            if (!isBatch && TxtCustomPattern != null) TxtCustomPattern.Visibility = Visibility.Collapsed;
        }

        private void ChkArchive_CheckedChanged(object sender, RoutedEventArgs e)
        {
             // Retrigger batch logic to update visibility
             ChkBatchMode_CheckedChanged(sender, e);
        }

        private void CmbFilePattern_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CmbFilePattern == null || TxtCustomPattern == null) return;
            var selectedItem = CmbFilePattern.SelectedItem as System.Windows.Controls.ComboBoxItem;
            if (selectedItem != null && (string)selectedItem.Tag == "Custom")
            {
                TxtCustomPattern.Visibility = Visibility.Visible;
            }
            else
            {
                TxtCustomPattern.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (ChkBatchMode.IsChecked == true)
            {
                using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
                {
                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        TxtFilePath.Text = dlg.SelectedPath;
                        LblStatus.Text = "TARGET FOLDER ACQUIRED.";
                    }
                }
            }
            else
            {
                OpenFileDialog dlg = new OpenFileDialog();
                if (dlg.ShowDialog() == true)
                {
                    TxtFilePath.Text = dlg.FileName;
                    LblStatus.Text = "TARGET FILE ACQUIRED.";
                }
            }
            LblStatus.Foreground = Brushes.White;
        }

        private void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = TxtFilePath.Text;
                bool isBatch = ChkBatchMode.IsChecked == true;
                bool isArchive = ChkArchive.IsChecked == true;

                if (string.IsNullOrWhiteSpace(path) || (isBatch ? !Directory.Exists(path) : !File.Exists(path)))
                {
                    MessageBox.Show("Invalid Target.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if ((_currentMode == AppMode.Encrypt || _currentMode == AppMode.Decrypt) && string.IsNullOrEmpty(TxtPassword.Text))
                {
                    MessageBox.Show("Key Required.", "Security Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (isBatch)
                {
                    if (isArchive)
                    {
                         ProcessFolderArchive(path);
                    }
                    else
                    {
                         ProcessBatch(path);
                    }
                }
                else
                {
                    ProcessSingleFile(path);
                }
            }
            catch (Exception ex)
            {
                ShowError($"SYSTEM FAILURE: {ex.Message}");
            }
        }

        private void ProcessFolderArchive(string folderPath)
        {
            // 1. Zip the folder
            string parentDir = Directory.GetParent(folderPath).FullName;
            string folderName = new DirectoryInfo(folderPath).Name;
            string zipPath = Path.Combine(parentDir, folderName + ".zip");
            
            // Delete existing if any
            if (File.Exists(zipPath)) File.Delete(zipPath);

            LblStatus.Text = "COMPRESSING DATA...";
            System.Windows.Forms.Application.DoEvents(); // Force UI update
            
            System.IO.Compression.ZipFile.CreateFromDirectory(folderPath, zipPath);

            // 2. Encrypt/Scan the Zip
            if (_currentMode == AppMode.Encrypt)
            {
                 LblStatus.Text = "ENCRYPTING ARCHIVE...";
                 System.Windows.Forms.Application.DoEvents();
                 
                 int res = CoreInterop.EncryptFileNative(zipPath, TxtPassword.Text);
                 if (res == 0)
                 {
                     ShowSuccess($"ARCHIVE ENCRYPTED: {zipPath}");
                     // Optional: Delete the unencrypted zip?
                     // File.Delete(zipPath); // Maybe keep it for safety unless requested
                 }
                 else 
                 {
                     ShowError($"ARCHIVE ENCRYPTION FAILED. CODE: {res}");
                 }
            }
            else if (_currentMode == AppMode.Scan)
            {
                 int res = CoreInterop.ScanFileNative(zipPath);
                 if (res == 0) ShowSuccess("ARCHIVE CLEAN.");
                 else if (res == 1) ShowError("VIRUS DETECTED IN ARCHIVE!");
                 else ShowError($"SCAN ERROR: {res}");
            }
            else // Decrypt doesn't make sense on a folder input for Archive mode generally, unless we treat it as "Batch Decrypt"?
            {
                // If user selected "Decrypt" and "Archive Mode", it usually implies they want to decrypt an archive.
                // But the input is a FOLDER (from Batch Mode).
                // This is a logic conflict. Archive Mode usually implies "Create Archive".
                // We'll warn the user.
                MessageBox.Show("To decrypt an archive, please switch off 'Batch Mode' and select the .zip file directly.", "Info");
            }
        }

        private void ProcessBatch(string folderPath)
        {
            var selectedItem = CmbFilePattern.SelectedItem as System.Windows.Controls.ComboBoxItem;
            string patterns = (string)selectedItem.Tag;
            
            if (patterns == "Custom") patterns = TxtCustomPattern.Text;
            if (string.IsNullOrWhiteSpace(patterns)) patterns = "*.*";

            string[] exts = patterns.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            int successCount = 0;
            int threatCount = 0;
            int errorCount = 0;
            
            // Limit report size
            string firstThreat = "";

            foreach (var ext in exts)
            {
                try 
                {
                    string[] files = Directory.GetFiles(folderPath, ext.Trim(), SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        // Update UI slightly to show progress (blocking in this simple thread model)
                        LblStatus.Text = $"SCANNING: {Path.GetFileName(file)}";
                        System.Windows.Forms.Application.DoEvents(); 

                        int res = ExecuteCoreAction(file);
                        
                        if (_currentMode == AppMode.Scan)
                        {
                            if (res == 0) // Clean
                            {
                                successCount++;
                            }
                            else if (res == 1) // Virus
                            {
                                threatCount++;
                                if (string.IsNullOrEmpty(firstThreat)) firstThreat = Path.GetFileName(file);
                            }
                            else // Error
                            {
                                errorCount++;
                            }
                        }
                        else // Encrypt/Decrypt
                        {
                            if (res == 0) successCount++;
                            else errorCount++;
                        }
                    }
                } 
                catch 
                { 
                    errorCount++; 
                } 
            }

            LblStatus.Text = "BATCH OPERATION COMPLETE.";

            if (_currentMode == AppMode.Scan)
            {
                if (threatCount > 0)
                {
                    string msg = $"SCAN COMPLETE.\n\nTHREATS DETECTED: {threatCount}\nSAFE FILES: {successCount}\nERRORS: {errorCount}";
                    if (!string.IsNullOrEmpty(firstThreat)) msg += $"\n\nFirst Detection: {firstThreat}";
                    ShowError(msg);
                }
                else
                {
                    ShowSuccess($"SCAN COMPLETE. SYSTEM CLEAN.\nScanned {successCount} files.");
                }
            }
            else
            {
                if (errorCount == 0 && successCount > 0) ShowSuccess($"BATCH COMPLETE. PROCESSED {successCount} FILES.");
                else ShowError($"BATCH COMPLETED WITH ERRORS.\nSuccess: {successCount}\nFailed: {errorCount}");
            }
        }

        private void ProcessSingleFile(string path)
        {
            int result = ExecuteCoreAction(path);
            if (result == 0) ShowSuccess("OPERATION SUCCESSFUL.");
            else if (_currentMode == AppMode.Scan && result == 1) ShowError("THREAT DETECTED!");
            else ShowError($"OPERATION FAILED. CODE: {result}");
        }

        private int ExecuteCoreAction(string path)
        {
            switch (_currentMode)
            {
                case AppMode.Encrypt: return CoreInterop.EncryptFileNative(path, TxtPassword.Text);
                case AppMode.Decrypt: return CoreInterop.DecryptFileNative(path, TxtPassword.Text);
                case AppMode.Scan: return CoreInterop.ScanFileNative(path);
                default: return -1;
            }
        }

        private void ShowSuccess(string msg)
        {
            LblStatus.Text = msg;
            LblStatus.Foreground = _currentMode == AppMode.Scan ? Brushes.Cyan : Brushes.LimeGreen;
            MessageBox.Show(msg, "Operation Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowError(string msg)
        {
            LblStatus.Text = msg;
            LblStatus.Foreground = Brushes.Red;
            MessageBox.Show(msg, "Operation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
