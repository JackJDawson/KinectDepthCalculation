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

        private int maxIntensityChosen = 8192;

        string csvname;
        StreamWriter write;

        public MainWindow()
        {
            InitializeComponent();
            this.sensor = KinectSensor.GetDefault();
            this.sensor.Open();

            this.frameReader = sensor.DepthFrameSource.OpenReader();
            this.frameReader.FrameArrived += this.Reader_DepthFrameArrived;

            this.DepthFrameData = new ushort[sensor.DepthFrameSource.FrameDescription.Width * sensor.DepthFrameSource.FrameDescription.Height];
            this.depthPixels = new byte[sensor.DepthFrameSource.FrameDescription.Width * sensor.DepthFrameSource.FrameDescription.Height];

            this.bitmap = new WriteableBitmap(sensor.DepthFrameSource.FrameDescription.Width, sensor.DepthFrameSource.FrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            this.DataContext = this;


          /*  Console.WriteLine(Directory.GetCurrentDirectory());
            csvname = String.Format("{0}\\{1}.csv",Directory.GetCurrentDirectory(), System.DateTime.Now.Ticks);
            write = new StreamWriter(csvname);
            write.WriteLine("Timestamp,AvgDepth");
            write.Flush();
            write.AutoFlush = true; */

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
                else
                {
                    Console.WriteLine("halp");
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

            for (int i = 0; i < this.DepthFrameData.Length; i++)
            {
                float intensity = this.DepthFrameData[i];

                intensity = (intensity >= sensor.DepthFrameSource.DepthMinReliableDistance && intensity <= sensor.DepthFrameSource.DepthMaxReliableDistance) ? intensity / 256 * 8000 : 0;

                this.depthPixels[i] = (byte) intensity;
            }


            this.bitmap.WritePixels(
                 new Int32Rect(0, 0, this.bitmap.PixelWidth, this.bitmap.PixelHeight),
                 this.depthPixels,
                 this.bitmap.PixelWidth,
                 0) ;





            /*              Region1DepthValue.Text = region1depth.ToString();
                      //  Region2DepthValue.Text= region2depth.ToString();
                      //Write depth data to CSV
                  if (region1depth > 0)
                      {

                          write.WriteLine(String.Format("{0}:{1}:{2}:{3},{4}", System.DateTime.Now.Hour, System.DateTime.Now.Minute, System.DateTime.Now.Second, System.DateTime.Now.Millisecond, region1depth));

                          region1bmp = new WriteableBitmap((int)Math.Abs(region1Corners.Item1.X - region1Corners.Item2.X), (int) Math.Abs(region1Corners.Item1.Y - region1Corners.Item2.Y), 96, 96, PixelFormats.Gray8, null);
                          region1bmp.WritePixels(new Int32Rect(0, 0, region1bmp.PixelWidth, region1bmp.PixelHeight), region1Depths, region1bmp.PixelWidth, 0);

                          BitmapEncoder encoder = new PngBitmapEncoder();
                          encoder.Frames.Add(BitmapFrame.Create(region1bmp));

                          try
                          {
                              using (FileStream fs = new FileStream(String.Format("{0}\\{1}.png", Directory.GetCurrentDirectory(), System.DateTime.Now.Ticks), FileMode.Create))
                              {
                                  encoder.Save(fs);
                              }
                          }
                          catch (Exception e)
                          {
                              Console.WriteLine(e.ToString());
                          }


                          region1Depths = new ushort[region1Depths.Length];
                      } 
           */
        }

        //In theory, the Kinect will clean up after itself. In practice, it sometimes doesn't. Either way, it's not a huge deal to clean up for it.
        void MainWindow_Closing(object sender, ConsoleCancelEventArgs e)
        {
            //Clean up streamwriter
            write.Flush();
            write.Close();

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




        private void depthImageViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {

                Point mousePos = Mouse.GetPosition(depthImageViewer);

                mousePos.X = Math.Floor(mousePos.X);
                mousePos.Y = Math.Floor(mousePos.Y);

        }

        private void processRegions()
        {

            double frameHeight = depthImageViewer.ActualHeight;
            double frameWidth = depthImageViewer.ActualWidth;
            int pixelHeight = this.bitmap.PixelHeight;
            int pixelWidth = this.bitmap.PixelWidth;

            double verticalScale = frameHeight / pixelHeight;
            double horizontalScale = frameWidth / pixelWidth;

        }
            
    }
}
