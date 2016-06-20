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
using Microsoft.Kinect;


namespace c_sharp_multi_source
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        hand _hand = new hand();
        IList<Body> _bodies;
       

        public MainWindow()
        {
            InitializeComponent();
        }

        private void log_changed(object sender, RoutedEventArgs e)
        {
            log.ScrollToEnd();
        }

        private void slider_forward_changed(object sender, RoutedEventArgs e)
        {
            _hand.forward_range = (int)slider_forward.Value;
            log.AppendText("Depth Forward Range is changed to"+ _hand.forward_range.ToString()+"\n");
        }

        private void slider_backward_changed(object sender, RoutedEventArgs e)
        {
            _hand.backward_range = (int)slider_backward.Value;
            log.AppendText("Depth Backward Range is changed to" + _hand.backward_range.ToString() + "\n");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            log.AppendText("load\n");
            _sensor = KinectSensor.GetDefault();
            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Body | FrameSourceTypes.BodyIndex);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            
            if (_reader != null)
            {
                _reader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            using (var frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    _hand.Update(frame);
                }
            }

            using (var frame = reference.BodyIndexFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    _hand.Update(frame);
                }
            }

            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    _bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(_bodies);
                    foreach (var body in _bodies)
                    {
                        if (body != null)
                        {
                            if (body.IsTracked)
                            {
                                _hand.CoordinateMapper = _sensor.CoordinateMapper;
                                _hand.Update(body);
                                textBox_r_hand.Text = _hand.hand_right_depth.ToString();
                                textBox_l_hand.Text = _hand.hand_left_depth.ToString();

                                //camera.Source = _hand.drawDepth();
                                camera.Source = _hand.draw_with_specific_depth();
                            }
                        }
                    }
                }
            }
        }
    }
}
