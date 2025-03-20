using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            try {
                if (isOnnx) {
                    onnxSession = new InferenceSession(modelPath);
                    Debug.WriteLine("ONNX model loaded successfully.");
                } else {
                    torchModel = torch.jit.load(modelPath);
                    Debug.WriteLine("Torch (.pth) model loaded successfully.");
                }
            } catch (Exception ex) {
                Debug.WriteLine($"Error loading model: {ex.Message}");
            }
        }

        public string ProcessAudio(string inputFilePath, string outputFolder) {
            try {
                if (!File.Exists(inputFilePath)) {
                    throw new FileNotFoundException($"Input file not found: {inputFilePath}");
                }

                string tempFile = Path.Combine(Path.GetTempPath(), "temp_resampled.wav");

                // 🔹 Ensure input WAV is resampled to 44100 Hz
                WavUtils.Ensure44100Hz(inputFilePath, tempFile);

                // 🔹 Prepare output file path
                string outputFilePath = Path.Combine(outputFolder, "rvc_output.wav");

                // 🔹 Load audio data
                float[] audioData = WavUtils.LoadWav(tempFile, out int sampleRate, out int channels);
                if (audioData == null || audioData.Length == 0) {
                    throw new Exception("Failed to load input WAV.");
                }

                // 🔹 Run inference
                float[] processedAudio = RunInference(audioData);

                // 🔹 Save processed audio
                WavUtils.SaveWav(outputFilePath, processedAudio, channels);

                return outputFilePath;
            } catch (Exception ex) {
                Debug.WriteLine($"[RvcInferenceEngine] Error: {ex.Message}");
                return string.Empty;
            }
        }

        private float[] RunInference(float[] inputAudio) {
            if (isOnnx) {
                return RunOnnxInference(inputAudio);
            } else {
                return RunTorchInference(inputAudio);
            }
        }

        private float[] RunOnnxInference(float[] inputAudio) {
            var inputTensor = new DenseTensor<float>(inputAudio, new[] { 1, inputAudio.Length });

            var inputs = new NamedOnnxValue[] {
                NamedOnnxValue.CreateFromTensor("input", inputTensor) // Ensure the input name matches the model
            };

            using var results = onnxSession.Run(inputs);
            var outputTensor = results.First().AsTensor<float>();
            return outputTensor.ToArray();
        }

        private float[] RunTorchInference(float[] inputAudio) {
            using var inputTensor = torch.tensor(inputAudio, new long[] { 1, inputAudio.Length });
            using var outputTensor = torchModel.call(inputTensor).to_type(torch.ScalarType.Float32); // Ensure proper call

            return outputTensor.data<float>().ToArray();
        }
    }
}
