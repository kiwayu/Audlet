namespace Audlet.Library;

// Decodes an image and re-encodes it as a JPEG at quality 85, scaled down so its longer side is at most maxSide.
// Each method throws when the image cannot be decoded. The app's is WpfCoverCodec.
public interface ICoverCodec
{
    byte[] ToJpeg(byte[] image, int maxSide);
    byte[] ToJpeg(string imagePath, int maxSide);
    byte[] ToJpegScaled(byte[] image, int maxSide);   // decodes straight to maxSide wide
}
