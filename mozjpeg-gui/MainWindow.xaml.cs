using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Size = System.Windows.Size;

namespace mozjpeg_gui
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{
		public string CjpegPath = "";
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

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			CjpegPath = Path.GetTempPath() + "tmp" + GetRandomString(4) + ".exe";
			File.WriteAllBytes(CjpegPath, mozjpeg_gui.Properties.Resources.cjpeg);
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			File.Delete(CjpegPath);
			if (File.Exists(ImagePath))
			{
				File.Delete(ImagePath);
			}
			if (File.Exists(ImageResizedPath))
			{
				File.Delete(ImageResizedPath);
			}
			if (File.Exists(PreviewPath))
			{
				File.Delete(PreviewPath);
			}
		}

		private void Window_Drop(object sender, DragEventArgs e)
		{
			string[] extension = { ".bmp", ".jpg", ".png", ".gif" };
			if (extension.Contains(Path.GetExtension(((string[])e.Data.GetData(DataFormats.FileDrop))[0]).ToLower()))
			{
				OpenImage(((string[])e.Data.GetData(DataFormats.FileDrop))[0]);
			}
			else
			{
				MessageBox.Show("不支持的文件格式。", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		string GetRandomString(int length, string charset = "0123456789ABCDEF")
		{
			string output = "";
            Random random = new Random(Guid.NewGuid().GetHashCode());
			for (int i = 0; i < length; i++)
			{
				output += charset[random.Next(0, charset.Length)];
			}
			return output;
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
			BitmapEncoder encoder = new BmpBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(new Uri(source)));

			using (var fileStream = new System.IO.FileStream(destination, FileMode.Create))
			{
				encoder.Save(fileStream);
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
			if (File.Exists(ImagePath))
			{
				File.Delete(ImagePath);
			}
			if (File.Exists(ImageResizedPath))
			{
				File.Delete(ImageResizedPath);
			}
			if (File.Exists(PreviewPath))
			{
				File.Delete(PreviewPath);
			}

			OpeningFileName = path;
			this.Title = "MozJPEG-GUI - " + Path.GetFileName(OpeningFileName);
			ImagePath = Path.GetTempPath() + "tmp" + GetRandomString(4) + ".bmp";
			SaveBMPImage(path, ImagePath);
			ImageFileLength = (new FileInfo(path)).Length;
			Label_ImageInfo.Content = FormatFileSize(ImageFileLength) + " (原图)";

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

		void PreviewImage() {
			if (File.Exists(PreviewPath))
			{
				File.Delete(PreviewPath);
			}
			PreviewPath = Path.GetTempPath() + "tmp" + GetRandomString(4) + ".jpg";

			string sample = "";
			switch (ComboBox_SampleFormat.SelectedIndex)
			{
				case 0:
					sample = " -sample 2x2 ";
					break;
				case 1:
					sample = " -sample 2x1 ";
					break;
				case 2:
					sample = " -sample 1x1 ";
					break;
				case 3:
					sample = " -rgb ";
					break;
			}
			Process process = new Process();
            process.StartInfo.FileName = CjpegPath;
            process.StartInfo.Arguments = "-quality " + Slider_Quality.Value.ToString() +
									" -smooth " +Slider_Smooth.Value +
									((bool)CheckBox_Progressive.IsChecked ? " -progressive " : " -baseline ") +
									((bool)CheckBox_Grayscale.IsChecked ? " -grayscale " : sample) +
									" -outfile \"" + PreviewPath +
									"\" \"" +
									ImageResizedPath + "\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            process.Start();
            process.WaitForExit();
            process.Close();
            stopwatch.Stop();

			PreviewBitmap = new BitmapImage();
			PreviewBitmap.BeginInit();
			PreviewBitmap.UriSource = new Uri(PreviewPath);
			PreviewBitmap.CacheOption = BitmapCacheOption.OnLoad;
			PreviewBitmap.EndInit();
			Image_PictureBox.Source = PreviewBitmap;
			Image_PictureBox.Width = PreviewBitmap.PixelWidth;
			Image_PictureBox.Height = PreviewBitmap.PixelHeight;

			PreviewFileLength = new FileInfo(PreviewPath).Length;
			Label_ImageInfo.Content = FormatFileSize(ImageFileLength) + " → " + FormatFileSize(PreviewFileLength) + string.Format(" ({0:0.##}% {1}ms)", (double)PreviewFileLength / (double)ImageFileLength * 100, (int)stopwatch.Elapsed.TotalMilliseconds);
			GC.Collect();
		}

		void CreateResizedImage()
		{
			if (!File.Exists(ImagePath) || Convert.ToInt32(TextBox_Width.Text) == 0 || Convert.ToInt32(TextBox_Height.Text) == 0)
			{
				return;
			}
			if (File.Exists(ImageResizedPath))
			{
				File.Delete(ImageResizedPath);
			}
			ImageResizedPath = Path.GetTempPath() + "tmp" + GetRandomString(4) + ".bmp";
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
			if ((((TextBox)sender).Text.IndexOf('.') != -1 && e.Text.IndexOf('.') != -1) || !Regex.IsMatch(e.Text, "^[0-9.]+$"))
			{
				e.Handled = true;
			}
		}

		void NumericCheckWithoutDot(object sender, TextCompositionEventArgs e) {
			if (!Regex.IsMatch(e.Text, "[0-9]+$"))
			{
				e.Handled = true;
			}
		}

		private void Button_OpenImage_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog fileDialog = new OpenFileDialog
			{
				InitialDirectory = Directory.GetCurrentDirectory(),
				Filter = "Image Files(*.bmp;*.jpg;*.png;*.gif)|*.bmp;*.jpg;*.png;*.gif"
			};
			if (!(bool)fileDialog.ShowDialog())
			{
				return;
			}

			OpenImage(fileDialog.FileName);
		}

		private void Button_SaveImage_Click(object sender, RoutedEventArgs e)
		{
			if (ImagePath == "")
			{
				return;
			}
			if (ImageResizedPath == "")
			{
				CreateResizedImage();
			}
			PreviewImage();
			SaveFileDialog fileDialog = new SaveFileDialog
			{
				InitialDirectory = Directory.GetCurrentDirectory(),
				Filter = "Image Files(*.jpg)|*.jpg;",
				FileName = Path.GetFileNameWithoutExtension(OpeningFileName) + ".jpg"
			};
			if (!(bool)fileDialog.ShowDialog())
			{
				return;
			}
			File.WriteAllBytes(fileDialog.FileName, File.ReadAllBytes(PreviewPath));
		}

		private void Slider_Quality_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Slider_Quality.Value = (int)Slider_Quality.Value;
		}

		private void Slider_Smooth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Slider_Smooth.Value = (int)Slider_Smooth.Value;
		}

		private void ComboBox_PreviewScale_DropDownClosed(object sender, EventArgs e)
		{
			double PreviewScale = Convert.ToDouble(((ComboBoxItem)ComboBox_PreviewScale.SelectedItem).Content) / 100;
			Image_PictureBox.LayoutTransform = new ScaleTransform(PreviewScale, PreviewScale);
		}

		private void TextBox_Scale_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (TextBox_Scale.Text == "" || Convert.ToDouble(TextBox_Scale.Text) == 0)
			{
				TextBox_Scale.Text = 100.ToString();
			}
			TextBox_Width.Text = Math.Round(ImageFileSize.Width * Convert.ToDouble(TextBox_Scale.Text) / 100).ToString();
			TextBox_Height.Text = Math.Round(ImageFileSize.Height * Convert.ToDouble(TextBox_Scale.Text) / 100).ToString();
		}

		private void CheckBox_Ratio_Checked(object sender, RoutedEventArgs e)
		{
			if (TextBox_Scale != null)
			{
				TextBox_Scale.IsEnabled = (bool)CheckBox_Ratio.IsChecked;
				if (TextBox_Scale.IsEnabled)
				{
					TextBox_Scale_TextChanged(sender, null);
				}
			}
		}

		private void TextBox_Width_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (TextBox_Width.Text == "" || Convert.ToDouble(TextBox_Width.Text) == 0 )
			{
				TextBox_Width.Text = ImageFileSize.Width.ToString();
			}
			if (CheckBox_Ratio != null && (bool)CheckBox_Ratio.IsChecked && TextBox_Width.IsFocused && ImageFileSize.Width != 0)
			{
				TextBox_Height.Text = Math.Round(Convert.ToDouble(TextBox_Width.Text) / ImageFileSize.Width * ImageFileSize.Height).ToString();
				TextBox_Scale.Text = Math.Round(Convert.ToDouble(TextBox_Width.Text) / ImageFileSize.Width * 100, 4).ToString();
			}
		}

		private void TextBox_Height_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (TextBox_Height.Text == "" || Convert.ToDouble(TextBox_Height.Text) == 0)
			{
				TextBox_Height.Text = ImageFileSize.Height.ToString();
			}
			if (CheckBox_Ratio != null && (bool)CheckBox_Ratio.IsChecked && TextBox_Height.IsFocused && ImageFileSize.Height != 0)
			{
				TextBox_Width.Text = Math.Round(Convert.ToDouble(TextBox_Height.Text) * ImageFileSize.Width / ImageFileSize.Height).ToString();
				TextBox_Scale.Text = Math.Round(Convert.ToDouble(TextBox_Height.Text) / ImageFileSize.Height * 100, 4).ToString();
			}
		}

		private void CheckBox_Grayscale_Checked(object sender, RoutedEventArgs e)
		{
			ComboBox_SampleFormat.IsEnabled = !(bool)CheckBox_Grayscale.IsChecked;
		}

		private void Button_Preview_Click(object sender, RoutedEventArgs e)
		{
			if (ImagePath == "")
			{
				return;
			}
            if (Convert.ToDouble(TextBox_Scale.Text) > 100)
            {
                if (MessageBox.Show("图片尺寸大于原图，会进行不必要的缩放，使图片清晰度降低，确定要继续吗？", this.Title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    return;
                }
            }
            if ((Convert.ToInt32(TextBox_Width.Text) > 4000 || Convert.ToInt32(TextBox_Height.Text) > 4000) && !IgnoreLargeImageWarning)
			{
				if (MessageBox.Show("图片尺寸较大，预览将会使程序卡住一段时间，确定要继续吗？\n（选择“是”后将不再提示）", this.Title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
				{
					return;
				}
                IgnoreLargeImageWarning = true;
			}
			CreateResizedImage();
			PreviewImage();
		}

		private void Button_Source_Click(object sender, RoutedEventArgs e)
		{
			if (ImagePath == "")
			{
				return;
			}
			CreateResizedImage();
			Image_PictureBox.Source = ImageResizedBitmap;
			Label_ImageInfo.Content = FormatFileSize(ImageFileLength) + " (原图)";
		}

        private void Button_OpenDirectory_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog browserDialog = new System.Windows.Forms.FolderBrowserDialog();
            browserDialog.Description = "选择要进行批处理的文件夹。";
            if (browserDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(browserDialog.SelectedPath);
            string[] extension = { ".bmp", ".jpg", ".png", ".gif" };
            int count = 0;
            foreach (FileInfo file in directoryInfo.GetFiles()) if (extension.Contains(file.Extension.ToLower())) count++;
            if (MessageBox.Show(string.Format("将对{0}的{1}张图片进行批量转换，准备好了吗？", browserDialog.SelectedPath, count), this.Title, MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;

            Directory.CreateDirectory(browserDialog.SelectedPath + "\\mozjpeg-converted");

            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                if (!extension.Contains(file.Extension.ToLower())) continue;

                GC.Collect();

                string BatchImagePath = Path.GetTempPath() + "tmp" + GetRandomString(4) + ".bmp";
                string BatchImageResizedPath = Path.GetTempPath() + "tmp" + GetRandomString(4) + ".bmp";
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

                string sample = "";
                switch (ComboBox_SampleFormat.SelectedIndex)
                {
                    case 0:
                        sample = " -sample 2x2 ";
                        break;
                    case 1:
                        sample = " -sample 2x1 ";
                        break;
                    case 2:
                        sample = " -sample 1x1 ";
                        break;
                    case 3:
                        sample = " -rgb ";
                        break;
                }
                Process process = new Process();
                process.StartInfo.FileName = CjpegPath;
                process.StartInfo.Arguments = "-quality " + Slider_Quality.Value.ToString() +
                                        " -smooth " + Slider_Smooth.Value +
                                        ((bool)CheckBox_Progressive.IsChecked ? " -progressive " : " -baseline ") +
                                        ((bool)CheckBox_Grayscale.IsChecked ? " -grayscale " : sample) +
                                        " -outfile \"" + browserDialog.SelectedPath + "\\mozjpeg-converted\\" + Path.GetFileNameWithoutExtension(file.Name) +
                                        ".jpg\" \"" +
                                        BatchImageResizedPath + "\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                process.WaitForExit();
                process.Close();

                File.Delete(BatchImagePath);
                File.Delete(BatchImageResizedPath);
            }

            MessageBox.Show("批量转换完成！\n转换后的文件保存在转换目录的mozjpeg-converted文件夹中。", this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Label_Title_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://github.com/TransparentLC/mozjpeg-gui");
        }
    }
}