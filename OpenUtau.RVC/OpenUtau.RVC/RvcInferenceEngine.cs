using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;
using TorchSharp;
using TorchSharp.Modules;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenUtau.RVC.Utils; // Ensure correct WavUtils namespace

namespace OpenUtau.RVC.Processing {
    public class RvcInferenceEngine {
        private RvcInferenceEngine engine;
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
                onnxSession = new InferenceSession(modelPath);
                Debug.WriteLine("✅ ONNX model loaded successfully.");
            } else {
                torchModel = torch.jit.load(modelPath);
                Debug.WriteLine($"✅ Torch (.pth) model loaded successfully. Model Type: {torchModel?.GetType()}");
            }
        }

        public async Task ProcessAsync(string inputWav, string outputWav, double pitchShift, Action<int> progressCallback) {
            try {
                // Ensure model is loaded
                if (torchModel == null && !isOnnx) {
                    LoadModel();
                }

                // Log selected model/index for debug
                Console.WriteLine($"🎯 Model: {modelPath}");
                Console.WriteLine($"🎯 Index: {indexPath}");

                // Read input audio
                float[] inputAudio = WavUtils.ReadWavToFloatArray(inputWav, out int sampleRate, out int channels);
                if (inputAudio == null || inputAudio.Length == 0) {
                    throw new Exception("❌ Failed to read input WAV.");
                }

                // Simulate pitch shift (you'll later implement real pitch logic)
                Console.WriteLine($"🎵 Pitch shift: {pitchShift}");

                // Run inference
                float[] outputAudio = RunInference(inputAudio);

                // Write output
                WavUtils.WriteFloatArrayToWav(outputWav, outputAudio, 44100, channels);

                // Report progress
                progressCallback?.Invoke(100);
                Console.WriteLine("✅ RVC Inference completed.");
            } catch (Exception ex) {
                Console.WriteLine($"❌ Error during RVC processing: {ex.Message}");
                progressCallback?.Invoke(0); // Report failure if needed
            }
        }

        public async Task Process(string modelPath, string indexPath, string inputPath, string outputPath, double pitch, Action<double> progressCallback) {
            await Task.Delay(1000); // Simulate AI processing
            progressCallback(100);
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

        public float[] RunTorchInference(float[] inputAudio) {
            string pythonPath = "python";
            string pthModelPath = this.modelPath;
            string ptModelPath = Path.ChangeExtension(pthModelPath, ".pt");

            if (!File.Exists(ptModelPath)) {
                Console.WriteLine($"🔹 Converting {pthModelPath} to TorchScript (.pt)...");
                ConvertPthToPt(pthModelPath, ptModelPath);
            }

            if (!File.Exists(ptModelPath)) {
                Console.WriteLine("❌ Error: Failed to convert .pth to .pt");
                return new float[0];
            }

            try {
                var torchModel = torch.jit.load(ptModelPath);

                using var inputTensor = torch.tensor(inputAudio, new long[] { 1, inputAudio.Length });

                torchModel.eval();
                var result = torchModel.call(inputTensor);
                if (result is torch.Tensor outputTensor) {
                    return outputTensor.data<float>().ToArray();
                }

                throw new Exception("❌ Torch model returned an unexpected output type.");
            } catch (Exception ex) {
                Console.WriteLine($"❌ Torch Inference Error: {ex.Message}");
                return new float[0];
            }
        }

        private void ConvertPthToPt(string pthModelPath, string ptModelPath) {
            try {
                ProcessStartInfo psi = new ProcessStartInfo {
                    FileName = "python",
                    Arguments = "\"convert_pth_to_pt.py\" \"" + pthModelPath + "\" \"" + ptModelPath + "\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);
                using StreamReader reader = process.StandardOutput;
                string output = reader.ReadToEnd();
                process.WaitForExit();

                Console.WriteLine($"🔹 Conversion Output: {output}");
            } catch (Exception ex) {
                Console.WriteLine($"❌ Python Conversion Error: {ex.Message}");
            }
        }
    }
}
