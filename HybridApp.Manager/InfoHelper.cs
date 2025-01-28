using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Data;

namespace HybridApp.Manager
{
    public class InfoHelper
    {
        public static string FixURLValidity(string url)
        {
            if (!url.StartsWith("http"))
            {
                url = $"https://{url}";
            }
            return url;
        }

        public static async Task<bool> ProcessWebsiteIcon(string url, string tmpDir, bool useGoogleSource = false)
        {
            WebView2 webView = new WebView2();
            await webView.EnsureCoreWebView2Async(null);

            bool result = true;

            try
            {
                var tcs = new TaskCompletionSource<bool>();
                bool navSuccess = false;

                webView.NavigationCompleted += (s, e) =>
                {
                    if (e.IsSuccess)
                    {
                        navSuccess = true;
                    }
                    tcs.SetResult(true);
                };

                webView.CoreWebView2.Navigate(url);
                await tcs.Task;

                if (!navSuccess)
                {
                    return false;
                }

                string faviconUrl = await webView.ExecuteScriptAsync("document.querySelector(\"link[rel='icon']\") ? document.querySelector(\"link[rel='icon']\").href : ''");

                if (useGoogleSource)
                {
                    result = await GetIconGoogle(url, tmpDir);
                }
                else if ((faviconUrl = faviconUrl.Trim('"')) != "")
                {
                    Uri faviconUri = new Uri(faviconUrl);
                    using HttpClient client = new HttpClient();
                    var response = await client.GetAsync(faviconUri);

                    if (response.IsSuccessStatusCode)
                    {
                        byte[] faviconData = await response.Content.ReadAsByteArrayAsync();

                        using (var inputStream = new MemoryStream(faviconData))
                        {
                            var bitmap = SKBitmap.Decode(inputStream);

                            if (bitmap == null)
                            {
                                result = await GetIconGoogle(url, tmpDir);
                            }
                            else
                            {
                                string outputFilePath = Path.Combine(tmpDir, "favicon.ico");

                                SKImage image = SKImage.FromBitmap(bitmap);
                                EncodeIcoFile(outputFilePath, image);
                            }
                        }
                    }
                }
                else
                {
                    result = await GetIconGoogle(url, tmpDir);
                }
            }
            catch
            {
                result = false;
            }
            finally
            {
                webView?.CoreWebView2?.Stop();
            }
            return result;
        }

        public static async Task<bool> GetIconGoogle(string url, string tmpDir)
        {
            try
            {
                Uri faviconUri = new Uri($"https://www.google.com/s2/favicons?domain={url}&size=256");
                using HttpClient client = new HttpClient();
                var response = await client.GetAsync(faviconUri);

                if (response.IsSuccessStatusCode)
                {
                    byte[] faviconData = await response.Content.ReadAsByteArrayAsync();
                    using (var inputStream = new MemoryStream(faviconData))
                    {
                        var bitmap = SKBitmap.Decode(inputStream);

                        if (bitmap == null)
                        {
                            throw new Exception();
                        }

                        string outputFilePath = Path.Combine(tmpDir, "favicon.ico");

                        SKImage image = SKImage.FromBitmap(bitmap);
                        EncodeIcoFile(outputFilePath, image);
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static void EncodeIcoFile(string path, SKImage img)
        {
            using (SKBitmap originalBitmap = SKBitmap.FromImage(img))
            {
                Bitmap bmp = ConvertSKBitmapToSystemDrawingBitmap(originalBitmap);
                SaveAsIcon(bmp, path);
            }
        }

        public static Bitmap ConvertSKBitmapToSystemDrawingBitmap(SKBitmap skBitmap)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                skBitmap.Encode(SKEncodedImageFormat.Png, 100).SaveTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
                return new System.Drawing.Bitmap(ms);
            }
        }

        public static void SaveAsIcon(Bitmap SourceBitmap, string FilePath)
        {
            FileStream FS = new FileStream(FilePath, FileMode.Create);
            FS.WriteByte(0); FS.WriteByte(0);
            FS.WriteByte(1); FS.WriteByte(0);
            FS.WriteByte(1); FS.WriteByte(0);

            FS.WriteByte((byte)SourceBitmap.Width);
            FS.WriteByte((byte)SourceBitmap.Height);
            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(0); FS.WriteByte(0);
            FS.WriteByte(32); FS.WriteByte(0);

            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(0);

            FS.WriteByte(22);
            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(0);

            SourceBitmap.Save(FS, ImageFormat.Png);

            long Len = FS.Length - 22;

            FS.Seek(14, SeekOrigin.Begin);
            FS.WriteByte((byte)Len);
            FS.WriteByte((byte)(Len >> 8));

            FS.Close();
        }

        public static void CopyToIcon(string path, string target) 
        {
            using (FileStream s = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var bitmap = SKBitmap.Decode(s);
                Bitmap bmp = ConvertSKBitmapToSystemDrawingBitmap(bitmap);
                SaveAsIcon(bmp, target);
            }
        }

        public static T FindChildElement<T>(DependencyObject parent, string childName) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T frameworkElement && frameworkElement.Name == childName)
                {
                    return frameworkElement;
                }

                var result = FindChildElement<T>(child, childName);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        public static RadioButtons GetRadioButtonsInItemsRepeater(ItemsRepeater repeater, int index)
        {
            var element = repeater.TryGetElement(index) as ContentControl;
            if (element != null)
            {
                var templateRoot = element.ContentTemplateRoot;
                return FindChildElement<RadioButtons>(templateRoot, "IconTypeSelection");
            }
            return null;
        }

        public static BitmapImage CreateBitmapImage(Uri uri) 
        {
            var bi = new BitmapImage();
            bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bi.UriSource = uri;
            return bi;
        }
    }

    public class IgnoreCacheConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string uriString && !string.IsNullOrWhiteSpace(uriString))
            {
                var bitmapImage = new BitmapImage
                {
                    CreateOptions = BitmapCreateOptions.IgnoreImageCache,
                    UriSource = new Uri(uriString)
                };
                return bitmapImage;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool visible)
            {
                return visible ? Visibility.Visible : Visibility.Collapsed;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
