﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace Microsoft.Samples.Kinect.FaceBasics
{
    //struct DataToController
    //{
    //    byte cheakId;
    //    byte cmd;
    //    byte act;
    //    byte reserve;
    //    byte []pixelsData;
    //}
    enum Animation : byte
    {
        nothing,
        byOrder,
        circle,
        logo,
        ripples,
        dof
    }

    enum VFX : byte
    {
        noting,
        flyIn,
        splitting,
        erase,
        zoom
    }

    public partial class MainWindow
    {
        private const int wallWidth = 60;

        private const int wallHeight = 35;

        //private WriteableBitmap animationBitmap = null;

        Animation animation = Animation.nothing;

        VFX vfx = VFX.noting;

        int timerCounter = 0;

        //private byte[] animationPixels = null;

        //定时器，用于特效动画的计时；
        private Timer timer;

        //用于切换kinect模式和自定义模式，false:kinect模式，true:自定义模式；
        private bool kinectMode = false;

        private bool depthScreen = false;

        private bool faceMode = false;

        //private bool faceHappy = false;

        double[,] distanceToCenter = null;

        byte[,] bitmapPixels = null;

        // 定时器回调函数
        void timerCall(object value)
        {
            ////
            switch (animation)
            {
                case Animation.byOrder:
                    if (timerCounter < wallPixels.GetLength(0))
                    {
                        for (int i = 0; i < wallWidth; i++)
                        {
                            wallPixels[timerCounter, i] = 200;
                        }
                    }
                    break;
                case Animation.logo:
                    switch (vfx)
                    {
                        case VFX.flyIn:
                            vfxFlyIn();
                            break;
                        case VFX.splitting:
                            vfxSplitting();
                            break;
                        case VFX.erase:
                            vfxErase();
                            break;
                        case VFX.zoom:

                            break;
                    }
                    break;
                case Animation.ripples:
                    
                    makeRipples();

                    break;

            }

            //将幕墙像素转换为待显示图像数据
            WallPixels2ImagePixels();

            // 显示
            //Array.Clear(depthPixels, 0, depthPixels.Length);  
            this.depthBitmap.Dispatcher.Invoke(
                new Action(
                    delegate
                    {
                        this.RenderPixels();
                    }
                    )
                );

            // udp send data
            UdpSendData();

            //周期计数
            ++timerCounter;
        }


        // 数据压缩
        private void DataCompress()
        {
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    // 每个5*5单元的像素数据
                    byte[,] unit = new byte[5, 5];

                    // 用于存储每个5*5单元压缩后的数据
                    byte[] unitData = new byte[7];

                    //获取每个5*5单元的数据
                    for (int m = 0; m < 5; m++)
                    {
                        for (int n = 0; n < 5; n++)
                        {
                            unit[m, n] = wallPixels[i * 5 + m , j * 5 + n];
                        }
                    }

                    //将5*5单元的数据压缩进7个字节
                    unitData[0] = (byte)((unit[0, 0]) + (unit[0, 1] << 2) + (unit[0, 2] << 4) + (unit[0, 3] << 6));
                    unitData[1] = (byte)((unit[0, 4]) + (unit[1, 0] << 2) + (unit[1, 1] << 4) + (unit[1, 2] << 6));
                    unitData[2] = (byte)((unit[1, 3]) + (unit[1, 4] << 2) + (unit[2, 0] << 4) + (unit[2, 1] << 6));
                    unitData[3] = (byte)((unit[2, 2]) + (unit[2, 3] << 2) + (unit[2, 4] << 4) + (unit[3, 0] << 6));
                    unitData[4] = (byte)((unit[3, 1]) + (unit[3, 2] << 2) + (unit[3, 3] << 4) + (unit[3, 4] << 6));
                    unitData[5] = (byte)((unit[4, 0]) + (unit[4, 1] << 2) + (unit[4, 2] << 4) + (unit[4, 3] << 6));
                    unitData[6] = (byte)(unit[4, 4]);

                    for (int k = 0; k < 7; k++)
                    {
                        SendData[i, j, k] = unitData[k];
                    }
                }
            }
        }


        // 将幕墙像素转换为深度像素，用于桌面显示；
        private void WallPixels2ImagePixels()
        {
            byte[,] depthData_424_512 = new byte[424, 512];

            for (int i = 0; i < 280; i++)
            {
                for (int j = 0; j < 480; j++)
                {
                    depthData_424_512[i + 72, j + 16] = wallPixels[i / 8, j / 8];
                }
            }

            // 将深度数据复制到用于显示的像素数组
            for (int i = 0; i < (int)(424*512); ++i)
            {
                imagePixels[i] = depthData_424_512[i / 512, i % 512];
            }
        }

        // 将深度像素转换为幕墙像素，用于幕墙显示；
        private unsafe void DepthFrameData2WallPixels(IntPtr depthFrameData)
        {
            ushort* frameData = (ushort*)depthFrameData;

            ushort[,] depthData_280_480 = new ushort[280, 480];

            // 将512*424的深度数据剪切为480*280的数据
            for (int i = 0; i < 280; i++)
            {
                for (int j = 0; j < 480; j++)
                {
                    ushort temp = 0;

                    byte player = bodyIndexDataArray[(i + 72) * 512 + 16 + j];

                    // if we're tracking a player for the current pixel
                    if (player != 0xff)
                    {
                        temp = *(frameData + (i + 72) * 512 + 16 + j);

                        //取范围内的有效深度数据，并将其缩放成256的灰度数据   
                        depthData_280_480[i, j] = (ushort)((temp >= 1000) && (temp <= 3000) ? (256 - (temp - 1000) / 8) : 0);
                    }
                }
            }

            //将480*280的深度图像压缩成60*35的图像数据
            for (int i = 0; i < 35; i++)
            {
                for (int j = 0; j < 60; j++)
                {
                    uint temp = 0;
                    for (int k = 0; k < 8; k++)
                    {
                        for (int n = 0; n < 8; n++)
                        {
                            temp += depthData_280_480[i * 8 + k, j * 8 + n];
                        }
                    }
                    wallPixels[i, j] = (byte)(temp / 64);
                    wallPixels[i, j] /= 63; //将0-250的数值转换为0-3；
                    wallPixels[i, j] *= 62;  //数值放大便于显示
                }
            }
        }

        // 网络发送数据
        void UdpSendData()
        {
            // 数据压缩
            DataCompress();

            // 数据分7个包发送
            for (int i = 0; i < 7; i++)
            {
                //前4个字节为cmd等信息,后12*7个字节为每一个二级单元的幕墙像素；
                //懒得用结构体，因为要与字节数组做相互转换；
                byte[] sendbuffer = new byte[12 * 7 + 4];

                for (int j = 0; j < 12; j++)
                {
                    for (int k = 0; k < 7; k++)
                    {
                        sendbuffer[j * 7 + k + 4] = SendData[i, j, k];
                    }
                }

                clientSocket.SendTo(sendbuffer, remotePoint[i]);
            }            
        }

        private unsafe void bitmapToArray(string file, ref byte[,] pixels)
        {
            using(Bitmap image = new Bitmap(file))
            {
                BitmapData bitmapData = image.LockBits(new Rectangle(0,0,image.Width,image.Height),
                                        ImageLockMode.ReadOnly,
                                        image.PixelFormat);

                byte* pointer = (byte*)bitmapData.Scan0.ToPointer();

                for (int i = 0; i < pixels.GetLength(0); i++)
                {
                    for (int j = 0; j < pixels.GetLength(1); j++)
                    {
                        pixels[i, j] = (byte)(255-*(pointer));
                        //pixels[i, j] = (byte)(*(pointer) < 25 ? 0 : 255);
                        pointer++;
                    }
                    //pointer += bitmapData.Stride - bitmapData.Width;
                }

                image.UnlockBits(bitmapData);
            }
        }

        //制造涟漪
        private void makeRipples()
        {
            double radius = (timerCounter * 1)%30;
            double radius2 = ((timerCounter * 1) - 15) % 30;

            //制作涟漪动画
            for (int i = 0; i < 35; i++)
            {
                for (int j = 0; j < 60; j++)
                {
                    double disToRadius = Math.Abs(distanceToCenter[i, j] - radius);
                    double disToRadius2 = Math.Abs(distanceToCenter[i, j] - radius2);

                    if (disToRadius >= 0 && disToRadius < 0.8)
                        wallPixels[i, j] = 255;
                    else if (disToRadius > 0.8 && disToRadius < 1.6)
                        wallPixels[i, j] = 200;
                    else if (disToRadius > 1.6 && disToRadius < 2.4)
                        wallPixels[i, j] = 100;
                    else
                    {
                        if (disToRadius2 >= 0 && disToRadius2 < 0.8)
                            wallPixels[i, j] = 255;
                        else if (disToRadius2 > 0.8 && disToRadius2 < 1.6)
                            wallPixels[i, j] = 200;
                        else if (disToRadius2 > 1.6 && disToRadius2 < 2.4)
                            wallPixels[i, j] = 100;
                        else
                            wallPixels[i, j] = 0;
                    }

                    //if (Math.Abs(distanceToCenter[i, j] - radius) < 0.8)
                    //    wallPixels[i, j] = 255;
                    //else
                    //    wallPixels[i, j] = 0;
                }
            }
        }

        //飞入特效
        private void vfxFlyIn()
        {
            if (timerCounter < 35)
            {
                for (int i = 0; i < timerCounter; i++)
                {
                    for (int j = 0; j < 60; j++)
                    {
                        wallPixels[i + 35 - timerCounter - 1, j] = bitmapPixels[i, j];
                    }
                }
            } 
        }

        //劈裂特效
        private void vfxSplitting()
        {
            if (timerCounter < 60 / 2)
            {
                for (int index = 0; index <= timerCounter; index++)
                {
                    for (int i = 0; i < 35; i++)
                    {
                        wallPixels[i, index] = bitmapPixels[i, index];
                        wallPixels[i, 60 - index - 1] = bitmapPixels[i, 60 - index - 1];
                    }
                }
            } 
        }

        //擦除特效
        private void vfxErase()
        {
            if (timerCounter < 35)
            {
                for (int i = 0; i < timerCounter; i++)
                {
                    for (int j = 0; j < 60; j++)
                    {
                        wallPixels[i + 35 - timerCounter - 1, j] = bitmapPixels[i + 35 - timerCounter - 1, j];
                    }
                }
            } 
        }

    }
}


//string path = @"D:\MovingWall Project\MovingWallGUI\face.txt";

//if (File.Exists(path))
//{
//    string[] readTxt = File.ReadAllLines(path);

//    byte[,] txtPixels = new byte[35, 60];

//    var res = readTxt.Select(x => x.Split(',')).ToArray();

//    for (int i = 0; i < 35; i++)
//    {
//        for (int j = 0; j < 60; j++)
//        {
//            txtPixels[i, j] = (byte)(Convert.ToByte(res[i][j])*60); 
//        }
//    }

//    Array.Copy(txtPixels, wallPixels, wallPixels.Length);
//}
