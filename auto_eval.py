import os
import argparse
from concurrent.futures import ThreadPoolExecutor
import re


"""
This script evaluates all models in the ml-agents results directory.
It assumes that the models are in subfolders.

Example folder structure:
results/
    hanning_agent-1/
        model.onnx  (This is the model that will be used)
        HanningAgentAO/ (this folder contains all intermediary models and will be ignored)
    hanning_agent-2/
        model.onnx  (This is the model that will be used)
        HanningAgentAO/ (this folder contains all intermediary models and will be ignored)
    rect_agent-1/
        model.onnx  (This is the model that will be used)
        RectAgentAO/ (this folder contains all intermediary models and will be ignored)
"""

def find_onnx_files(results_dir):
    # Dictionary to store subfolder names and their corresponding .onnx files
    onnx_files = {}
    # Iterate through all subfolders under `results`
    for subfolder in os.listdir(results_dir):
        subfolder_path = os.path.join(results_dir, subfolder)
        # Check if it is a directory
        if os.path.isdir(subfolder_path):
            onnx_files[subfolder] = []
            # Check for .onnx files in the current subfolder
            for item in os.listdir(subfolder_path):
                item_path = os.path.join(subfolder_path, item)
                # Check if it is a file and has a .onnx extension
                if os.path.isfile(item_path) and item.endswith('.onnx'):
                    onnx_files[subfolder].append(item)
    return onnx_files


def evaluate_model(folder, file, decision_period, results_dir, executable_path, smoketest, dynamic):
    print(f"Evaluating {file} in {folder} with decision period {decision_period}...")
    # Figure out the full agent name from the inefficient naming convention
    name = folder
    # Split name by both "-" and "_"
    parts = re.split(r'[-_]', name)
    agent_name = parts[0]  # agents are named: hanning_53 or similar
    use_ao = "ao" == parts[1]  # ao agents are named: hanning_ao_35 or similar
    agent_name = f"{agent_name}ao" if use_ao else agent_name
    # Get full path to the model file
    relative_model_path = os.path.join(results_dir, folder, file)
    full_model_path = os.path.join(os.getcwd(), relative_model_path)
    # Create some unique name for the output csv
    output_name = f"{name}_d{decision_period}"
    if dynamic:
        target_speed = 5
        output_name += "_dynamic"
    else:
        target_speed = 0
        output_name += "_static"
    # Params for running the benchmark
    args = ["-benchmark"]
    if smoketest:
        args.append("-smoketest")
    args.append(f"-agent {agent_name}")
    args.append(f"-model {full_model_path}")
    args.append(f"-name {output_name}")
    args.append(f"-decisionPeriod {decision_period}")
    # Add dynamic target speed parameter

    args.append(f"-targetSpeed {target_speed}")

    args_str = " ".join(args)
    command = f"{executable_path} {args_str}"
    print(command)
    os.system(command)


def eval_all(onnx_files, results_dir, executable_path, max_workers, smoketest, dynamic):
    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = []
        use_dynamic_target = [True, False] if dynamic else [False]
        for folder, files in onnx_files.items():
            for file in files:
                for is_dynamic in use_dynamic_target:
                    decision_periods = [1, 10]  # During eval, both 1 and 10 give similar results
                    decision_periods = [1]
                    for decision_period in decision_periods:
                        futures.append(
                            executor.submit(evaluate_model, folder, file, decision_period, results_dir, executable_path,
                                            smoketest, dynamic=is_dynamic))
        # Optionally, wait for all futures to complete
        for future in futures:
            future.result()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description="Evaluate ONNX models.")
    parser.add_argument("--results_dir", required=False, default="results",
                        help="Directory containing the models (the results-folder of mlagents training)")
    parser.add_argument("--build_path", required=False,
                        default="./builds/aaaa/audio.x86_64",
                        help="Path to the executable")
    parser.add_argument("--max_workers", required=False, type=int, default=os.cpu_count(),
                        help="Maximum number of worker threads to use (Defaults to number of CPUs)")
    parser.add_argument("--smoketest", action='store_true', help="Run a very short benchmark for debugging")
    parser.add_argument("--dynamic", action='store_true',
                        help="Run benchmark with dynamic target speed (targetspeed 5)")

    args = parser.parse_args()
    results_dir = args.results_dir
    executable_path = args.build_path
    max_workers = args.max_workers
    smoketest = args.smoketest
    dynamic = args.dynamic

    onnx_files = find_onnx_files(results_dir)
    for folder, files in onnx_files.items():
        print(f"Subfolder: {folder}")
        for file in files:
            print(f"  .onnx file: {file}")
    eval_all(onnx_files, results_dir, executable_path, max_workers, smoketest, dynamic)
