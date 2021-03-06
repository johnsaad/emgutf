﻿//----------------------------------------------------------------------------
//  Copyright (C) 2004-2018 by EMGU Corporation. All rights reserved.       
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using Emgu.TF.Util.TypeEnum;

#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE
using UnityEngine;
#elif __ANDROID__
using Android.Graphics;
#elif __IOS__
using CoreGraphics;
using UIKit;
#elif __UNIFIED__
using AppKit;
using CoreGraphics;
#endif

namespace Emgu.Models
{
    /// <summary>
    /// Platform specific implementation of Image IO
    /// </summary>
    public class NativeImageIO
    {
        public static void ReadImageFileToTensor<T>(
            String fileName, 
            IntPtr dest, 
            int inputHeight = -1, 
            int inputWidth = -1, 
            float inputMean = 0.0f, 
            float scale = 1.0f)
        {
#if __ANDROID__
            Android.Graphics.Bitmap bmp = BitmapFactory.DecodeFile(fileName);

            if (inputHeight > 0 || inputWidth >  0)
            {
                Bitmap resized = Bitmap.CreateScaledBitmap(bmp, inputWidth, inputHeight, false);
                bmp.Dispose();
                bmp = resized;
            }
            int[] intValues = new int[bmp.Width * bmp.Height];
            float[] floatValues = new float[bmp.Width * bmp.Height * 3];
            bmp.GetPixels(intValues, 0, bmp.Width, 0, 0, bmp.Width, bmp.Height);
            for (int i = 0; i < intValues.Length; ++i)
            {
                int val = intValues[i];
                floatValues[i * 3 + 0] = (((val >> 16) & 0xFF) - inputMean) * scale;
                floatValues[i * 3 + 1] = (((val >> 8) & 0xFF) - inputMean) * scale;
                floatValues[i * 3 + 2] = ((val & 0xFF) - inputMean) * scale;
            }

            Marshal.Copy(floatValues, 0, dest, floatValues.Length);

#elif __IOS__
            UIImage image = new UIImage(fileName);
			if (inputHeight > 0 || inputWidth > 0)
			{
                UIImage resized = image.Scale(new CGSize(inputWidth, inputHeight));
                image.Dispose();
				image = resized;
			}
            int[] intValues = new int[(int) (image.Size.Width * image.Size.Height)];
            float[] floatValues = new float[(int) (image.Size.Width * image.Size.Height * 3)];
            System.Runtime.InteropServices.GCHandle handle = System.Runtime.InteropServices.GCHandle.Alloc(intValues, System.Runtime.InteropServices.GCHandleType.Pinned);
            using (CGImage cgimage = image.CGImage)
            using (CGColorSpace cspace = CGColorSpace.CreateDeviceRGB())
            using (CGBitmapContext context = new CGBitmapContext(
                handle.AddrOfPinnedObject(),
                (nint)image.Size.Width,
                (nint)image.Size.Height,
                8,
                (nint)image.Size.Width * 4,
                cspace,
                CGImageAlphaInfo.PremultipliedLast
                ))
            {
                context.DrawImage(new CGRect(new CGPoint(), image.Size), cgimage);

            }
            handle.Free();

			for (int i = 0; i < intValues.Length; ++i)
			{
				int val = intValues[i];
				floatValues[i * 3 + 0] = (((val >> 16) & 0xFF) - inputMean) * scale;
				floatValues[i * 3 + 1] = (((val >> 8) & 0xFF) - inputMean) * scale;
				floatValues[i * 3 + 2] = ((val & 0xFF) - inputMean) * scale;
			}
			System.Runtime.InteropServices.Marshal.Copy(floatValues, 0, dest, floatValues.Length);
#elif __UNIFIED__
            NSImage image = new NSImage(fileName);
            if (inputHeight > 0 || inputWidth > 0)
            {
                NSImage resized = new NSImage(new CGSize(inputWidth, inputHeight));
                resized.LockFocus();
                image.DrawInRect(new CGRect(0, 0, inputWidth, inputHeight), CGRect.Empty, NSCompositingOperation.SourceOver, 1.0f);
                resized.UnlockFocus();       
                image.Dispose();
                image = resized;
            }
            int[] intValues = new int[(int) (image.Size.Width * image.Size.Height)];
            float[] floatValues = new float[(int) (image.Size.Width * image.Size.Height * 3)];
            System.Runtime.InteropServices.GCHandle handle = System.Runtime.InteropServices.GCHandle.Alloc(intValues, System.Runtime.InteropServices.GCHandleType.Pinned);
            using (CGImage cgimage = image.CGImage)
            using (CGColorSpace cspace = CGColorSpace.CreateDeviceRGB())
            using (CGBitmapContext context = new CGBitmapContext(
                handle.AddrOfPinnedObject(),
                (nint)image.Size.Width,
                (nint)image.Size.Height,
                8,
                (nint)image.Size.Width * 4,
                cspace,
                CGImageAlphaInfo.PremultipliedLast
                ))
            {
                context.DrawImage(new CGRect(new CGPoint(), image.Size), cgimage);

            }
            handle.Free();

            for (int i = 0; i < intValues.Length; ++i)
            {
                int val = intValues[i];
                floatValues[i * 3 + 0] = (((val >> 16) & 0xFF) - inputMean) * scale;
                floatValues[i * 3 + 1] = (((val >> 8) & 0xFF) - inputMean) * scale;
                floatValues[i * 3 + 2] = ((val & 0xFF) - inputMean) * scale;
            }
            System.Runtime.InteropServices.Marshal.Copy(floatValues, 0, dest, floatValues.Length);

#else
            if (Emgu.TF.Util.Platform.OperationSystem ==  OS.Windows)
            {
                //Do something for Windows
                System.Drawing.Bitmap bmp = new Bitmap(fileName);

                if (inputHeight > 0 || inputWidth > 0)
                {
                    //resize bmp
                    System.Drawing.Bitmap newBmp = new Bitmap(bmp, inputWidth, inputHeight);
                    bmp.Dispose();
                    bmp = newBmp;
                    //bmp.Save("tmp.png");
                }

                byte[] byteValues = new byte[bmp.Width * bmp.Height * 3];
                System.Drawing.Imaging.BitmapData bd = new System.Drawing.Imaging.BitmapData();

                bmp.LockBits(
                    new Rectangle(0, 0, bmp.Width, bmp.Height), 
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb, bd);
                Marshal.Copy(bd.Scan0, byteValues, 0, byteValues.Length);
                bmp.UnlockBits(bd);

                if (typeof(T) == typeof(float))
                {
                    float[] floatValues = new float[bmp.Width * bmp.Height * 3];
                    for (int i = 0; i < byteValues.Length; ++i)
                    {
                        floatValues[i] = ((float)byteValues[i] - inputMean) * scale;
                    }
                    Marshal.Copy(floatValues, 0, dest, floatValues.Length);
                } else if (typeof(T) == typeof(byte))
                {
                    

                    bool swapBR = false;
                    if (swapBR)
                    {
                        int imageSize = bmp.Width * bmp.Height;
                        byte[] bValues = new byte[imageSize * 3];
                        for (int i = 0; i < imageSize; ++i)
                        {
                            bValues[i * 3] = (byte)(((float)byteValues[i * 3 + 2] - inputMean) * scale);
                            bValues[i * 3 + 1] = (byte)(((float)byteValues[i * 3 + 1] - inputMean) * scale);
                            bValues[i * 3 + 2] = (byte)(((float)byteValues[i * 3 + 0] - inputMean) * scale);
                            
                        }
                        Marshal.Copy(bValues, 0, dest, bValues.Length);
                    } else
                    {
                        if (! (inputMean == 0.0f && scale == 1.0f))
                            for (int i = 0; i < byteValues.Length; ++i)
                            {
                                byteValues[i] = (byte) ( ((float)byteValues[i] - inputMean) * scale );
                            }
                        Marshal.Copy(byteValues, 0, dest, byteValues.Length);
                    }

                } else
                {
                    throw new Exception(String.Format("Destination data type {0} is not supported.", typeof(T).ToString()));
                }
            }
            else
            {
                throw new Exception("Not implemented");
            }
#endif
        }

