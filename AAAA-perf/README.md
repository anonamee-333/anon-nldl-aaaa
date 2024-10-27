# Multi-client measurements

These scripts can be used to replicate the multi-listener benchmark as used in our paper.

This folder includes two scripts:
- ``measure.ipynb``
- ``plot.ipynb``

## Usage:

1. Install requirements with pip (virtualenv or conda recommended)
2. Change the settings in the ``measure.ipynb``
   - Path to the Unity build
   - Number of audio sources to measure with (by default, 32 might be the maximum usable amount limited by SteamAudio settings)
1. Run measurements using the ``measure.ipynb``
2. Plot results using the ``plot.ipynb``
