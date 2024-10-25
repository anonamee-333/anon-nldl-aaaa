import re
import os

from collections import namedtuple

def parse_filename(file_path):
    # Parses evaluation filenames to subcomponents
    # e.g., "Hanning_hanning_1_d1_Complex.csv" or "agent_model_decision-period_scene.csv"
    FilenameParts = namedtuple('FilenameParts', ['agent', 'model', 'model_id', 'scene', 'decision_period'])

    # Extract the filename from the path and split it to components
    filename = os.path.basename(file_path)
    splitted = filename.replace(".csv", "").split("_")



    # Get agent name (agent_model or agent_ao_model)
    agent = splitted[0]
    model_start_index = 1
    if splitted[1] == "ao":
        agent = f"{agent}_{splitted[1]}"
        model_start_index = 2

    # Get scene name (last word in the name)
    scene = splitted[-1]
    scene_start_index = -1
    if splitted[-2] == "static" or splitted[-2] == "dynamic":
        scene_start_index = -2
        scene = "_".join(splitted[scene_start_index:])

    # Get decision period (Before scene, starts with d)
    decision_period = splitted[scene_start_index-1].replace("d", "")
    decision_period = int(decision_period)

    # Get model name
    model_end_index = len(splitted) + (scene_start_index-1)  # Account for scene-name and decision-period
    model_name = "_".join(splitted[model_start_index:model_end_index])
    
    # Get model id
    model_parts = model_name.split("_")
    model_id = None
    if model_parts and model_parts[-1].isdigit():
        model_id = model_parts.pop(-1)
        model_name = "_".join(model_parts)

    return FilenameParts(agent=agent, model=model_name, model_id=model_id, scene=scene, decision_period=decision_period)


def parse_filename_regex(filename):
    # Regular expression to match the patterns
    pattern = r"(.+?)_(.+?)_(\d+|ao)_(d\d+)_([^.]+)\.csv"
    match = re.match(pattern, filename)

    if not match:
        print(filename)
        return "Filename format is incorrect"

    agent, _, id_part, decision_interval, scene = match.groups()

    if id_part == 'ao':
        agent = f"{agent}_{id_part}"
        id_part = match.group(3)

    # Clean up attributes
    decision_interval = decision_interval[1:]  # Remove 'd' from decision_interval
    scene = scene.lower()  # Change scene to lowercase

    parsed_result = f"agent={agent}, id={id_part}, decision_interval={decision_interval}, scene={scene}"
    return parsed_result

if __name__ == "__main__":
    # Example usage
    filenames = [
        "Hanning_hanning_80_d1_Medium.csv",
        "Rect_rect_43_d10_Complex.csv",
        "Hanning_hanning_ao_5_d1_Easy.csv",
        "ChHanning_channing_ao_36_d6_dididid.csv",
        "HanningAO_hanning-ao-no-los-dynamic_1_d1_static_Complex.csv",
        "HanningAO_hanning-ao-no-los-dynamic_10_d1_dynamic_Complex.csv"
    ]

    for fname in filenames:
        print(parse_filename(fname))