        public static byte[] PixelToJpeg(byte[] rawPixel, int width, int height, int channels)
        {
#if __ANDROID__
            if (channels != 4)
                throw new NotImplementedException("Only 4 channel pixel input is supported.");
            using (Bitmap bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888))
            using (MemoryStream ms = new MemoryStream())
            {
                IntPtr ptr = bitmap.LockPixels();
                //GCHandle handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
                Marshal.Copy(rawPixel, 0, ptr, rawPixel.Length);

                bitmap.UnlockPixels();

                bitmap.Compress(Bitmap.CompressFormat.Jpeg, 90, ms);
                return ms.ToArray();
            }
#elif __IOS__
            if (channels != 3)
                throw new NotImplementedException("Only 3 channel pixel input is supported.");
            System.Drawing.Size sz = new System.Drawing.Size(width, height);
            GCHandle handle = GCHandle.Alloc(rawPixel, GCHandleType.Pinned);
            using (CGColorSpace cspace = CGColorSpace.CreateDeviceRGB())
            using (CGBitmapContext context = new CGBitmapContext(
                handle.AddrOfPinnedObject(),
                sz.Width, sz.Height,
                8,
                sz.Width * 3,
                cspace,
                CGImageAlphaInfo.PremultipliedLast))
            using (CGImage cgImage = context.ToImage())
            using (UIImage newImg = new UIImage(cgImage))
            {
                handle.Free();
                var jpegData = newImg.AsJPEG();
                byte[] raw = new byte[jpegData.Length];
                System.Runtime.InteropServices.Marshal.Copy(jpegData.Bytes, raw, 0,
                    (int)jpegData.Length);
                return raw;
            }
#elif __UNIFIED__ //OSX
                    if (channels != 4)
                throw new NotImplementedException("Only 4 channel pixel input is supported.");
                                    System.Drawing.Size sz = new System.Drawing.Size(width, height);

