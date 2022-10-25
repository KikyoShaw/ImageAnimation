using ImageParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfAnimation.ImageAnimationHelp
{
    public struct KeyFrame
    {
        /// <summary>
        /// 当前帧的bitmap
        /// </summary>
        public BitmapSource AFrame;
        /// <summary>
        /// 当前帧距离前一帧的间隔，单位-毫秒，第一帧的时候是 0
        /// </summary>
        public double Duration;
    }
    public class Loader
    {
        /// <summary>
        /// 读取webp单图文件到Bitmapsource
        /// 解码宽度和高度只设置一个的时候按原图比例缩放，两个都设置打破原图比例
        /// </summary>
        /// <param name="localPath">本地路径</param>
        /// <param name="decodeWidth">解码宽度</param>
        /// <param name="decodeHeight">解码高度</param>
        /// <returns>成功返回 wpf BitmapSource，失败返回 null</returns>
        public static BitmapSource GetWebpFrameByLocalPath(string localPath, int decodeWidth = 0, int decodeHeight = 0)
        {
            using var webp = new ImageParser.Webp();
            var pr = webp.ParseWebpFile(localPath, decodeWidth, decodeHeight);
            if (!pr)
                return null;

            return GetFirstFrameInner(webp);
        }

        public static byte[] GetWebpByteByLocalPath(string localPath, int decodeWidth = 0, int decodeHeight = 0)
        {
            using var webp = new ImageParser.Webp();
            var pr = webp.ParseWebpFile(localPath, decodeWidth, decodeHeight);
            if (!pr)
                return null;

            //return GetFirstFrameInnerByte(webp);
            return GetFirstFrameInnerByte2(webp);
        }

        /// <summary>
        /// 读取webp内存数据到Bitmapsource
        /// 解码宽度和高度只设置一个的时候按原图比例缩放，两个都设置打破原图比例
        /// </summary>
        /// <param name="memBytes">内存数据</param>
        /// <param name="decodeWidth">解码宽度</param>
        /// <param name="decodeHeight">解码高度</param>
        /// <returns>成功返回 wpf BitmapSource，失败返回 null</returns>
        public static BitmapSource GetWebpFrameByMemBytes(byte[] memBytes, int decodeWidth = 0, int decodeHeight = 0)
        {
            using var webp = new ImageParser.Webp();
            var pr = webp.ParseWebpMemBytes(memBytes, decodeWidth, decodeHeight);
            if (!pr)
                return null;

            return GetFirstFrameInner(webp);
        }

        /// <summary>
        /// 读取webp或gif动画
        /// </summary>
        /// <param name="localPath">本地路径</param>
        /// <returns>动画的序列帧</returns>
        public static List<KeyFrame> GetKeyFramesByLocalPath(string localPath)
        {
            var frames = new List<KeyFrame>();
            using var ani = new ImageParser.Animation();

            var pr = ani.ParseAnimationFrames(localPath);
            if (!pr)
                return frames;

            GetKeyFramesInner(ani, ref frames);

            return frames;
        }

        /// <summary>
        /// 读取webp或gif动画
        /// </summary>
        /// <param name="localPath">内存数据</param>
        /// <returns>动画的序列帧</returns>
        public static List<KeyFrame> GetKeyFramesByMemBytes(byte[] memBytes)
        {
            var frames = new List<KeyFrame>();
            using var ani = new ImageParser.Animation();
            var pr = ani.ParseAnimationFrames(memBytes);
            if (!pr)
                return frames;

            GetKeyFramesInner(ani, ref frames);

            return frames;
        }

        private static void GetKeyFramesInner(ImageParser.Animation ani, ref List<KeyFrame> frames)
        {
            var index = 0;
            var count = ani.GetFrameCount();
            var height = ani.GetCavHeight();
            var width = ani.GetCavWidth();
            var pf = ani.HasAlpha() ? PixelFormats.Bgra32 : PixelFormats.Bgr32;
            var rawStride = (width * pf.BitsPerPixel + 7) / 8;
            for (;index < count; ++index)
            {
                if (!ani.GetFrameAt(index, out var dataPtr, out var dataSize, out var duration))
                    continue;

                var bitmap = BitmapSource.Create(width, height, 96, 96, pf, null, dataPtr, dataSize, rawStride);
                frames.Add(new KeyFrame
                {
                    AFrame = bitmap,
                    Duration = duration
                });
            }
        }

        private static BitmapSource GetFirstFrameInner(ImageParser.Webp webp)
        {
            if (!webp.GetOk())
                return null;

            var height = webp.GetCavHeight();
            var width = webp.GetCavWidth();
            var pf = webp.HasAlpha() ? PixelFormats.Bgra32 : PixelFormats.Bgr32;
            var rawStride = (width * pf.BitsPerPixel + 7) / 8;
            if (webp.GetFrameData(out var dataPtr, out var dataSize))
            {
                return BitmapSource.Create(width, height, 96, 96, pf, null, dataPtr, dataSize, rawStride);
            }

            return null;
        }

        private static byte[] GetFirstFrameInnerByte(ImageParser.Webp webp)
        {
            if (!webp.GetOk())
                return null;

            if (webp.GetFrameData(out var dataPtr, out var dataSize))
            {
                var temp = new byte[dataSize];
                Marshal.Copy(dataPtr, temp, 0, dataSize);
                return temp;
            }

            return null;
        }

        private static byte[] GetFirstFrameInnerByte2(ImageParser.Webp webp)
        {
            if (!webp.GetOk())
                return null;

            var height = webp.GetCavHeight();
            var width = webp.GetCavWidth();
            var pf = webp.HasAlpha() ? PixelFormats.Bgra32 : PixelFormats.Bgr32;
            var rawStride = (width * pf.BitsPerPixel + 7) / 8;
            if (webp.GetFrameData(out var dataPtr, out var dataSize))
            {
                var temp = BitmapSource.Create(width, height, 96, 96, pf, null, dataPtr, dataSize, rawStride);
                return ConvertToBytes(temp);
            }

            return null;
        }

        private static byte[] ConvertToBytes(BitmapSource bitmapSource)
        {
            byte[] buffer = null;
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            MemoryStream memoryStream = new MemoryStream();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memoryStream);
            memoryStream.Position = 0;
            if (memoryStream.Length > 0)
            {
                using (BinaryReader br = new BinaryReader(memoryStream))
                {
                    buffer = br.ReadBytes((int)memoryStream.Length);
                }
            }
            memoryStream.Close();
            return buffer;
        }
    }
}
