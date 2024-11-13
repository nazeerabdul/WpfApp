using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace SprayPaintApp
{
    public partial class MainWindow : Window
    {
        private WriteableBitmap bitmap;      // Bitmap where paint modifications are applied
        private BitmapImage originalImage;   // Original image loaded for reference during erasing
        private bool isSpraying = false;     // Tracks if spray painting is active
        private bool isErasing = false;      // Tracks if eraser mode is active
        private Color sprayColor = Colors.Black; // Default spray color
        private Random random = new Random(); // Random generator for spray effect

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Filter = "Image files (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp" };
            if (openFileDialog.ShowDialog() == true)
            {
                originalImage = new BitmapImage(new Uri(openFileDialog.FileName));
                bitmap = new WriteableBitmap(originalImage);
                DisplayedImage.Source = bitmap;
            }
        }

        private void SaveImageButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog { Filter = "PNG Image|*.png" };
            if (saveFileDialog.ShowDialog() == true)
            {
                using (var fileStream = new System.IO.FileStream(saveFileDialog.FileName, System.IO.FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(fileStream);
                }
            }
        }

        private void ColorPicker_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                sprayColor = Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
            }
        }

        private void ToggleEraseMode_Click(object sender, RoutedEventArgs e)
        {
            isErasing = !isErasing;
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => isSpraying = true;
        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => isSpraying = false;

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (isSpraying && bitmap != null)
            {
                var position = e.GetPosition(DisplayedImage);
                if (isErasing)
                    EraseAtPosition(position);
                else
                    SprayAtPosition(position);
            }
        }

        private void SprayAtPosition(Point position)
        {
            int centerX = (int)(position.X * bitmap.PixelWidth / DisplayedImage.ActualWidth);
            int centerY = (int)(position.Y * bitmap.PixelHeight / DisplayedImage.ActualHeight);
            int sprayRadius = 10;  // Fixed spray radius
            int sprayDensity = 30; // Fixed spray density
            byte alpha = 255;      // Fully opaque spray

            bitmap.Lock();
            try
            {
                for (int i = 0; i < sprayDensity; i++)
                {
                    double angle = random.NextDouble() * Math.PI * 2;
                    double radius = random.NextDouble() * sprayRadius;
                    int x = centerX + (int)(radius * Math.Cos(angle));
                    int y = centerY + (int)(radius * Math.Sin(angle));

                    if (x >= 0 && x < bitmap.PixelWidth && y >= 0 && y < bitmap.PixelHeight)
                    {
                        Color currentSprayColor = Color.FromArgb(alpha, sprayColor.R, sprayColor.G, sprayColor.B);
                        byte[] colorData = { currentSprayColor.B, currentSprayColor.G, currentSprayColor.R, currentSprayColor.A };

                        Int32Rect rect = new Int32Rect(x, y, 1, 1);
                        bitmap.WritePixels(rect, colorData, 4, 0);
                    }
                }
                bitmap.AddDirtyRect(new Int32Rect(centerX - sprayRadius, centerY - sprayRadius, sprayRadius * 2, sprayRadius * 2));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while spraying: {ex.Message}", "Spray Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                bitmap.Unlock();
            }
        }

        private void EraseAtPosition(Point position)
        {
            int centerX = (int)(position.X * bitmap.PixelWidth / DisplayedImage.ActualWidth);
            int centerY = (int)(position.Y * bitmap.PixelHeight / DisplayedImage.ActualHeight);
            int sprayRadius = 10;  // Fixed spray radius

            bitmap.Lock();
            try
            {
                for (int i = 0; i < 30; i++)
                {
                    double angle = random.NextDouble() * Math.PI * 2;
                    double radius = random.NextDouble() * sprayRadius;
                    int x = centerX + (int)(radius * Math.Cos(angle));
                    int y = centerY + (int)(radius * Math.Sin(angle));

                    if (x >= 0 && x < bitmap.PixelWidth && y >= 0 && y < bitmap.PixelHeight)
                    {
                        byte[] originalColor = new byte[4];
                        originalImage.CopyPixels(new Int32Rect(x, y, 1, 1), originalColor, 4, 0);
                        Int32Rect rect = new Int32Rect(x, y, 1, 1);
                        bitmap.WritePixels(rect, originalColor, 4, 0);
                    }
                }
                bitmap.AddDirtyRect(new Int32Rect(centerX - sprayRadius, centerY - sprayRadius, sprayRadius * 2, sprayRadius * 2));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while erasing: {ex.Message}", "Erase Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                bitmap.Unlock();
            }
        }
    }
}
