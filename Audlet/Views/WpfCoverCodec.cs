using System.Windows.Media;
using System.Windows.Media.Imaging;
using Audlet.Library;

namespace Audlet.Views;

public sealed class WpfCoverCodec : ICoverCodec
{
    public byte[] ToJpeg(byte[] image, int maxSide) =>
        Encode(BitmapDecoder.Create(new MemoryStream(image), BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0], maxSide);

    public byte[] ToJpeg(string imagePath, int maxSide) =>
        Encode(BitmapDecoder.Create(new Uri(imagePath), BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0], maxSide);

    public byte[] ToJpegScaled(byte[] image, int maxSide)
    {
        var img = new BitmapImage();
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.StreamSource = new MemoryStream(image);
        img.DecodePixelWidth = maxSide;
        img.EndInit();
        img.Freeze();
        return Encode(img, maxSide);
    }

    private static byte[] Encode(BitmapSource frame, int maxSide)
    {
        var scale = Math.Min(1.0, (double)maxSide / Math.Max(frame.PixelWidth, frame.PixelHeight));
        BitmapSource src = scale < 1.0 ? new TransformedBitmap(frame, new ScaleTransform(scale, scale)) : frame;
        var enc = new JpegBitmapEncoder { QualityLevel = 85 };
        enc.Frames.Add(BitmapFrame.Create(src));
        using var ms = new MemoryStream();
        enc.Save(ms);
        return ms.ToArray();
    }
}
