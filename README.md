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


## TODO:

- Prepare repository for camera ready paper


## Usage instructions (training and testing models)

The project does not include any pretrained models. You can run the scenes, but the agent will simply navigate back-and-forth
along some nodes of the navigation grid as there is no input from the model.

Therefore, you need to train a model:

1. Open the unity project
2. Make a build with the training-scene
3. Train a model using the build and ML-Agents
4. Import the trained model to Unity
5. Open any of the benchmark scenes and place the model to the agent
   - By default, pressing play will run the benchmark for 100 episodes
7. Plot the resulting .csv file
