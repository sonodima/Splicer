//
//  MainWindow.xaml.cs
//  Splicer
//
//  Written with love by sonodima
//

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Splicer
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);



        // Window Focus
        private bool splice_focussed;
        private int focussed_pid;

        // Peek Meter
        private int peek_value;
        private int last_peek;
        private DoubleAnimation peek_animation;
        private bool can_animate;

        // Folders / Paths
        public static string splice_folder;
        public static string splicer_folder;

        // Processes
        private Process[] splice_processes;
        private Process splice_process;

        // Splicer Classes
        private SplicerRecorder splicer_recorder;
        private SplicerFile splicer_file;



        public MainWindow()
        {
            InitializeComponent();

            splice_focussed = false;
            focussed_pid = 0;
            peek_value = 0;
            last_peek = 0;
            peek_animation = new DoubleAnimation();
            can_animate = false;
            splice_folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\splice";
            splicer_folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Splicer";
            splicer_recorder = new SplicerRecorder();
            splicer_file = new SplicerFile();
        }



        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // If it doesn't exist, create a workspace in Documents.
            if (!Directory.Exists(splicer_folder))
            {
                Directory.CreateDirectory(splicer_folder);
            }


            // Timer that will update the peek volume.
            DispatcherTimer update_peek_value = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            update_peek_value.Tick += UpdatePeekValue_Tick;
            update_peek_value.IsEnabled = true;


            // Timer that will update Splice process's status and window focus.
            DispatcherTimer update_splice_window = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            update_splice_window.Tick += UpdateSpliceWindow_Tick;
            update_splice_window.IsEnabled = true;
        }



        private void UpdatePeekValue_Tick(object sender, EventArgs e)
        {
            if (splicer_recorder.is_recording)
            {
                peek_value = (int)(splicer_recorder.peek_value * 2.8);

                peek_animation.From = last_peek;
                peek_animation.To = splicer_recorder.peek_value * 2.8;
                peek_animation.Duration = TimeSpan.FromMilliseconds(100);
                gainMeter.BeginAnimation(Rectangle.WidthProperty, peek_animation);

                last_peek = peek_value;

                capturingCanvas.Visibility = Visibility.Visible;
                startButton.IsEnabled = false;
                autoToggle.IsEnabled = false;
                stopButton.IsEnabled = (bool)!autoToggle.IsChecked;
   
                can_animate = true;
            }
            else
            {
                if (can_animate)
                {
                    ProcessRecording();

                    peek_animation.From = last_peek;
                    peek_animation.To = 0;
                    peek_animation.Duration = TimeSpan.FromMilliseconds(500);
                    gainMeter.BeginAnimation(Rectangle.WidthProperty, peek_animation);

                    last_peek = 0;
                    peek_value = 0;

                    capturingCanvas.Visibility = Visibility.Hidden;
                    startButton.IsEnabled = true;
                    stopButton.IsEnabled = false;
                    autoToggle.IsEnabled = true;

                    can_animate = false;
                }
            }
        }



        private void UpdateSpliceWindow_Tick(object sender, EventArgs e)
        {
            // Check whether Splice is running or not. If it's not, launch it.
            splice_processes = Process.GetProcessesByName("Splice");
            if (splice_processes.Length == 0)
            {
                if (!File.Exists(splice_folder + "\\Splice.exe"))
                {
                    MessageBox.Show("Unable to start Splice.exe.\nPlease run it and open Splicer again.", "Unable to start Splice", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(1);
                }

                Process.Start(splice_folder + "\\Splice.exe");
                Thread.Sleep(200);
                Process.Start(splice_folder + "\\Splice.exe");
                Thread.Sleep(200);

                do
                {
                    splice_processes = Process.GetProcessesByName("Splice");
                    Thread.Sleep(200);
                } while (splice_processes.Length != 3);
            }


            // Get main Splice process.
            foreach (Process process in splice_processes)
            {
                if (process.MainWindowHandle != (IntPtr)0)
                {
                    splice_process = process;
                }
            }


            if (splice_process != null && splice_process.MainWindowHandle != (IntPtr)0)
            {
                // Handle focus.
                GetWindowThreadProcessId(GetForegroundWindow(), out focussed_pid);
                if (focussed_pid == splice_process.Id || focussed_pid == Process.GetCurrentProcess().Id)
                {
                    if (splice_focussed == false)
                    {
                        SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
                        SetForegroundWindow(splice_process.MainWindowHandle);
                        splice_focussed = true;
                    }
                }
                else if (splice_focussed == true)
                {
                    splice_focussed = false;
                }
            }
            else
            {
                splice_focussed = false;
            }
        }



        private void ProcessRecording()
        {
            splicer_file.Process();

            nameLabel.Content = splicer_file.name;
            weightLabel.Content = splicer_file.weight / 1024 + "kb";
            lengthLabel.Content = splicer_file.length.Seconds + "s";
        }



        private void DragEvent(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string[] file_list = new string[] { splicer_folder + "\\" + splicer_file.name + ".wav" };
            DataObject file_drag_data = new DataObject(DataFormats.FileDrop, file_list);
            DragDrop.DoDragDrop(this, file_drag_data, DragDropEffects.Copy);
        }



        // Window / Controls

        private void windowDragger_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            splicer_recorder.Start();
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            splicer_recorder.Stop();
        }

        private void autoToggle_Click(object sender, RoutedEventArgs e)
        {
            splicer_recorder.auto_mode = (bool)autoToggle.IsChecked;
        }
    }
}