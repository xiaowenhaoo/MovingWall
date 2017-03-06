using System;
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
        logo
    }
    public partial class MainWindow
    {
        private const int wallWidth = 60;

        private const int wallHeight = 35;

        //private WriteableBitmap animationBitmap = null;

        Animation animation = Animation.nothing;

        long timerCounter = 0;

        //private byte[] animationPixels = null;

        //定时器，用于特效动画的计时；
        private Timer timer;

        //用于切换kinect模式和自定义模式，false:kinect模式，true:自定义模式；
        private bool kinectMode = false;

        private bool depthScreen = false;

        private bool faceMode = false;

        //private bool faceHappy = false;

        byte[,] faceWithNoMotion = {
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,3,3,3,3,3,3,3,3,0,0,0,3,3,3,3,3,3,3,3,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,2,2,2,2,2,2,1,1,2,1,1,2,2,2,2,2,2,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,2,2,2,2,2,2,1,1,2,1,1,2,2,2,2,2,2,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,2,2,2,2,2,2,1,1,2,1,1,2,2,2,2,2,2,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,2,2,2,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,2,2,2,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,2,2,2,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,3,3,3,3,3,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,3,3,3,3,3,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,3,3,3,3,3,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,3,3,3,3,3,3,3,3,3,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,3,3,3,3,3,3,3,3,3,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,3,3,3,3,3,3,3,3,3,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,3,3,3,3,3,3,3,3,3,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,3,3,3,3,3,3,3,3,3,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                   };




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
                    
                    break;
                case Animation.logo:

                    //byte []image = new byte[35*60];
                    
                    //Array.Copy(image, imagePixels, imagePixels.Length);

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
                        pixels[i,j] = *(pointer);
                        pointer++;
                    }
                    //pointer += bitmapData.Stride - bitmapData.Width;
                }

                image.UnlockBits(bitmapData);
            }
        }
    }
}
