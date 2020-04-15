//
//  SplicerRecorder.cs
//  Splicer
//
//  Written with love by sonodima
//

using System;
using System.Windows;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Splicer
{
    class SplicerRecorder
    {
        private WasapiLoopbackCapture loopback_capture;
        private WaveFileWriter wave_writer;
        private MMDeviceEnumerator device_enumerator;
        private MMDevice default_device;

        private bool waiting_audio;
        private int gain_value;

        public int peek_value { get; private set; }
        public bool auto_mode { private get; set; }
        public bool is_recording { get; private set; }


        public SplicerRecorder()
        {
            loopback_capture = new WasapiLoopbackCapture();
            device_enumerator = new MMDeviceEnumerator();

            waiting_audio = true;
            gain_value = 0;

            peek_value = 0;
            auto_mode = true;
            is_recording = false;
        }


        public void Start()
        {
            peek_value = 0;
            is_recording = true;

            try
            {
                // Setup Loopack Capture with events.
                loopback_capture = new WasapiLoopbackCapture();
                loopback_capture.StartRecording();
                loopback_capture.RecordingStopped += OnRecordingStopped;
                loopback_capture.DataAvailable += CaptureOnDataAvailable;
            }
            catch (Exception)
            {
                MessageBox.Show("There was an error using loopback device.", "Loopback error", MessageBoxButton.OK, MessageBoxImage.Error);
                is_recording = false;
            }
        }


        public void Stop()
        {
            loopback_capture.StopRecording();
        }


        private void GetPeek()
        {
            // Get current peek value.
            default_device = device_enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            peek_value = (int)(default_device.AudioMeterInformation.MasterPeakValue * 100 + 0.5);
        }


        private void CaptureOnDataAvailable(object sender, WaveInEventArgs waveInEventArgs)
        {
            // Create a writer.
            if (wave_writer == null)
            {
                wave_writer = new WaveFileWriter(MainWindow.splicer_folder + "\\temp", loopback_capture.WaveFormat);
            }

            // Get current gain.
            gain_value = Math.Abs(BitConverter.ToInt16(waveInEventArgs.Buffer, (waveInEventArgs.BytesRecorded - 2) < 0 ? 0 : (waveInEventArgs.BytesRecorded - 2)));

            if (gain_value > 0)
            {
                // If not silent, write on file writer and update peek value.
                waiting_audio = false;
                wave_writer.Write(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);
                GetPeek();
            }
            else if (waiting_audio == false && auto_mode)
            {
                // If silent and in auto mode, stop recording.
                waiting_audio = true;
                Stop();
            }
        }


        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            // Dispose file writer and capture device.
            wave_writer.Dispose();
            loopback_capture.Dispose();
            wave_writer = null;
            loopback_capture = null;

            // Check for errors
            if (e.Exception != null)
            {
                MessageBox.Show("There was an error saving the file.", "Saving error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            is_recording = false;
        }
    }
}
