import torch
import numpy as np
import sys

def load_model(model_path):
    print(f"🔹 Loading model from: {model_path}")
    model = torch.load(model_path, map_location="cpu")  # Load .pth model
    model.eval()
    return model

def run_inference(model_path, input_audio):
    model = load_model(model_path)

    # Convert input array to PyTorch tensor
    input_tensor = torch.tensor(input_audio, dtype=torch.float32).unsqueeze(0)

    with torch.no_grad():
        output = model(input_tensor)

    return output.squeeze().numpy().tolist()  # Convert back to list

if __name__ == "__main__":
    # Get parameters from C# (model path + input audio)
    model_path = sys.argv[1]
    input_audio = list(map(float, sys.argv[2:]))  # Read input audio as list of floats
    
    # Run inference
    output_audio = run_inference(model_path, input_audio)
    
    # Print output as space-separated values for C# to read
    print(" ".join(map(str, output_audio)))
