using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;



namespace KinectDepthCalculation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor sensor = null;
        private DepthFrameReader frameReader = null;

        private ushort[] DepthFrameData = null;
        private byte[] depthPixels = null;
        
        private WriteableBitmap bitmap = null;

        private const float MAX_DEPTH_INTENSITY = 8192;

        private const float MAX_DEPTH_OUTPUT = 255;
        private const float MIN_DEPTH_OUTPUT = 1;

        private int maxIntensityChosen = 8192;

        private double region1depth=-1;
        private double region2depth=-1;

        private int drawingRegion = 0;

        private bool pointSelected = false;
        private Point currentPoint;

        //TODO Redefine this to just use 
        private Tuple<Point, Point> region1Corners;
        private Tuple<Point, Point> region2Corners;

        //Will return true if a given pixel, defined by position running LTR is located within the bounds of the region. Initially returns false since no regions are defined.
        private Func<int, bool> pixelInRegion1 = (int pixel) => false;
        private Func<int, bool> pixelInRegion2 = (int pixel) => false;

        private ushort[] region1Depths;
        private ushort[] region2Depths;


        public MainWindow()
        {
            InitializeComponent();
            this.sensor = KinectSensor.GetDefault();
            this.sensor.Open();

            this.frameReader = sensor.DepthFrameSource.OpenReader();
            this.frameReader.FrameArrived += this.Reader_DepthFrameArrived;

            this.DepthFrameData = new ushort[sensor.DepthFrameSource.FrameDescription.Width * sensor.DepthFrameSource.FrameDescription.Height];
            this.depthPixels = new byte[sensor.DepthFrameSource.FrameDescription.Width * sensor.DepthFrameSource.FrameDescription.Height];

            this.bitmap = new WriteableBitmap(sensor.DepthFrameSource.FrameDescription.Width, sensor.DepthFrameSource.FrameDescription.Height, 96.0,96.0,PixelFormats.Gray8, null);

            this.DataContext = this;

            this.drawingRegion = 0;

            this.pointSelected = false;
    }

        private void Reader_DepthFrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            bool DepthFrameProcessed = false;

            using (DepthFrame DepthFrame = e.FrameReference.AcquireFrame())
            {
                if (DepthFrame != null)
                {
                    FrameDescription DepthFrameDescription = DepthFrame.FrameDescription;

                    if (
                        ((DepthFrameDescription.Width * DepthFrameDescription.Height) == this.DepthFrameData.Length) 
                        && (DepthFrameDescription.Width == this.bitmap.PixelWidth) 
                        && (DepthFrameDescription.Height == this.bitmap.PixelHeight)
                        )
                    {
                        DepthFrame.CopyFrameDataToArray(this.DepthFrameData);

                        DepthFrameProcessed = true;
                    }
                }
            }

            if (DepthFrameProcessed)
            {
                // do conversion to bitmap
                handleDepthFrame();
            }
        }

        //TODO Split processing and rendering - ideally processing will be framerate-independent in case of slow devices
        private void handleDepthFrame()
        {
            int region1iterator = 0;
            int region2iterator = 0;

            region1depth = 0;
            region2depth = 0;


            for (int i = 0; i < this.DepthFrameData.Length; i++)
            {
                ushort depth = this.DepthFrameData[i];

                float intensity = depth / (float)maxIntensityChosen;
                intensity = Math.Min(MAX_DEPTH_OUTPUT, Math.Max(MIN_DEPTH_OUTPUT, intensity * 255));

                this.depthPixels[i] = (byte)(intensity);

                if (pixelInRegion1(i))
                {
                    region1Depths[region1iterator] = (ushort)intensity;
                    region1depth += intensity / region1Depths.Length;
                    region1iterator++;
                } 
                if (pixelInRegion2(i))
                {
                    region2Depths[region2iterator] = (ushort)intensity;
                    region2depth += intensity / region2Depths.Length;
                    region2iterator++;
                }

            }

            this.bitmap.WritePixels(
                 new Int32Rect(0, 0, this.bitmap.PixelWidth, this.bitmap.PixelHeight),
                 this.depthPixels,
                 this.bitmap.PixelWidth,
                 0);

            using (FileStream stream = new FileStream(String.Format("\\{0}.png", System.DateTime.Now.Ticks), FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(this.bitmap));
                encoder.Save(stream);
            }

            Region1DepthValue.Text = region1depth.ToString();
            Region2DepthValue.Text= region2depth.ToString();

        }

        //In theory, the Kinect will clean up after itself. In practice, it sometimes doesn't. Either way, it's not a huge deal to clean up for it.
        void MainWindow_Closing(object sender, ConsoleCancelEventArgs e)
        {
            if (this.sensor != null)
            {
                this.sensor.Close();
            }

            if (this.frameReader != null)
            {
                this.frameReader.Dispose(); 
            }
        }


        private void DepthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            maxIntensityChosen = (int)e.NewValue;
        }

        public ImageSource DepthImageSource
        {
            get { return this.bitmap; }
        }

        public double MaximumDepth
        {
            get { return MAX_DEPTH_INTENSITY; }
        }

        //TODO Maybe rewrite region handling to use n regions, rather than this dumb code duplication
        public String Region1Depth
        {
            get { return this.region1depth.ToString(); }
        }

        public String Region2Depth
        {
            get { return this.region2depth.ToString();}
        }

        private void Button_Draw_Region_1_Click(object sender, RoutedEventArgs e)
        {
            if (this.drawingRegion == 1)
            {
                this.drawingRegion = 0;
            }
            else
            {
                this.drawingRegion = 1;
            }
        }

        private void Button_Draw_Region_2_Click(object sender, RoutedEventArgs e)
        {
            if (this.drawingRegion == 2)
            {
                this.drawingRegion = 0;   
            }
            else
            {
                this.drawingRegion = 2;
            }
        }

        private void depthImageViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.drawingRegion != 0)
            {
                Point mousePos = Mouse.GetPosition(depthImageViewer);


                if (!this.pointSelected)
                { 
                    this.currentPoint = mousePos;
                    this.pointSelected = true;
                    
                    // TODO Visual indicator of selected point - small + or something
                }
                else { 
                    if (this.drawingRegion == 1)
                    {
                        this.region1Corners = new Tuple<Point, Point>(this.currentPoint, mousePos);
                    }else if(this.drawingRegion == 2)
                    {
                        this.region2Corners = new Tuple<Point, Point>(this.currentPoint, mousePos);
                    }
                    this.pointSelected = false;
                    this.drawingRegion = 0;

                    //TODO Calculate regions of interest
                    processRegions();

                    //TODO Visual indication of selected regions - probably just red rectangle
                 }
        
            }
        }

        private void processRegions()
        {

            double frameHeight = depthImageViewer.ActualHeight;
            double frameWidth = depthImageViewer.ActualWidth;
            int pixelHeight = this.bitmap.PixelHeight;
            int pixelWidth = this.bitmap.PixelWidth;

            double verticalScale = frameHeight / pixelHeight;
            double horizontalScale = frameWidth / pixelWidth;

            //Generate a list of pixels in region 1

            if (this.region1Corners != null)
            {
                double minX = Math.Min(this.region1Corners.Item1.X, this.region1Corners.Item2.X) / verticalScale;
                double maxX = Math.Max(this.region1Corners.Item1.X, this.region1Corners.Item2.X) / verticalScale;
                double minY = Math.Min(this.region1Corners.Item1.Y, this.region1Corners.Item2.Y) / verticalScale;
                double maxY = Math.Max(this.region1Corners.Item1.Y, this.region1Corners.Item2.Y) / verticalScale;

                //Define lambda expression to determine whether a point is within the requested bounds

                pixelInRegion1 = (int pixel) => (pixel % pixelWidth >= minY && pixel % pixelWidth <= maxY && pixel / pixelWidth >= minX && pixel / pixelWidth <= maxX);
                this.region1Depths = new ushort[(int)((maxX - minX) * (maxY - minY))];
            }
            //Ditto for region 2

            if (this.region2Corners != null) 
            {
                double minX = Math.Min(this.region2Corners.Item1.X, this.region2Corners.Item2.X) / verticalScale;
                double maxX = Math.Max(this.region2Corners.Item1.X, this.region2Corners.Item2.X) / verticalScale;
                double minY = Math.Min(this.region2Corners.Item1.Y, this.region2Corners.Item2.Y) / verticalScale;
                double maxY = Math.Max(this.region2Corners.Item1.Y, this.region2Corners.Item2.Y) / verticalScale;

                //Define lambda expression to determine whether a point is within the requested bounds

                pixelInRegion2 = (int pixel) => (pixel % pixelWidth >= minY && pixel % pixelWidth <= maxY && pixel / pixelWidth >= minX && pixel / pixelWidth <= maxX);
                this.region2Depths = new ushort[(int)((maxX - minX) * (maxY - minY))];
            }
        }
    }
}
