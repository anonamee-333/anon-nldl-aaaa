# Configuration files for ML-Agents


## Requirements

Use pip (conda or virtualenv recommended) to install requirements:

`pip install -r requirements.txt`

## Notes

To make sure that the audio is not distorted and the playback is always consistent,
consider using the following settings.

These are discussed in the documentation of the audio sensor by mbaske 
https://github.com/mbaske/ml-audio-sensor?tab=readme-ov-file#issues

```
engine_settings:
    time_scale: 1  # Increasing time scale will distort audio
    target_frame_rate: 50  # Can be higher, but should match capture framerate
    capture_frame_rate: 50  # Can be higher, but should match target framerate
```

## Usage

Run training with ML-Agents

`mlagents-learn ./config/hanning.yaml --run-id=hanning-1`
