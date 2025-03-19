using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenUtau.RVC.Utils; // Ensure WavUtils is included

namespace OpenUtau.RVC.Processing {
    public class RvcInferenceEngine {
        private readonly string modelPath;
        private readonly string indexPath;
        private readonly bool isOnnx;

        private InferenceSession onnxSession;
        private torch.nn.Module torchModel;

        public RvcInferenceEngine(string modelPath, string indexPath) {
            this.modelPath = modelPath;
            this.indexPath = indexPath;
            this.isOnnx = modelPath.EndsWith(".onnx");

            LoadModel();
        }

        private void LoadModel() {
            if (isOnnx) {
                // Load ONNX model
                onnxSession = new InferenceSession(modelPath);
                Debug.WriteLine("ONNX model loaded successfully.");
            } else {
                // Load Torch (.pth) model
                torchModel = torch.jit.load(modelPath);
                Debug.WriteLine("Torch (.pth) model loaded successfully.");
            }
        }

        public void ProcessAsync(string modelPath, string indexPath, string inputWav, string outputWav, double pitchShift, Action<int> progressCallback) {
            try {
                // Load the model if not already loaded
                if (torchModel == null && !isOnnx) {
                    LoadModel();
                }

                // Load input WAV
                float[] inputAudio = WavUtils.LoadWav(inputWav, out int sampleRate, out int channels);
                if (inputAudio == null || inputAudio.Length == 0) {
                    throw new Exception("Failed to read input WAV.");
                }

                // Run inference
                float[] outputAudio = RunInference(inputAudio);

                // Save output WAV
                WavUtils.SaveWav(outputWav, outputAudio, channels);

                // Mark process as completed
                progressCallback?.Invoke(100);
            } catch (Exception ex) {
                Console.WriteLine($"Error during RVC processing: {ex.Message}");
            }
        }

        public string ProcessAudio(string inputFilePath, string outputFolder) {
            try {
                if (!File.Exists(inputFilePath)) {
                    throw new FileNotFoundException($"Input file not found: {inputFilePath}");
                }

                string tempFile = Path.Combine(Path.GetTempPath(), "temp_resampled.wav");

                // 🔹 Step 1: Ensure input WAV is resampled to 44100 Hz
                WavUtils.Ensure44100Hz(inputFilePath, tempFile);

                // 🔹 Step 2: Prepare output file path
                string outputFilePath = Path.Combine(outputFolder, "rvc_output.wav");

                // 🔹 Step 3: Run inference
                float[] audioData = WavUtils.LoadWav(tempFile, out int sampleRate, out int channels);
                float[] processedAudio = RunInference(audioData);

                // 🔹 Step 4: Save processed audio
                WavUtils.SaveWav(outputFilePath, processedAudio, channels);

                return outputFilePath;
            } catch (Exception ex) {
                Debug.WriteLine($"[RvcInferenceEngine] Error: {ex.Message}");
                return string.Empty;
            }
        }

        private float[] RunInference(float[] inputAudio) {
            return isOnnx ? RunOnnxInference(inputAudio) : RunTorchInference(inputAudio);
        }

        private float[] RunOnnxInference(float[] inputAudio) {
            var inputTensor = new DenseTensor<float>(inputAudio, new[] { 1, inputAudio.Length });

            var inputs = new NamedOnnxValue[] {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };

            using var results = onnxSession.Run(inputs);
            var outputTensor = results.First().AsTensor<float>();
            return outputTensor.ToArray();
        }

        private float[] RunTorchInference(float[] inputAudio) {
            using var inputTensor = torch.tensor(inputAudio, new long[] { 1, inputAudio.Length });
            using var outputTensor = torchModel.forward(inputTensor).to_type(torch.ScalarType.Float32);

            return outputTensor.data<float>().ToArray();
        }
    }
}