            using (CGColorSpace cspace = CGColorSpace.CreateDeviceRGB())
            using (CGBitmapContext context = new CGBitmapContext(
                rawPixel,
                sz.Width, sz.Height,
                8,
                sz.Width * 4,
                cspace,
                CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrder32Big))
            using (CGImage cgImage = context.ToImage())

            using (NSBitmapImageRep newImg = new NSBitmapImageRep(cgImage))
            {
                var jpegData = newImg.RepresentationUsingTypeProperties(NSBitmapImageFileType.Jpeg);

                byte[] raw = new byte[jpegData.Length];
                System.Runtime.InteropServices.Marshal.Copy(jpegData.Bytes, raw, 0,
                    (int)jpegData.Length);
                return raw;
            }
#else
            throw new NotImplementedException("Not Implemented");
#endif
        }


#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE
#else

        private static float[] ScaleLocation(float[] location, int imageWidth, int imageHeight)
        {
            float left = location[0] * imageWidth;
            float top = location[1] * imageHeight;
            float right = location[2] * imageWidth;
            float bottom = location[3] * imageHeight;
            return new float[] { left, top, right, bottom };
        }

#if __MACOS__

        public static void DrawAnnotations(NSImage img, Annotation[] annotations)
        {
            img.LockFocus();

            NSColor redColor = NSColor.Red;
            redColor.Set();
            var context = NSGraphicsContext.CurrentContext;
            var cgcontext = context.CGContext;
            cgcontext.ScaleCTM(1, -1);
            cgcontext.TranslateCTM(0, -img.Size.Height);
            //context.IsFlipped = !context.IsFlipped;
            for (int i = 0; i < annotations.Length; i++)
            {
                float[] rects = ScaleLocation(annotations[i].Rectangle, (int)img.Size.Width, (int)img.Size.Height);
                CGRect cgRect = new CGRect(
                    rects[0],
                    rects[1],
                    rects[2] - rects[0],
                    rects[3] - rects[1]);
                NSBezierPath.StrokeRect(cgRect);
            }
            img.UnlockFocus();
        }
#endif

        /// <summary>
        /// Image annotation
        /// </summary>
        public class Annotation
        {
            /// <summary>
            /// The coordinates of the rectangle, the values are in the range of [0, 1], each rectangle contains 4 values, corresponding to the top left corner (x0, y0) and bottom right corner (x1, y1)
            /// </summary>
            public float[] Rectangle;

            /// <summary>
            /// The text to be drawn on the top left corner of the Rectangle
            /// </summary>
            public String Label;
        }

        /// <summary>
        /// Read the file and draw rectangles on it.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="annotations">Annotations to be add to the image. Can consist of rectangles and lables</param>
        /// <returns>The image in Jpeg stream format</returns>
        public static byte[] ImageFileToJpeg(String fileName, Annotation[] annotations = null)
        {
#if __ANDROID__
            BitmapFactory.Options options = new BitmapFactory.Options();
            options.InMutable = true;
            Android.Graphics.Bitmap bmp = BitmapFactory.DecodeFile(fileName, options);

            Android.Graphics.Paint p = new Android.Graphics.Paint();
            p.SetStyle(Paint.Style.Stroke);
            p.AntiAlias = true;
            p.Color = Android.Graphics.Color.Red;
            Canvas c = new Canvas(bmp);
                        
            for (int i = 0; i < annotations.Length; i++)
            {
                float[] rects = ScaleLocation(annotations[i].Rectangle, bmp.Width, bmp.Height);
                Android.Graphics.Rect r = new Rect((int)rects[0], (int) rects[1], (int) rects[2], (int) rects[3]);
                c.DrawRect(r, p);
            }     

            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Compress(Bitmap.CompressFormat.Jpeg, 90, ms);
                return ms.ToArray();
            }
#elif __MACOS__
            NSImage img = NSImage.ImageNamed(fileName);

