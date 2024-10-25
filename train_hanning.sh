#!/bin/bash
# Configuration files
CONFIG_FILES=("AAAA-mlagents/config/hanning.yaml")
# Number of training iterations
NUM_ITERATIONS=5
# Loop to run the training process multiple times per configuration file
for i in $(seq 1 $NUM_ITERATIONS); do
  echo "Starting training run iteration $i..."
  # Loop over each configuration file
  for CONFIG_PATH in "${CONFIG_FILES[@]}"; do
    # Generate a unique run ID for each training run
    RUN_ID=$(basename "$CONFIG_PATH" .yaml)_${i}
    echo "Starting training run $i with run ID $RUN_ID for config $CONFIG_PATH..."
    # Run the training command
    mlagents-learn $CONFIG_PATH --run-id=$RUN_ID --num-envs=10
    # Check the exit status of the training command
    if [ $? -ne 0 ]; then
      echo "Training run $i with config $CONFIG_PATH failed!"
    else
      echo "Training run $i with config $CONFIG_PATH completed successfully."
    fi
  done
  echo "All configuration files for training run iteration $i completed."
done
echo "All training runs for all configurations completed."
