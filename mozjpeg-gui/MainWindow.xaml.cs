using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Json;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Size = System.Windows.Size;

namespace mozjpeg_gui
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{
		public string CjpegPath = "";
        public string CwebpPath = "";
        public string DwebpPath = "";
        public string OpeningFileName = "";
		public string ImagePath = "";
		public string ImageResizedPath = "";
		public string PreviewPath = "";
		public long ImageFileLength = 0;
		public long PreviewFileLength = 0;
		public Size ImageFileSize = new Size(0, 0);
		public BitmapImage ImageBitmap;
		public BitmapImage ImageResizedBitmap;
		public BitmapImage PreviewBitmap;
        public bool IgnoreLargeImageWarning = false;
        public string CurrentLanguage = "zh-CN";

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
            CjpegPath = Path.GetTempPath() + Guid.NewGuid() + ".exe";
            CwebpPath = Path.GetTempPath() + Guid.NewGuid() + ".exe";
            DwebpPath = Path.GetTempPath() + Guid.NewGuid() + ".exe";
            File.WriteAllBytes(CjpegPath, mozjpeg_gui.Properties.Resources.cjpeg);
            File.WriteAllBytes(CwebpPath, mozjpeg_gui.Properties.Resources.cwebp);
            File.WriteAllBytes(DwebpPath, mozjpeg_gui.Properties.Resources.dwebp);

            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Label_ImageInfo.Content = string.Format(
                "MozJPEG-GUI v{0}.{1}.{2} by TransparentLC",
                versionInfo.FileMajorPart,
                versionInfo.FileMinorPart,
                versionInfo.FileBuildPart
            );
        }

        private void Window_Closed(object sender, EventArgs e)
		{
            File.Delete(CjpegPath);
            File.Delete(CwebpPath);
            File.Delete(DwebpPath);
            if (File.Exists(ImagePath)) File.Delete(ImagePath);
            if (File.Exists(ImageResizedPath)) File.Delete(ImageResizedPath);
            if (File.Exists(PreviewPath)) File.Delete(PreviewPath);
        }

        private void Window_Drop(object sender, DragEventArgs e)
		{
			string[] extension = { ".bmp", ".jpg", ".jpeg", ".png", ".gif", ".webp" };
			if (extension.Contains(Path.GetExtension(((string[])e.Data.GetData(DataFormats.FileDrop))[0]).ToLower()))
			{
				OpenImage(((string[])e.Data.GetData(DataFormats.FileDrop))[0]);
			}
			else
			{
				MessageBox.Show((string)TryFindResource("UnsupportedFormat"), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		string FormatFileSize(long Size) {
			string[] Unit = { "B", "KB", "MB", "GB", "TB" };
			int UnitIndex = 0;
			double DSize = Size;
			while (DSize >= 1024 && UnitIndex < Unit.Length - 1) {
				DSize /= 1024;
				UnitIndex++;
			}
			return string.Format("{0:0.##} {1}", DSize, Unit[UnitIndex]);
		}

		void SaveBMPImage(string source, string destination) {
            if (Path.GetExtension(source).ToLower() == ".webp")
            {
                Process process = new Process();
                process.StartInfo.FileName = DwebpPath;
                process.StartInfo.Arguments = "\"" + source + "\" -bmp -mt -o \"" + destination + "\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                process.WaitForExit();
                process.Close();
            }
            else
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(new Uri(source)));
                using (var fileStream = new FileStream(destination, FileMode.Create))
                {
                    encoder.Save(fileStream);
                }
            }
        }

		void SaveResizedBMPImage(string source, string destination, System.Drawing.Drawing2D.InterpolationMode interpolation, int width, int height)
		{
			Bitmap bsource = new Bitmap(source);
			Bitmap bitmap = new Bitmap(width, height);
			using (Graphics g = Graphics.FromImage(bitmap))
			{
				g.InterpolationMode = interpolation;
				g.DrawImage(bsource, 0, 0, width, height);
			}
			bitmap.Save(destination, System.Drawing.Imaging.ImageFormat.Bmp);
			bitmap.Dispose();
			bsource.Dispose();
		}

		void OpenImage(string path)
		{
			if (File.Exists(ImagePath)) File.Delete(ImagePath);
			if (File.Exists(ImageResizedPath)) File.Delete(ImageResizedPath);
			if (File.Exists(PreviewPath)) File.Delete(PreviewPath);

			OpeningFileName = path;
			this.Title = "MozJPEG-GUI - " + Path.GetFileName(OpeningFileName);
			ImagePath = Path.GetTempPath() + Guid.NewGuid() + ".bmp";
			SaveBMPImage(path, ImagePath);
			ImageFileLength = (new FileInfo(path)).Length;
			Label_ImageInfo.Content = FormatFileSize(ImageFileLength) + " (Original)";

			ImageBitmap = new BitmapImage();
			ImageBitmap.BeginInit();
			ImageBitmap.UriSource = new Uri(ImagePath);
			ImageBitmap.CacheOption = BitmapCacheOption.OnLoad;
			ImageBitmap.EndInit();
			Image_PictureBox.Source = ImageBitmap;
			Image_PictureBox.Width = ImageBitmap.PixelWidth;
			Image_PictureBox.Height = ImageBitmap.PixelHeight;

			ImageFileSize.Width = ImageBitmap.PixelWidth;
			ImageFileSize.Height = ImageBitmap.PixelHeight;

			TextBox_Width.Text = ImageFileSize.Width.ToString();
			TextBox_Height.Text = ImageFileSize.Height.ToString();
			TextBox_Scale.Text = 100.ToString();

			CreateResizedImage();
            IgnoreLargeImageWarning = false;

			GC.Collect();
		}

        void EncodeImage(string source, string outFile) {
            string argument = "";
            string binaryPath = "";
            switch (ComboBox_Tool.SelectedIndex) 
            {
                case 0: // mozjpeg
                    binaryPath = CjpegPath;

                    string sample = "";
                    string image = " -optimize ";

                    switch (ComboBox_MozJPEG_SampleFormat.SelectedIndex)
                    {
                        case 0: // yuv420
                            sample = " -sample 2x2 ";
                            break;
                        case 1: // yuv422
                            sample = " -sample 2x1 ";
                            break;
                        case 2: // yuv444
                            sample = " -sample 1x1 ";
                            break;
                        case 3: // rgb
                            sample = " -rgb ";
                            break;
                    }
                    switch (ComboBox_MozJPEG_ImageFormat.SelectedIndex)
                    {
                        case 0: // baseline
                            image += "-baseline";
                            break;
                        case 1: // progressive grayscale
                            image += "-progressive -greyscale";
                            break;
                        case 2: // progressive non-interleaved
                            image += "-progressive -dc-scan-opt 0";
                            break;
                        case 3: // progressive interleaved
                            image += "-progressive -dc-scan-opt 2";
                            break;
                    }
                    argument = "-quality " + Slider_MozJPEG_Quality.Value.ToString() +
                               " -smooth " + Slider_MozJPEG_Smooth.Value +
                               image +
                               sample +
                               " -quant-table " + ComboBox_MozJPEG_QuantTable.SelectedIndex +
                               " -outfile \"" + outFile +
                               "\" \"" +
                               source + "\"";
                    break;
                case 1: // libwebp
                    binaryPath = CwebpPath;

                    string[] presets = { "default", "photo", "picture", "drawing", "icon", "text" };
                    if ((bool)CheckBox_libWebP_Lossless.IsChecked)
                    {
                        argument = "-q " + Slider_libWebP_Quality.Value.ToString() +
                                   " -m 6 -mt -quiet -lossless \"" + source + "\" -o \"" + outFile + "\"";
                    }
                    else if (ComboBox_libWebP_Preset.SelectedIndex != 0)
                    {
                        argument = "-q " + Slider_libWebP_Quality.Value.ToString() +
                                   " -preset " + presets[ComboBox_libWebP_Preset.SelectedIndex] +
                                   " -m 6 -mt -quiet \"" + source + "\" -o \"" + outFile + "\"";
                    }
                    else 
                    {
                        argument = "-q " + Slider_libWebP_Quality.Value.ToString() +
                                   " -f " + Slider_libWebP_Filter.Value.ToString() +
                                   ((bool)CheckBox_libWebP_LowMemory.IsChecked ? " -low_memory" : "") +
                                   ((bool)CheckBox_libWebP_SharpYUV.IsChecked ? " -sharp_yuv" : "") +
                                   " -m 6 -mt -quiet \"" + source + "\" -o \"" + outFile + "\"";
                    }
                    break;
            }

            Process process = new Process();
            process.StartInfo.FileName = binaryPath;
            process.StartInfo.Arguments = argument;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            // MessageBox.Show(argument);

            process.Start();
            process.WaitForExit();
            process.Close();
        }

        void PreviewImage() {
			if (File.Exists(PreviewPath)) File.Delete(PreviewPath);
            switch (ComboBox_Tool.SelectedIndex) 
            {
                case 0: // mozjpeg
			        PreviewPath = Path.GetTempPath() + Guid.NewGuid() + ".jpg";
                    break;
                case 1: // libwebp
                    PreviewPath = Path.GetTempPath() + Guid.NewGuid() + ".webp";
                    break;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            EncodeImage(ImageResizedPath, PreviewPath);
            stopwatch.Stop();

            PreviewBitmap = new BitmapImage();
            PreviewBitmap.BeginInit();
            // 使用webp编码则临时转码一张bmp，然后删除
            if (ComboBox_Tool.SelectedIndex == 1)
            {
                string decodedWebPImagePath = Path.GetTempPath() + Guid.NewGuid() + ".bmp";
                SaveBMPImage(PreviewPath, decodedWebPImagePath);
                PreviewBitmap.UriSource = new Uri(decodedWebPImagePath);
                PreviewBitmap.CacheOption = BitmapCacheOption.OnLoad;
                PreviewBitmap.EndInit();
                File.Delete(decodedWebPImagePath);
            }
            else 
            {
                PreviewBitmap.UriSource = new Uri(PreviewPath);
                PreviewBitmap.CacheOption = BitmapCacheOption.OnLoad;
                PreviewBitmap.EndInit();
            }

            Image_PictureBox.Source = PreviewBitmap;
            Image_PictureBox.Width = PreviewBitmap.PixelWidth;
            Image_PictureBox.Height = PreviewBitmap.PixelHeight;

            PreviewFileLength = new FileInfo(PreviewPath).Length;
            Label_ImageInfo.Content = FormatFileSize(ImageFileLength) + " → " + FormatFileSize(PreviewFileLength) + string.Format(" ({0:0.##}% {1}ms)", (double)PreviewFileLength / (double)ImageFileLength * 100, (int)stopwatch.Elapsed.TotalMilliseconds);
            GC.Collect();
		}

        void CreateResizedImage()
		{
			if (!File.Exists(ImagePath) || Convert.ToInt32(TextBox_Width.Text) == 0 || Convert.ToInt32(TextBox_Height.Text) == 0) return;
			if (File.Exists(ImageResizedPath)) File.Delete(ImageResizedPath);
			ImageResizedPath = Path.GetTempPath() + Guid.NewGuid() + ".bmp";
			System.Drawing.Drawing2D.InterpolationMode interpolation = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			if (ComboBox_Interpolation != null)
			{
				switch (ComboBox_Interpolation.SelectedIndex)
				{
					case 0:
						interpolation = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
						break;
					case 1:
						interpolation = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
						break;
					case 2:
						interpolation = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
						break;
				}
			}
			SaveResizedBMPImage(ImagePath, ImageResizedPath, interpolation, Convert.ToInt32(TextBox_Width.Text), Convert.ToInt32(TextBox_Height.Text));

			ImageResizedBitmap = new BitmapImage();
			ImageResizedBitmap.BeginInit();
			ImageResizedBitmap.UriSource = new Uri(ImageResizedPath);
			ImageResizedBitmap.CacheOption = BitmapCacheOption.OnLoad;
			ImageResizedBitmap.EndInit();
			Image_PictureBox.Source = ImageResizedBitmap;
			Image_PictureBox.Width = ImageResizedBitmap.PixelWidth;
			Image_PictureBox.Height = ImageResizedBitmap.PixelHeight;
		}

		void NumericCheckWithDot(object sender, TextCompositionEventArgs e) {
			if ((((TextBox)sender).Text.IndexOf('.') != -1 && e.Text.IndexOf('.') != -1) || !Regex.IsMatch(e.Text, "^[0-9.]+$")) e.Handled = true;
		}

		void NumericCheckWithoutDot(object sender, TextCompositionEventArgs e) {
			if (!Regex.IsMatch(e.Text, "[0-9]+$")) e.Handled = true;
		}

		private void Button_OpenImage_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog fileDialog = new OpenFileDialog
			{
				InitialDirectory = Directory.GetCurrentDirectory(),
				Filter = "Image Files(*.bmp;*.jpg;*.jpeg;*.png;*.gif;*.webp)|*.bmp;*.jpg;*.jpeg;*.png;*.gif;*.webp"
			};
			if (!(bool)fileDialog.ShowDialog()) return;

			OpenImage(fileDialog.FileName);
		}

		private void Button_SaveImage_Click(object sender, RoutedEventArgs e)
		{
			if (ImagePath == "") return;
			if (ImageResizedPath == "") CreateResizedImage();
			PreviewImage();

			SaveFileDialog fileDialog = new SaveFileDialog
			{
				InitialDirectory = Directory.GetCurrentDirectory(),
			};
            switch (ComboBox_Tool.SelectedIndex)
            {
                case 0: // mozjpeg
                    fileDialog.Filter = "Image Files(*.jpg)|*.jpg;";
                    fileDialog.FileName = Path.GetFileNameWithoutExtension(OpeningFileName) + ".jpg";
                    break;
                case 1: // libwebp
                    fileDialog.Filter = "Image Files(*.webp)|*.webp;";
                    fileDialog.FileName = Path.GetFileNameWithoutExtension(OpeningFileName) + ".webp";
                    break;
            }
            if (!(bool)fileDialog.ShowDialog()) return;
			File.WriteAllBytes(fileDialog.FileName, File.ReadAllBytes(PreviewPath));
		}

        private void Button_UploadImage_Click(object sender, RoutedEventArgs e)
        {
            if (ImagePath == "") return;
            if (ImageResizedPath == "") CreateResizedImage();
            PreviewImage();

            // string uploadPath = "";
            // switch (ComboBox_Tool.SelectedIndex)
            // {
            //     case 0: // mozjpeg
            //         uploadPath = PreviewPath;
            //         break;
            //     case 1: // libwebp
            //         uploadPath = Path.ChangeExtension(PreviewPath, ".jpg");
            //         File.Move(PreviewPath, uploadPath);
            //         break;
            // }

            Dictionary<string, object> postParam = new Dictionary<string, object>
            {
                { "scene", "productImageRule" },
                { "name", Path.GetFileName(Path.ChangeExtension(PreviewPath, ".jpg")) },
                { "file", new FormUpload.FileParameter(File.ReadAllBytes(PreviewPath)) }
            };

            HttpWebResponse response;
            try
            {
                response = FormUpload.MultipartFormDataPost(
                    "https://kfupload.alibaba.com/mupload",
                    "iAliexpress/6.22.1 (iPhone; iOS 12.1.2; Scale/2.00)",
                    postParam
                );
            }
            catch (Exception)
            {
                MessageBox.Show((string)TryFindResource("UnableToConnect"), "", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StreamReader reader = new StreamReader(response.GetResponseStream());
            string responseText = reader.ReadToEnd();
            reader.Dispose();

            JsonObject responseKeyValues = JsonValue.Parse(responseText) as JsonObject;

            if (responseKeyValues["code"] == 0)
            {
                Clipboard.SetText(responseKeyValues["url"]);
                MessageBox.Show(string.Format((string)TryFindResource("UploadSuccess"), (string)responseKeyValues["url"]), "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show((string)TryFindResource("UploadFailed"), "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Slider_MozJPEG_Quality_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
            Slider_MozJPEG_Quality.Value = (int)Slider_MozJPEG_Quality.Value;
		}

		private void Slider_MozJPEG_Smooth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
            Slider_MozJPEG_Smooth.Value = (int)Slider_MozJPEG_Smooth.Value;
		}

        private void Slider_libWebP_Quality_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider_libWebP_Quality.Value = (int)Slider_libWebP_Quality.Value;
        }

        private void Slider_libWebP_Filter_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider_libWebP_Filter.Value = (int)Slider_libWebP_Filter.Value;
        }

        private void ComboBox_PreviewScale_DropDownClosed(object sender, EventArgs e)
		{
			double PreviewScale = Convert.ToDouble(((ComboBoxItem)ComboBox_PreviewScale.SelectedItem).Content) / 100;
			Image_PictureBox.LayoutTransform = new ScaleTransform(PreviewScale, PreviewScale);
		}

		private void TextBox_Scale_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (TextBox_Scale.Text == "" || Convert.ToDouble(TextBox_Scale.Text) == 0) TextBox_Scale.Text = 100.ToString();
			TextBox_Width.Text = Math.Round(ImageFileSize.Width * Convert.ToDouble(TextBox_Scale.Text) / 100).ToString();
			TextBox_Height.Text = Math.Round(ImageFileSize.Height * Convert.ToDouble(TextBox_Scale.Text) / 100).ToString();
		}

		private void CheckBox_Ratio_Checked(object sender, RoutedEventArgs e)
		{
			if (TextBox_Scale != null)
			{
				TextBox_Scale.IsEnabled = (bool)CheckBox_Ratio.IsChecked;
				if (TextBox_Scale.IsEnabled) TextBox_Scale_TextChanged(sender, null);
			}
		}

		private void TextBox_Width_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (TextBox_Width.Text == "" || Convert.ToDouble(TextBox_Width.Text) == 0 ) TextBox_Width.Text = ImageFileSize.Width.ToString();
			if (CheckBox_Ratio != null && (bool)CheckBox_Ratio.IsChecked && TextBox_Width.IsFocused && ImageFileSize.Width != 0)
			{
				TextBox_Height.Text = Math.Round(Convert.ToDouble(TextBox_Width.Text) / ImageFileSize.Width * ImageFileSize.Height).ToString();
				TextBox_Scale.Text = Math.Round(Convert.ToDouble(TextBox_Width.Text) / ImageFileSize.Width * 100, 4).ToString();
			}
		}

		private void TextBox_Height_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (TextBox_Height.Text == "" || Convert.ToDouble(TextBox_Height.Text) == 0) TextBox_Height.Text = ImageFileSize.Height.ToString();
			if (CheckBox_Ratio != null && (bool)CheckBox_Ratio.IsChecked && TextBox_Height.IsFocused && ImageFileSize.Height != 0)
			{
				TextBox_Width.Text = Math.Round(Convert.ToDouble(TextBox_Height.Text) * ImageFileSize.Width / ImageFileSize.Height).ToString();
				TextBox_Scale.Text = Math.Round(Convert.ToDouble(TextBox_Height.Text) / ImageFileSize.Height * 100, 4).ToString();
			}
		}

		private void Button_Preview_Click(object sender, RoutedEventArgs e)
		{
			if (ImagePath == "") return;
            if (Convert.ToDouble(TextBox_Scale.Text) > 100)
            {
                if (MessageBox.Show((string)TryFindResource("UnnecessaryResizing"), this.Title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;
            }
            if ((Convert.ToInt32(TextBox_Width.Text) > 4000 || Convert.ToInt32(TextBox_Height.Text) > 4000) && !IgnoreLargeImageWarning)
			{
                if (MessageBox.Show((string)TryFindResource("LoadImageTooLarge"), this.Title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;
                IgnoreLargeImageWarning = true;
			}
			CreateResizedImage();
			PreviewImage();
		}

		private void Button_Source_Click(object sender, RoutedEventArgs e)
		{
            if (ImagePath == "") return;
			CreateResizedImage();
			Image_PictureBox.Source = ImageResizedBitmap;
			Label_ImageInfo.Content = FormatFileSize(ImageFileLength) + " (Original)";
		}

        private void Button_OpenDirectory_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog browserDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = (string)TryFindResource("BatchSelectFolder")
            };
            if (browserDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            DirectoryInfo directoryInfo = new DirectoryInfo(browserDialog.SelectedPath);
            string[] extension = { ".bmp", ".jpg", ".jpeg", ".png", ".gif", "*.webp" };
            int count = 0;
            foreach (FileInfo file in directoryInfo.GetFiles()) if (extension.Contains(file.Extension.ToLower())) count++;
            if (MessageBox.Show(string.Format((string)TryFindResource("BatchConfirm"), browserDialog.SelectedPath, count), this.Title, MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;

            Directory.CreateDirectory(browserDialog.SelectedPath + "\\mozjpeg-converted");

            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                if (!extension.Contains(file.Extension.ToLower())) continue;

                GC.Collect();

                string BatchImagePath = Path.GetTempPath() + Guid.NewGuid() + ".bmp";
                string BatchImageResizedPath = Path.GetTempPath() + Guid.NewGuid() + ".bmp";
                System.Drawing.Drawing2D.InterpolationMode interpolation = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                if (ComboBox_Interpolation != null)
                {
                    switch (ComboBox_Interpolation.SelectedIndex)
                    {
                        case 0:
                            interpolation = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            break;
                        case 1:
                            interpolation = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                            break;
                        case 2:
                            interpolation = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                            break;
                    }
                }

                SaveBMPImage(file.FullName, BatchImagePath);
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(BatchImagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                int width = bitmap.PixelWidth;
                int height = bitmap.PixelHeight;
                double ratio = (double)width / (double)height;
                switch (ComboBox_Batch.SelectedIndex)
                {
                    case 0:
                        width = (int)Math.Round(width * Convert.ToDouble(TextBox_Scale.Text) / 100);
                        height = (int)Math.Round(height * Convert.ToDouble(TextBox_Scale.Text) / 100);
                        break;
                    case 1:
                        width = Convert.ToInt32(TextBox_Width.Text);
                        height = (int)Math.Round(width / ratio);
                        break;
                    case 2:
                        height = Convert.ToInt32(TextBox_Height.Text);
                        width = (int)Math.Round(height * ratio);
                        break;
                }
                SaveResizedBMPImage(BatchImagePath, BatchImageResizedPath, interpolation, width, height);

                switch (ComboBox_Tool.SelectedIndex) 
                {
                    case 0: // mozjpeg
                        EncodeImage(BatchImageResizedPath, browserDialog.SelectedPath + "\\mozjpeg-converted\\" + Path.GetFileNameWithoutExtension(file.Name) + ".jpg");
                        break;
                    case 1: // libwebp
                        EncodeImage(BatchImageResizedPath, browserDialog.SelectedPath + "\\mozjpeg-converted\\" + Path.GetFileNameWithoutExtension(file.Name) + ".webp");
                        break;
                }

                File.Delete(BatchImagePath);
                File.Delete(BatchImageResizedPath);
            }

            MessageBox.Show((string)TryFindResource("BatchComplete"), this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            browserDialog.Dispose();
        }

        private void Button_Readme_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/TransparentLC/mozjpeg-gui");
        }

        private void ComboBox_Tool_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (ComboBox_Tool.SelectedIndex)
            {
                case 0: // mozjpeg
                    GroupBox_libWebP_Config.Visibility = Visibility.Collapsed;
                    GroupBox_MozJPEG_Config.Visibility = Visibility.Visible;
                    break;
                case 1: // libwebp
                    GroupBox_MozJPEG_Config.Visibility = Visibility.Collapsed;
                    GroupBox_libWebP_Config.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void Button_Language_Click(object sender, RoutedEventArgs e)
        {
            switch (CurrentLanguage) 
            {
                case "zh-CN":
                    CurrentLanguage = "en-US";
                    break;
                case "en-US":
                    CurrentLanguage = "zh-CN";
                    break;
            }
            ResourceDictionary rd = new ResourceDictionary
            {
                Source = new Uri(@"Language." + CurrentLanguage + ".xaml", UriKind.Relative)
            };
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(rd);
        }
    }
}