            DrawAnnotations(img, annotations);
            /*
            img.LockFocus();

            NSColor redColor = NSColor.Red;
            redColor.Set();
            var context = NSGraphicsContext.CurrentContext;
            var cgcontext = context.CGContext;
            cgcontext.ScaleCTM(1, -1);
            cgcontext.TranslateCTM(0, -img.Size.Height);
            //context.IsFlipped = !context.IsFlipped;
            for (int i = 0; i < annotations.Length; i++)
            {
                float[] rects = ScaleLocation(annotations[i].Rectangle, (int)img.Size.Width, (int) img.Size.Height);
                CGRect cgRect = new CGRect(
                    rects[0], 
                    rects[1], 
                    rects[2] - rects[0], 
                    rects[3] - rects[1]);
                NSBezierPath.StrokeRect(cgRect);
            }
            img.UnlockFocus();
            */

            var imageData = img.AsTiff();
            var imageRep = NSBitmapImageRep.ImageRepsWithData(imageData)[0] as NSBitmapImageRep;
            var jpegData = imageRep.RepresentationUsingTypeProperties(NSBitmapImageFileType.Jpeg, null);
            byte[] jpeg = new byte[jpegData.Length];
            System.Runtime.InteropServices.Marshal.Copy(jpegData.Bytes, jpeg, 0, (int)jpegData.Length);
            return jpeg;
#elif __IOS__
            UIImage uiimage = new UIImage(fileName);

            UIGraphics.BeginImageContextWithOptions(uiimage.Size, false, 0);
            var context = UIGraphics.GetCurrentContext();

            uiimage.Draw(new CGPoint());
            context.SetStrokeColor(UIColor.Red.CGColor);
            context.SetLineWidth(2);
            for (int i = 0; i < annotations.Length; i++)
            {
                float[] rects = ScaleLocation(
                    annotations[i].Rectangle,
                    (int)uiimage.Size.Width,
                    (int)uiimage.Size.Height);
                CGRect cgRect = new CGRect(
                                           (nfloat)rects[0],
                                           (nfloat)rects[1],
                                           (nfloat)(rects[2] - rects[0]),
                                           (nfloat)(rects[3] - rects[1]));
                context.AddRect(cgRect);
                context.DrawPath(CGPathDrawingMode.Stroke);
            }
            UIImage imgWithRect = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();

            var jpegData = imgWithRect.AsJPEG();
			byte[] jpeg = new byte[jpegData.Length];
			System.Runtime.InteropServices.Marshal.Copy(jpegData.Bytes, jpeg, 0, (int)jpegData.Length);
            return jpeg;
#else
            if (Emgu.TF.Util.Platform.OperationSystem == OS.Windows)
            {
                Bitmap img = new Bitmap(fileName);

                if (annotations != null)
                {
                    using (Graphics g = Graphics.FromImage(img))
                    {
                        for (int i = 0; i < annotations.Length; i++)
                        {
                            if (annotations[i].Rectangle != null)
                            {
                                float[] rects = ScaleLocation(annotations[i].Rectangle, img.Width, img.Height);
                                PointF origin = new PointF(rects[0], rects[1]);
                                RectangleF rect = new RectangleF(rects[0], rects[1], rects[2] - rects[0], rects[3] - rects[1]);
                                Pen redPen = new Pen(Color.Red, 3);
                                g.DrawRectangle(redPen, Rectangle.Round(rect));

                                String label = annotations[i].Label;
                                if (label != null)
                                {
                                    g.DrawString(label, new Font(FontFamily.GenericSansSerif, 20f), Brushes.Red, origin);
                                }
                            }
                        }
                        g.Save();
                    }
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    return ms.ToArray();
                }
            }
            else
            {
                throw new Exception("DrawResultsToJpeg Not implemented for this platform");
            }
#endif

        }

#endif
    }
}
