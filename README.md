# Audio-aware agents

This repository contains the scenes and scripts we created for experiments with DRL-based audio-aware agents, 
along with some publicly available external plugins from others.

Structure:
1. AAAA-unity (Unity project)
   - Assets
      - Includes the three scenes as seen in the paper ("simple", "medium", "complex")
   - Scripts
      - AudioAgent related scripts (consists of multiple scripts)
      - Target related scripts (Target of the audio agent, can move around randomly)
      - Benchmark related scripts (Measures agent performance over multiple episodes)
      - Navgrid related scripts (Agents use this for moving around)
      - Multiple other scripts supporting this and other experiments
   - External assets
      - ML-Agents audiosensor by mbaske (https://github.com/mbaske/ml-audio-sensor)
      - Steam Audio (https://valvesoftware.github.io/steam-audio/)
      - Footstep audio samples by Nox_Sound (https://assetstore.unity.com/packages/audio/sound-fx/foley/footsteps-essentials-189879)
      - ML-Agents (https://github.com/Unity-Technologies/ml-agents)
1. AAAA-mlagents
   - ML-Agents configuration files
1. AAAA-perf
   - Scripts for plotting and measuring multi-listener performance
1. builds
   - Includes a build that was used to train and evaluate the agents in our paper 
   - You can build your own by launching the Unity project and creating a "DedicatedServer" build with default settings
     - The non-server build might not support commandline arguments that are necessary for running the experiments.
1. plotting
   - Scripts for plotting the results from benchmarking trained models.

## Installing requirements

Folders ``AAAA-mlagents``, ``AAAA-perf`` and ``plotting`` each have their own `requirements.txt`.

It is recommended to create a separate python environment (using conda or virtualenv) for each of them, 
as they may have conflicting dependencies.

Example:

1. ``cd AAAA-mlagents``
2. ``conda env create -f conda.yaml``
3. ``conda activate aaaa-mlagents``

or

1. ``virtualenv -p python3.9 venv``
2. ``source venv/bin/activate``
3. ``pip install -r requirements.txt``



## Usage instructions (training and testing models)

The project does not include any pretrained models. You can run the scenes, but the agent will simply navigate back-and-forth
along some nodes of the navigation grid as there is no input from the model.

Training and evaluation:

1. If using Windows, change the `env_path` to point to the Windows-build
   - Do this either in the `AAAA-mlagents/config/*.yaml` files or add `--env ENV_PATH` override to the `mlagents-learn` command.
1. Train the agents with default parameters (by default, will output models to `results/`)
   - `mlagents-learn AAAA-mlagents/config/hanning.yaml --num-envs 10 --run-id=hanning_1`
   - `mlagents-learn AAAA-mlagents/config/rect.yaml --num-envs 10 --run-id=rect_1`
   - Or run scripts ``train_rect.sh`` and ``train_hanning.sh``
2. Run evaluation (by default, will output csv:s to `logs/`)
   - `python auto_eval.py --help`
   - `python auto_eval.py --results_dir results --build_path path/to/build`
   - or manually ``./build.x86_64 -agent hanningAO -model models/hanning-1.onnx -benchmark -name hanning_1 -decisionPeriod 1``
5. Plot the results using the scripts in ``plotting/``
   -  If you used the ``auto_eval.py``, these scripts should work with minimal modification.
   -  If you are not using the ``auto_eval.py``, you may need to modify the plotting scripts.
      The plotting scripts expect the .csv-files to follow a very specific naming convention. 
      

## Usage instructions (multi-listener benchmark)

Follow the instructions in the ``AAAA-perf/readme.md``.

## Args (for running your own experiments)

The Unity build supports the following arguments:

- `--help` (prints all commands)
- `-agent` (Choose from one of the following options: `hanningAO`, `rectAO`, `random`)
- `-benchmark` (Enable benchmark mode. Should not be used while training.)
- `-model` (Path to model, if loading an external ONNX model for benchmark)
- `-name` (Prefix for the output CSV name for the benchmark results)
- `-smoketest` (Changes benchmark settings to a very short benchmark)
- `-decisionPeriod` (Integer, usually 1 or 10. Should be a fraction of the audio buffer length)
- `-losReward` (Float value to set the line-of-sight reward scale [recommended value: 0])
- `-targetSpeed` (Float value to set the speed of the target. Use 0 to make the target static. Otherwise it will randomly navigate in the environment.)
- `-audioSources` (Integer, used to multiply the amount of audio sources in the environment for testing performance)

**Examples:**

- Benchmark:
  ```shell
  ./build.x86_64 -agent hanningAO -model models/hanning-1.onnx -benchmark -name hanning_1 -decisionPeriod 1 -targetSpeed 0
  ```

- Training (Used in combination with mlagents-learn (by setting these env args in the mlagents configuration). Does not do much on its own.):
  ```shell
  ./build.x86_64 -agent hanningAO -losReward 0 -decisionPeriod 1
  ```

- Smoketest (A very short benchmark for debugging):
  ```shell
  ./build.x86_64 -agent hanningAO -benchmark -smoketest
  ```

### Agents

The build includes five agents (Switch with the `-agent` argument):

- Hanning (Audio agent with Hanning windowing function, but includes position sensor and raycast sensors)
- HanningAO (Audio agent with Hanning windowing function. AO means AudioOnly. It does not have any other sensors besides the audio-sensor.)
- Rect (Audio agent with rectangular windowing function, but includes position sensor and raycast sensors)
- RectAO (Audio agent with rectangular windowing function. AO means AudioOnly. It does not have any other sensors besides the audio-sensor.)
- Random  (Navigates to random coordinates in the scene using Unity NavMesh. Technically uses a NN-model similarly to other agents, but the actions from the model are ignored.)

### LoSReward

The reward signal includes an optional component for line-of-sight. Here the agent is rewarded if it correctly
guesses whether it has line-of-sight to the target or not. The third element of the action vector is used for this.
The motivation is to train the agent to switch between using the NavGrid and directly moving along the action vector.

`-losreward 0` disables the line-of-sight reward by scaling it with 0. This is recommended, as the switching between NavGrid and direct movement is not implemented.

### DecisionPeriod

As the audio buffer is (by default) configured as 10 steps long (0.2 seconds), it is possible to save performance by only making a decision
every 10 steps. Making decisions on every step is possible, but the agent will not travel far in one step and
there is little new information gained compared to the previous step. Training might be faster with 1 step decision period.

`-decisionPeriod 1` for training

`-decisionPeriod 10` for runtime



