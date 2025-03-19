using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OpenUtau.RVC.Utils {
    public static class WavUtils {
        private const int DefaultSampleRate = 44100; // Ensuring compatibility with UTAU

        /// <summary>
        /// Loads a WAV file and returns a float array of audio samples.
        /// Always ensures the sample rate is 44100 Hz for compatibility.
        /// </summary>
        public static float[] ReadWavToFloatArray(string filePath, out int sampleRate, out int channels) {
            using var reader = new WaveFileReader(filePath);
            sampleRate = reader.WaveFormat.SampleRate;
            channels = reader.WaveFormat.Channels;
            var sampleProvider = reader.ToSampleProvider();

            float[] samples = new float[reader.SampleCount];
            sampleProvider.Read(samples, 0, (int)reader.SampleCount);

            return samples;
        }

        /// <summary>
        /// Saves an array of float samples as a WAV file with a fixed 44100 Hz sample rate.
        /// </summary>
        public static void WriteFloatArrayToWav(string filePath, float[] samples, int channels) {
            using var writer = new WaveFileWriter(filePath, new WaveFormat(DefaultSampleRate, 16, channels));
            var byteBuffer = new byte[samples.Length * 2];

            for (int i = 0; i < samples.Length; i++) {
                short sample = (short)(samples[i] * short.MaxValue);
                BitConverter.GetBytes(sample).CopyTo(byteBuffer, i * 2);
            }

            writer.Write(byteBuffer, 0, byteBuffer.Length);
        }

        /// <summary>
        /// Resamples a WAV file to 44100 Hz if it's not already at that rate.
        /// </summary>
        public static void ResampleTo44100Hz(string inputPath, string outputPath) {
            using var reader = new WaveFileReader(inputPath);
            if (reader.WaveFormat.SampleRate == DefaultSampleRate) {
                File.Copy(inputPath, outputPath, true);
                return;
            }

            using var resampler = new MediaFoundationResampler(reader, new WaveFormat(DefaultSampleRate, reader.WaveFormat.Channels)) {
                ResamplerQuality = 60
            };
            WaveFileWriter.CreateWaveFile(outputPath, resampler);
        }

        /// <summary>
        /// Merges multiple WAV files into one.
        /// </summary>
        public static void MergeWavFiles(string outputFilePath, params string[] inputFilePaths) {
            using var outputWriter = new WaveFileWriter(outputFilePath, new WaveFormat(DefaultSampleRate, 16, 1));
            foreach (var filePath in inputFilePaths) {
                using var reader = new WaveFileReader(filePath);
                var buffer = new byte[reader.Length];
                int bytesRead = reader.Read(buffer, 0, buffer.Length);
                outputWriter.Write(buffer, 0, bytesRead);
            }
        }

        /// <summary>
        /// Normalizes the audio level of a WAV file.
        /// </summary>
        public static void NormalizeWav(string inputPath, string outputPath, float targetLevel = 0.9f) {
            float[] samples = ReadWavToFloatArray(inputPath, out int sampleRate, out int channels);
            float maxSample = 0f;

            foreach (var sample in samples) {
                if (Math.Abs(sample) > maxSample) {
                    maxSample = Math.Abs(sample);
                }
            }

            if (maxSample == 0f) return; // Avoid division by zero

            float gain = targetLevel / maxSample;
            for (int i = 0; i < samples.Length; i++) {
                samples[i] *= gain;
            }

            WriteFloatArrayToWav(outputPath, samples, channels);
        }

        /// <summary>
        /// Trims silence from the beginning and end of a WAV file.
        /// </summary>
        public static void TrimSilence(string inputPath, string outputPath, float silenceThreshold = 0.01f) {
            float[] samples = ReadWavToFloatArray(inputPath, out int sampleRate, out int channels);
            int start = 0, end = samples.Length - 1;

            while (start < samples.Length && Math.Abs(samples[start]) < silenceThreshold) {
                start++;
            }

            while (end > start && Math.Abs(samples[end]) < silenceThreshold) {
                end--;
            }

            int newLength = end - start + 1;
            float[] trimmedSamples = new float[newLength];
            Array.Copy(samples, start, trimmedSamples, 0, newLength);

            WriteFloatArrayToWav(outputPath, trimmedSamples, channels);
        }
    }
}
