using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace c_sharp_multi_source
{
    public class hand
    {

        public int DepthWidth { get; set; }
        public int DepthHeight { get; set; }

        public int MinDepth { get; set; }
        public int MaxDepth { get; set; }

        public CoordinateMapper CoordinateMapper { get; set; }


        public int handLeftX = -1;
        public int handLeftY = -1;

        public int handRightX = -1;
        public int handRightY = -1;

        public int hand_right_depth = 0;
        public int hand_left_depth = 0;
        ushort[] pixelData = null;
        byte[] bodyIdx = null;

        public int forward_range = 10;
        public int backward_range = 10;
        ~hand()
        {
            if(pixelData != null)
                pixelData.Reverse();
            if (bodyIdx != null)
                bodyIdx.Reverse();
        }

        public unsafe void Update(DepthFrame frame)
        {
            MinDepth = frame.DepthMinReliableDistance;
            MaxDepth = frame.DepthMaxReliableDistance;

            DepthWidth = frame.FrameDescription.Width;
            DepthHeight = frame.FrameDescription.Height;
            if(pixelData == null)
                pixelData = new ushort[DepthWidth * DepthHeight];
            frame.CopyFrameDataToArray(pixelData);
        }

        public unsafe void Update(BodyIndexFrame frame)
        {
            if (bodyIdx == null)
            {
                bodyIdx = new byte[DepthWidth * DepthHeight];
            }
            frame.CopyFrameDataToArray(bodyIdx);
        }

        public unsafe void Update(Body body)
        {
            Joint jointHandLeft = body.Joints[JointType.HandLeft];
            Joint jointHandRight = body.Joints[JointType.HandRight];

            DepthSpacePoint depthPointHandLeft = CoordinateMapper.MapCameraPointToDepthSpace(jointHandLeft.Position);
            DepthSpacePoint depthPointHandRight = CoordinateMapper.MapCameraPointToDepthSpace(jointHandRight.Position);

            handLeftX = (int)depthPointHandLeft.X;
            handLeftY = (int)depthPointHandLeft.Y;

            handRightX = (int)depthPointHandRight.X;
            handRightY = (int)depthPointHandRight.Y;

            hand_right_depth = this.calculate_depth_value(handRightX, handRightY);
            hand_left_depth = this.calculate_depth_value(handLeftX, handLeftY);
        }
        
        private int calculate_depth_value(int handX, int handY)
        {
            if(handX >=0 && handY >=0)
            {
                int idx = handX + handY * DepthWidth;
                if(idx < DepthWidth * DepthHeight)
                {
                    return pixelData[idx];
                }
            }
            return 0;
        }

        public unsafe ImageSource drawDepth()
        {
            return pixelData.ToBitMap(DepthWidth, DepthHeight, MinDepth, MaxDepth);
        }

        public unsafe ImageSource draw_with_specific_depth()
        {
            return pixelData.display_hand_only(bodyIdx, DepthWidth, DepthHeight, MinDepth, MaxDepth, hand_right_depth, forward_range, backward_range);
        }
    }

    public static class extension
    {
        public unsafe static ImageSource ToBitMap(this ushort[] pixelData, int DepthWidth, int DepthHeight, int minDepth, int maxDepth)
        {
            PixelFormat format = PixelFormats.Bgr32;
            byte[] pixels = new byte[DepthWidth * DepthHeight * (format.BitsPerPixel + 7) / 8];
            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < pixelData.Length; ++depthIndex)
            {
                ushort depth = pixelData[depthIndex];

                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                pixels[colorIndex++] = intensity; // Blue
                pixels[colorIndex++] = intensity; // Green
                pixels[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            int stride = DepthWidth * format.BitsPerPixel / 8;
            return BitmapSource.Create(DepthWidth, DepthHeight, 96, 96, format, null, pixels, stride);
        }

        public unsafe static ImageSource display_hand_only(this ushort[] pixelData, byte[] bodyIdx,int DepthWidth, int DepthHeight, int minDepth, int maxDepth, int depth_value, int forward_range, int backward_range)
        {
            PixelFormat format = PixelFormats.Bgr32;
            byte[] pixels = new byte[DepthWidth * DepthHeight * (format.BitsPerPixel + 7) / 8];
            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < pixelData.Length; ++depthIndex)
            {
                ushort depth = pixelData[depthIndex];

                //byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 255);
                //byte intensity = 0 // black;
                byte intensity;
                if (depth >= (depth_value - forward_range) && depth <= (depth_value + backward_range) && (bodyIdx[depthIndex] < 6))
                {
                    intensity = 255;
                }
                else
                {
                    intensity = 0;
                }
                pixels[colorIndex++] = intensity; // Blue
                pixels[colorIndex++] = intensity; // Green
                pixels[colorIndex++] = intensity; // Red

                ++colorIndex;
            }
            int stride = DepthWidth * format.BitsPerPixel / 8;
            return BitmapSource.Create(DepthWidth, DepthHeight, 96, 96, format, null, pixels, stride);
        }
    }
}
