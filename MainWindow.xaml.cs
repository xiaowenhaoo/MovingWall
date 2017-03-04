//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.FaceBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    //using System.Windows.Media.Media3D;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Face;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Thickness of face bounding box and face points
        /// </summary>
        private const double DrawFaceShapeThickness = 8;

        /// <summary>
        /// Font size of face property text 
        /// </summary>
        private const double DrawTextFontSize = 30;

        /// <summary>
        /// Radius of face point circle
        /// </summary>
        private const double FacePointRadius = 1.0;

        /// <summary>
        /// Text layout offset in X axis
        /// </summary>
        private const float TextLayoutOffsetX = -0.1f;

        /// <summary>
        /// Text layout offset in Y axis
        /// </summary>
        private const float TextLayoutOffsetY = -0.15f;

        /// <summary>
        /// Face rotation display angle increment in degrees
        /// </summary>
        private const double FaceRotationIncrementInDegrees = 5.0;

        /// <summary>
        /// Formatted text to indicate that there are no bodies/faces tracked in the FOV
        /// </summary>
        private FormattedText textFaceNotTracked = new FormattedText(
                        "No bodies or faces are tracked ...",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Georgia"),
                        DrawTextFontSize,
                        Brushes.White);

        /// <summary>
        /// Text layout for the no face tracked message
        /// </summary>
        private Point textLayoutFaceNotTracked = new Point(10.0, 10.0);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        public KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        //private DepthSpacePoint[] colorMappedToDepthPoints = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array to store bodies
        /// </summary>
        private Body[] bodies = null;
        private Body currentTrackedBody = null;

        private ulong currentTrackingId = 0;

        /// <summary>
        /// Number of bodies tracked
        /// </summary>
        private int bodyCount;

        /// <summary>
        /// Face frame sources
        /// </summary>
        // private FaceFrameSource[] faceFrameSources = null;
        private FaceFrameSource faceFrameSource = null;

        /// <summary>
        /// Face frame readers
        /// </summary>
        //private FaceFrameReader[] faceFrameReaders = null;
        private FaceFrameReader faceFrameReader = null;

        /// <summary>
        /// Storage for face frame results
        /// </summary>
        //private FaceFrameResult[] faceFrameResults = null;
        private FaceFrameResult faceFrameResult = null;

        /// <summary>
        /// Width of display (color space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (color space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// Display rectangle
        /// </summary>
        private Rect displayRect;

        /// <summary>
        /// List of brushes for each face tracked
        /// </summary>
        private List<Brush> faceBrush;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        //depth frame
        private DepthFrameReader depthFrameReader = null;

        private FrameDescription depthFrameDescription = null;

        private WriteableBitmap depthBitmap = null;

        private byte[] depthPixels = null;

        private byte[] imagePixels = null;
        /// <summary>
        /// Map depth range to byte range
        /// </summary>
        private const int MapDepthToByte = 8000 / 256;

        // body index frame
        private BodyIndexFrameReader bodyIndexFrameReader = null;

        private byte[] bodyIndexDataArray = null;

        //socket 
        private Socket clientSocket;

        EndPoint[] remotePoint;

        byte[,] wallPixels = null;

        byte[, ,] SendData = null;
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the color frame details
            FrameDescription frameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;

            // set the display specifics
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;
            this.displayRect = new Rect(0.0, 0.0, this.displayWidth, this.displayHeight);

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // wire handler for body frame arrival
            this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;

            // set the maximum number of bodies that would be tracked by Kinect
            this.bodyCount = this.kinectSensor.BodyFrameSource.BodyCount;

            // allocate storage to store body objects
            //this.bodies = new Body[this.bodyCount];

            // specify the required face frame results
            FaceFrameFeatures faceFrameFeatures =
                FaceFrameFeatures.BoundingBoxInColorSpace
                | FaceFrameFeatures.PointsInColorSpace
                | FaceFrameFeatures.RotationOrientation
                | FaceFrameFeatures.FaceEngagement
                | FaceFrameFeatures.Glasses
                | FaceFrameFeatures.Happy
                | FaceFrameFeatures.LeftEyeClosed
                | FaceFrameFeatures.RightEyeClosed
                | FaceFrameFeatures.LookingAway
                | FaceFrameFeatures.MouthMoved
                | FaceFrameFeatures.MouthOpen;

            // create a face frame source + reader to track each face in the FOV
            //this.faceFrameSources = new FaceFrameSource[this.bodyCount];
            //this.faceFrameReaders = new FaceFrameReader[this.bodyCount];
            //for (int i = 0; i < this.bodyCount; i++)
            //{
            //    // create the face frame source with the required face frame features and an initial tracking Id of 0
            //    this.faceFrameSources[i] = new FaceFrameSource(this.kinectSensor, 0, faceFrameFeatures);

            //    // open the corresponding reader
            //    this.faceFrameReaders[i] = this.faceFrameSources[i].OpenReader();
            //}
            this.faceFrameSource = new FaceFrameSource(this.kinectSensor, 0, faceFrameFeatures);
            this.faceFrameReader = this.faceFrameSource.OpenReader();

            // allocate storage to store face frame results for each face in the FOV
            //this.faceFrameResults = new FaceFrameResult[this.bodyCount];

            // populate face result colors - one for each face index
            this.faceBrush = new List<Brush>()
            {
                Brushes.White, 
                Brushes.Orange,
                Brushes.Green,
                Brushes.Red,
                Brushes.LightBlue,
                Brushes.Yellow
            };

            // depth frame
            this.depthFrameReader = this.kinectSensor.DepthFrameSource.OpenReader();

            this.depthFrameReader.FrameArrived += this.Reader_DepthFrameArrived;

            this.depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // body index frame
            this.bodyIndexFrameReader = this.kinectSensor.BodyIndexFrameSource.OpenReader();

            this.bodyIndexFrameReader.FrameArrived += this.Reader_BodyIndexFrameArrived;

            this.bodyIndexDataArray = new byte[512 * 424];

            // create the bitmap to display
            this.depthBitmap = new WriteableBitmap(this.depthFrameDescription.Width, this.depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            //this.animationBitmap = new WriteableBitmap(wallWidth, wallHeight, 96.0, 96.0, PixelFormats.Gray8, null);

            // allocate space to put the pixels being received and converted
            this.depthPixels = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height];

            this.imagePixels = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height];
            //this.colorMappedToDepthPoints = new DepthSpacePoint[displayWidth * displayHeight];

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();

            // socket
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            remotePoint = new EndPoint[7];
            remotePoint[0] = new IPEndPoint(IPAddress.Parse("192.168.2.114"), 10001);  //xiao
            remotePoint[1] = new IPEndPoint(IPAddress.Parse("192.168.2.109"), 10001);  //xusiyuan
            remotePoint[2] = new IPEndPoint(IPAddress.Parse("192.168.2.103"), 10001);  //xiao
            remotePoint[3] = new IPEndPoint(IPAddress.Parse("192.168.2.113"), 10001);  //moudi
            remotePoint[4] = new IPEndPoint(IPAddress.Parse("192.168.2.119"), 10001);  //weiwenzhao
            remotePoint[5] = new IPEndPoint(IPAddress.Parse("192.168.2.102"), 10001);  //huyong
            remotePoint[6] = new IPEndPoint(IPAddress.Parse("192.168.2.104"), 10001);

            wallPixels = new byte[35, 60];

            SendData = new byte[7, 12, 7];

            //animationPixels = new byte[35 * 60];
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                //return this.imageSource;
                return this.depthBitmap;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Converts rotation quaternion to Euler angles 
        /// And then maps them to a specified range of values to control the refresh rate
        /// </summary>
        /// <param name="rotQuaternion">face rotation quaternion</param>
        /// <param name="pitch">rotation about the X-axis</param>
        /// <param name="yaw">rotation about the Y-axis</param>
        /// <param name="roll">rotation about the Z-axis</param>
        private static void ExtractFaceRotationInDegrees(Vector4 rotQuaternion, out int pitch, out int yaw, out int roll)
        {
            double x = rotQuaternion.X;
            double y = rotQuaternion.Y;
            double z = rotQuaternion.Z;
            double w = rotQuaternion.W;

            // convert face rotation quaternion to Euler angles in degrees
            double yawD, pitchD, rollD;
            pitchD = Math.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Math.PI * 180.0;
            yawD = Math.Asin(2 * ((w * y) - (x * z))) / Math.PI * 180.0;
            rollD = Math.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Math.PI * 180.0;

            // clamp the values to a multiple of the specified increment to control the refresh rate
            double increment = FaceRotationIncrementInDegrees;
            pitch = (int)(Math.Floor((pitchD + ((increment / 2.0) * (pitchD > 0 ? 1.0 : -1.0))) / increment) * increment);
            yaw = (int)(Math.Floor((yawD + ((increment / 2.0) * (yawD > 0 ? 1.0 : -1.0))) / increment) * increment);
            roll = (int)(Math.Floor((rollD + ((increment / 2.0) * (rollD > 0 ? 1.0 : -1.0))) / increment) * increment);
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //for (int i = 0; i < this.bodyCount; i++)
            //{
            //    if (this.faceFrameReaders[i] != null)
            //    {
            //        // wire handler for face frame arrival
            //        this.faceFrameReaders[i].FrameArrived += this.Reader_FaceFrameArrived;
            //    }
            //}

            this.faceFrameReader.FrameArrived += this.Reader_FaceFrameArrived;

            timer = new Timer(new TimerCallback(timerCall), null, Timeout.Infinite, 100);

            //if (this.bodyFrameReader != null)
            //{
            //    // wire handler for body frame arrival
            //    this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;
            //}
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            //for (int i = 0; i < this.bodyCount; i++)
            //{
            //    if (this.faceFrameReaders[i] != null)
            //    {
            //        // FaceFrameReader is IDisposable
            //        this.faceFrameReaders[i].Dispose();
            //        this.faceFrameReaders[i] = null;
            //    }

            //    if (this.faceFrameSources[i] != null)
            //    {
            //        // FaceFrameSource is IDisposable
            //        this.faceFrameSources[i].Dispose();
            //        this.faceFrameSources[i] = null;
            //    }
            //}

            // 释放定时器资源
            timer.Dispose();

            if (this.faceFrameReader != null)
            {
                // FaceFrameReader is IDisposable
                this.faceFrameReader.Dispose();
                this.faceFrameReader = null;
            }

            if (this.faceFrameSource != null)
            {
                // FaceFrameSource is IDisposable
                this.faceFrameSource.Dispose();
                this.faceFrameSource = null;
            }

            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.depthFrameReader != null)
            {
                // DepthFrameReader is IDisposable
                this.depthFrameReader.Dispose();
                this.depthFrameReader = null;
            }

            if (this.bodyIndexFrameReader != null)
            {
                // remove the event handler
                this.bodyIndexFrameReader.FrameArrived -= this.Reader_BodyIndexFrameArrived;

                // BodyIndexFrameReder is IDisposable
                this.bodyIndexFrameReader.Dispose();
                this.bodyIndexFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the face frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame != null)
                {
                    // get the index of the face source from the face source array
                    //int index = this.GetFaceSourceIndex(faceFrame.FaceFrameSource);

                    // check if this face frame has valid face frame results
                    if (this.ValidateFaceBoxAndPoints(faceFrame.FaceFrameResult))
                    {
                        // store this face frame result to draw later
                        //this.faceFrameResults[index] = faceFrame.FaceFrameResult;
                        this.faceFrameResult = faceFrame.FaceFrameResult;

                        string faceText = string.Empty;

                        if (this.faceFrameResult.FaceProperties != null)
                        {
                            foreach (var item in faceFrameResult.FaceProperties)
                            {
                                faceText += item.Key.ToString() + " : ";

                                if (item.Value == DetectionResult.Maybe)
                                {
                                    faceText += DetectionResult.Yes + "\n";
                                }
                                else
                                {
                                    faceText += item.Value.ToString() + "\n";
                                }

                                //判断人物表情
                                if (item.Key == FaceProperty.Happy)
                                {
                                    //if (item.Value == DetectionResult.Yes || item.Value == DetectionResult.Maybe)
                                    //    faceHappy = true;
                                    //else
                                    //    faceHappy = false;
                                }
                            }
                        }

                        //this.StatusText = faceText;
                    }
                    else
                    {
                        // indicates that the latest face frame result from this reader is invalid
                        //this.faceFrameResults[index] = null;
                        this.faceFrameResult = null;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the index of the face frame source
        /// </summary>
        /// <param name="faceFrameSource">the face frame source</param>
        /// <returns>the index of the face source in the face source array</returns>
        //private int GetFaceSourceIndex(FaceFrameSource faceFrameSource)
        //{
        //    int index = -1;

        //    for (int i = 0; i < this.bodyCount; i++)
        //    {
        //        if (this.faceFrameSources[i] == faceFrameSource)
        //        {
        //            index = i;
        //            break;
        //        }
        //    }

        //    return index;
        //}

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame == null)
                {
                    // We might miss the chance to acquire the frame, it will be null if it's missed
                    return;
                }

                if (this.currentTrackedBody != null)
                {
                    this.currentTrackedBody = FindBodyWithTrackingId(bodyFrame, this.currentTrackingId);

                    if (this.currentTrackedBody != null)
                    {
                        return;
                    }
                }

                Body selectedBody = FindClosestBody(bodyFrame);

                if (selectedBody == null)
                {
                    return;
                }

                this.currentTrackedBody = selectedBody;
                this.currentTrackingId = selectedBody.TrackingId;

                // 没有此句，faceFrameArrived读取不到数值
                this.faceFrameSource.TrackingId = selectedBody.TrackingId;
            }
        }

        /// <summary>
        /// Draws face frame results
        /// </summary>
        /// <param name="faceIndex">the index of the face frame corresponding to a specific body in the FOV</param>
        /// <param name="faceResult">container of all face frame results</param>
        /// <param name="drawingContext">drawing context to render to</param>
        private void DrawFaceFrameResults(int faceIndex, FaceFrameResult faceResult, DrawingContext drawingContext)
        {
            // choose the brush based on the face index
            Brush drawingBrush = this.faceBrush[0];
            if (faceIndex < this.bodyCount)
            {
                drawingBrush = this.faceBrush[faceIndex];
            }

            Pen drawingPen = new Pen(drawingBrush, DrawFaceShapeThickness);

            // draw the face bounding box
            var faceBoxSource = faceResult.FaceBoundingBoxInColorSpace;
            Rect faceBox = new Rect(faceBoxSource.Left, faceBoxSource.Top, faceBoxSource.Right - faceBoxSource.Left, faceBoxSource.Bottom - faceBoxSource.Top);
            drawingContext.DrawRectangle(null, drawingPen, faceBox);

            if (faceResult.FacePointsInColorSpace != null)
            {
                // draw each face point
                foreach (PointF pointF in faceResult.FacePointsInColorSpace.Values)
                {
                    drawingContext.DrawEllipse(null, drawingPen, new Point(pointF.X, pointF.Y), FacePointRadius, FacePointRadius);
                }
            }

            string faceText = string.Empty;

            // extract each face property information and store it in faceText
            if (faceResult.FaceProperties != null)
            {
                foreach (var item in faceResult.FaceProperties)
                {
                    faceText += item.Key.ToString() + " : ";

                    // consider a "maybe" as a "no" to restrict 
                    // the detection result refresh rate
                    if (item.Value == DetectionResult.Maybe)
                    {
                        faceText += DetectionResult.No + "\n";
                    }
                    else
                    {
                        faceText += item.Value.ToString() + "\n";
                    }
                }
            }

            // extract face rotation in degrees as Euler angles
            if (faceResult.FaceRotationQuaternion != null)
            {
                int pitch, yaw, roll;
                ExtractFaceRotationInDegrees(faceResult.FaceRotationQuaternion, out pitch, out yaw, out roll);
                faceText += "FaceYaw : " + yaw + "\n" +
                            "FacePitch : " + pitch + "\n" +
                            "FacenRoll : " + roll + "\n";
            }

            // render the face property and face rotation information
            Point faceTextLayout;
            if (this.GetFaceTextPositionInColorSpace(faceIndex, out faceTextLayout))
            {
                drawingContext.DrawText(
                        new FormattedText(
                            faceText,
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface("Georgia"),
                            DrawTextFontSize,
                            drawingBrush),
                        faceTextLayout);
            }
        }

        /// <summary>
        /// Computes the face result text position by adding an offset to the corresponding 
        /// body's head joint in camera space and then by projecting it to screen space
        /// </summary>
        /// <param name="faceIndex">the index of the face frame corresponding to a specific body in the FOV</param>
        /// <param name="faceTextLayout">the text layout position in screen space</param>
        /// <returns>success or failure</returns>
        private bool GetFaceTextPositionInColorSpace(int faceIndex, out Point faceTextLayout)
        {
            faceTextLayout = new Point();
            bool isLayoutValid = false;

            Body body = this.bodies[faceIndex];
            if (body.IsTracked)
            {
                var headJoint = body.Joints[JointType.Head].Position;

                CameraSpacePoint textPoint = new CameraSpacePoint()
                {
                    X = headJoint.X + TextLayoutOffsetX,
                    Y = headJoint.Y + TextLayoutOffsetY,
                    Z = headJoint.Z
                };

                ColorSpacePoint textPointInColor = this.coordinateMapper.MapCameraPointToColorSpace(textPoint);

                faceTextLayout.X = textPointInColor.X;
                faceTextLayout.Y = textPointInColor.Y;
                isLayoutValid = true;
            }

            return isLayoutValid;
        }

        /// <summary>
        /// Validates face bounding box and face points to be within screen space
        /// </summary>
        /// <param name="faceResult">the face frame result containing face box and points</param>
        /// <returns>success or failure</returns>
        private bool ValidateFaceBoxAndPoints(FaceFrameResult faceResult)
        {
            bool isFaceValid = faceResult != null;

            if (isFaceValid)
            {
                var faceBox = faceResult.FaceBoundingBoxInColorSpace;
                if (faceBox != null)
                {
                    // check if we have a valid rectangle within the bounds of the screen space
                    isFaceValid = (faceBox.Right - faceBox.Left) > 0 &&
                                  (faceBox.Bottom - faceBox.Top) > 0 &&
                                  faceBox.Right <= this.displayWidth &&
                                  faceBox.Bottom <= this.displayHeight;

                    if (isFaceValid)
                    {
                        var facePoints = faceResult.FacePointsInColorSpace;
                        if (facePoints != null)
                        {
                            foreach (PointF pointF in facePoints.Values)
                            {
                                // check if we have a valid face point within the bounds of the screen space
                                bool isFacePointValid = pointF.X > 0.0f &&
                                                        pointF.Y > 0.0f &&
                                                        pointF.X < this.displayWidth &&
                                                        pointF.Y < this.displayHeight;

                                if (!isFacePointValid)
                                {
                                    isFaceValid = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return isFaceValid;
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            if (this.kinectSensor != null)
            {
                // on failure, set the status text
                this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                                : Properties.Resources.SensorNotAvailableStatusText;
            }
        }

        /// <summary>
        /// Handles the depth frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_DepthFrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            bool depthFrameProcessed = false;

            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)) &&
                            (this.depthFrameDescription.Width == this.depthBitmap.PixelWidth) && (this.depthFrameDescription.Height == this.depthBitmap.PixelHeight))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue;

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            //// maxDepth = depthFrame.DepthMaxReliableDistance

                            this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);

                            depthFrameProcessed = true;
                        }
                    }
                }
            }

            if (depthFrameProcessed && kinectMode)
            {
                this.RenderPixels();

                UdpSendData();
            }

        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderPixels()
        {
            if (kinectMode)
            {
                if (faceMode)
                {
                    //将面部数据传送待显示图像数据
                    WallPixels2ImagePixels();
                }
                else
                {
                    if (depthScreen)
                    {
                        //将人体深度数据(424*512)传送给待显示图像数据
                        Array.Copy(depthPixels, imagePixels, depthPixels.Length);
                    }
                    else 
                    {
                        //将人体深度数据(35*60)传送给待显示图像数据
                        WallPixels2ImagePixels();
                    }
                }
            }
            else
            {
                //将动画数据传递给待显示图像数据
                WallPixels2ImagePixels();
            }

            this.depthBitmap.WritePixels(
                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                this.imagePixels,
                this.depthBitmap.PixelWidth,
                0);
        }

        /// <summary>
        /// Directly accesses the underlying image buffer of the DepthFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the depthFrameData pointer.
        /// </summary>
        /// <param name="depthFrameData">Pointer to the DepthFrame image data</param>
        /// <param name="depthFrameDataSize">Size of the DepthFrame image data</param>
        /// <param name="minDepth">The minimum reliable depth value for the frame</param>
        /// <param name="maxDepth">The maximum reliable depth value for the frame</param>
        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            if (faceMode)
            {
                for(int i=0;i<wallHeight;i++)
                {
                    for (int j = 0; j < wallWidth; j++)
                    {
                        wallPixels[i, j] = (byte)(faceWithNoMotion[i, j] * 64);
                    }
                }
                //Array.Copy(faceWithNoMotion, wallPixels, wallPixels.Length);
            }
            else
            {
                DepthFrameData2WallPixels(depthFrameData);
            }

            if (depthScreen)
            {
                //WallPixels2ImagePixels();
                // convert depth to a visual representation
                for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
                {
                    // Get the depth for this pixel
                    ushort depth = frameData[i];

                    // To convert to a byte, we're mapping the depth value to the byte range.
                    // Values outside the reliable depth range are mapped to 0 (black).
                    this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
                }
            }
            
        }

        /// <summary>
        /// Handles the body index frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_BodyIndexFrameArrived(object sender, BodyIndexFrameArrivedEventArgs e)
        {
            using (BodyIndexFrame bodyIndexFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyIndexFrame != null)
                {
                    bodyIndexFrame.CopyFrameDataToArray(bodyIndexDataArray);
                }
            }
        }

        /// <summary>
        /// Find if there is a body tracked with the given trackingId
        /// </summary>
        /// <param name="bodyFrame">A body frame</param>
        /// <param name="trackingId">The tracking Id</param>
        /// <returns>The body object, null of none</returns>
        private static Body FindBodyWithTrackingId(BodyFrame bodyFrame, ulong trackingId)
        {
            Body result = null;

            Body[] bodies = new Body[bodyFrame.BodyCount];
            bodyFrame.GetAndRefreshBodyData(bodies);

            foreach (var body in bodies)
            {
                if (body.IsTracked)
                {
                    if (body.TrackingId == trackingId)
                    {
                        result = body;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Finds the closest body from the sensor if any
        /// </summary>
        /// <param name="bodyFrame">A body frame</param>
        /// <returns>Closest body, null of none</returns>
        private static Body FindClosestBody(BodyFrame bodyFrame)
        {
            Body result = null;
            double closestBodyDistance = double.MaxValue;

            Body[] bodies = new Body[bodyFrame.BodyCount];
            bodyFrame.GetAndRefreshBodyData(bodies);

            foreach (var body in bodies)
            {
                if (body.IsTracked)
                {
                    var currentLocation = body.Joints[JointType.SpineBase].Position;

                    var currentDistance = VectorLength(currentLocation);

                    if (result == null || currentDistance < closestBodyDistance)
                    {
                        result = body;
                        closestBodyDistance = currentDistance;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the length of a vector from origin
        /// </summary>
        /// <param name="point">Point in space to find it's distance from origin</param>
        /// <returns>Distance from origin</returns>
        private static double VectorLength(CameraSpacePoint point)
        {
            var result = Math.Pow(point.X, 2) + Math.Pow(point.Y, 2) + Math.Pow(point.Z, 2);

            result = Math.Sqrt(result);

            return result;
        }



        private void SwitchModeButton_Click(object sender, RoutedEventArgs e)
        {
            kinectMode = !kinectMode;
            if (kinectMode)
            {
                timer.Change(Timeout.Infinite, 100);
            }
            else
            {
                timer.Change(0, 100);
            }
        }


        private void Animaion1Button_Click(object sender, RoutedEventArgs e)
        {
            animation = Animation.byOrder;
            timerCounter = 0;
        }

       
        private void SwitchScreen_Click(object sender, RoutedEventArgs e)
        {
            depthScreen = !depthScreen;
        }

        private void FaceMode_Click(object sender, RoutedEventArgs e)
        {
            faceMode = !faceMode;
        }


    }
}
