using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NAudio.Wave;
using TorchSharp;
using TorchSharp.Tensor;
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

        public string ProcessAudio(string inputFilePath, string outputFolder) {
            try {
                if (!File.Exists(inputFilePath)) {
                    throw new FileNotFoundException($"Input file not found: {inputFilePath}");
                }

                string tempFile = Path.Combine(Path.GetTempPath(), "temp_resampled.wav");

                // 🔹 Step 1: Ensure input WAV is resampled to 44100 Hz
                WavUtils.ResampleTo44100Hz(inputFilePath, tempFile);

                // 🔹 Step 2: Prepare output file path
                string outputFilePath = Path.Combine(outputFolder, "rvc_output.wav");

                // 🔹 Step 3: Run inference
                float[] audioData = WavUtils.ReadWavToFloatArray(tempFile);
                float[] processedAudio = RunInference(audioData);

                // 🔹 Step 4: Save processed audio
                WavUtils.WriteFloatArrayToWav(outputFilePath, processedAudio, 44100);

                return outputFilePath;
            }
            catch (Exception ex) {
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
