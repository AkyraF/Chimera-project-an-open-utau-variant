using System;
using System.IO;
using NAudio.Wave;

namespace OpenUtau.RVC.Utils {
    public static class WavUtils {
        private const int DefaultSampleRate = 44100; // Ensuring compatibility with UTAU

        /// <summary>
        /// Loads a WAV file and returns a float array of audio samples.
        /// </summary>
        public static float[] ReadWavToFloatArray(string filePath, out int sampleRate, out int channels) {
            using (var reader = new WaveFileReader(filePath)) {
                sampleRate = reader.WaveFormat.SampleRate;
                channels = reader.WaveFormat.Channels;
                var sampleProvider = reader.ToSampleProvider();

                var sampleBuffer = new float[reader.SampleCount];
                sampleProvider.Read(sampleBuffer, 0, (int)reader.SampleCount);
                return sampleBuffer;
            }
        }

        /// <summary>
        /// Saves a float array of audio samples as a WAV file.
        /// </summary>
        public static void WriteFloatArrayToWav(string filePath, float[] samples, int sampleRate, int channels) {
            using (var writer = new WaveFileWriter(filePath, new WaveFormat(sampleRate, 16, channels))) {
                var byteBuffer = new byte[samples.Length * 2];

                for (int i = 0; i < samples.Length; i++) {
                    short sample = (short)(samples[i] * short.MaxValue);
                    BitConverter.GetBytes(sample).CopyTo(byteBuffer, i * 2);
                }

                writer.Write(byteBuffer, 0, byteBuffer.Length);
            }
        }
    }
}
