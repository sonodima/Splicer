//
//  SplicerFile.cs
//  Splicer
//
//  Written with love by sonodima
//

using System;
using System.IO;
using NAudio.Wave;

namespace Splicer
{
    class SplicerFile
    {
        public string name { get; set; }
        public long weight { get; set; }
        public TimeSpan length { get; set; }


        public SplicerFile()
        {
            name = "";
            weight = 0;
            length = TimeSpan.FromMilliseconds(0);
        }


        private string GenerateName()
        {
            // Create an unique string
            return "Splicer_" + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second + "." + new Random().Next(0, 9999);
        }


        public void Process()
        {
            // Generate name
            name = GenerateName();

            // Rename file from temp to target name
            File.Move(MainWindow.splicer_folder + "\\temp", MainWindow.splicer_folder + "\\" + name + ".wav");

            // Get file weight
            weight = new FileInfo(MainWindow.splicer_folder + "\\" + name + ".wav").Length;

            // Get audio length
            length = new WaveFileReader(MainWindow.splicer_folder + "\\" + name + ".wav").TotalTime;
        }
    }
}
