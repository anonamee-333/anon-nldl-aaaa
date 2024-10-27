from gymnasium.spaces import Box, MultiDiscrete, Tuple as TupleSpace
import logging
import numpy as np
import random
import time
from typing import Callable, Optional, Tuple

from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from ray.rllib.env.multi_agent_env import MultiAgentEnv
from ray.rllib.policy.policy import PolicySpec
from ray.rllib.utils.annotations import PublicAPI
from ray.rllib.utils.typing import MultiAgentDict, PolicyID, AgentID, MultiEnvDict

logger = logging.getLogger(__name__)


@PublicAPI
class BetterUnity3DEnv(MultiAgentEnv):
    """A MultiAgentEnv representing a single Unity3D game instance.
    For an example on how to use this Env with a running Unity3D editor
    or with a compiled game, see:
    `rllib/examples/unity3d_env_local.py`
    For an example on how to use it inside a Unity game client, which
    connects to an RLlib Policy server, see:
    `rllib/examples/serving/unity3d_[client|server].py`
    Supports all Unity3D (MLAgents) examples, multi- or single-agent and
    gets converted automatically into an ExternalMultiAgentEnv, when used
    inside an RLlib PolicyClient for cloud/distributed training of Unity games.
    """

    # Default base port when connecting directly to the Editor
    _BASE_PORT_EDITOR = 5004
    # Default base port when connecting to a compiled environment
    _BASE_PORT_ENVIRONMENT = 49905
    # The worker_id for each environment instance
    _WORKER_ID = 0

    def __init__(
            self,
            file_name: str = None,
            port: Optional[int] = None,
            seed: int = 0,
            no_graphics: bool = False,
            timeout_wait: int = 30,
            episode_horizon: int = 1000,
            soft_horizon: bool = True,
            timescale: int = 1,
            observation_high: float = 1,  # Should be 1, but some unity examples give too high values crashing RLlib
            args: Optional[list] = None,
            log_folder: Optional[str] = None,
    ):
        """Initializes a Unity3DEnv object.
        Args:
            file_name (Optional[str]): Name of the Unity game binary.
                If None, will assume a locally running Unity3D editor
                to be used, instead.
            port (Optional[int]): Port number to connect to Unity environment.
            seed: A random seed value to use for the Unity3D game.
            no_graphics: Whether to run the Unity3D simulator in
                no-graphics mode. Default: False.
            timeout_wait: Time (in seconds) to wait for connection from
                the Unity3D instance.
            episode_horizon: A hard horizon to abide to. After at most
                this many steps (per-agent episode `step()` calls), the
                Unity3D game is reset and will start again (finishing the
                multi-agent episode that the game represents).
                Note: The game itself may contain its own episode length
                limits, which are always obeyed (on top of this value here).
        """

        super().__init__()

        if file_name is None:
            print(
                "No game binary provided, will use a running Unity editor "
                "instead.\nMake sure you are pressing the Play (|>) button in "
                "your editor to start."
            )

        import mlagents_envs
        from mlagents_envs.environment import UnityEnvironment

        # Try connecting to the Unity3D game instance. If a port is blocked
        port_ = None
        while True:
            # Sleep for random time to allow for concurrent startup of many
            # environments (num_workers >> 1). Otherwise, would lead to port
            # conflicts sometimes.
            # time.sleep(random.random() * 0.5)
            port_ = port or (
                self._BASE_PORT_ENVIRONMENT if file_name else self._BASE_PORT_EDITOR
            )
            # cache the worker_id and
            # increase it for the next environment
            # worker_id_ = BetterUnity3DEnv._WORKER_ID if file_name else 0
            worker_id_ = random.randint(0, 9999)  # Use random port and retry if conflict happens
            BetterUnity3DEnv._WORKER_ID += 1
            seed = worker_id_  # TODO: Seed is hardcoded here, argument for parent function is ignored!
            print(f"Seed: {seed}")
            try:
                channel = EngineConfigurationChannel()
                self.unity_env = UnityEnvironment(
                    file_name=file_name,
                    worker_id=worker_id_,
                    base_port=port_,
                    seed=seed,
                    no_graphics=no_graphics,
                    timeout_wait=timeout_wait,
                    side_channels=[channel],
                    additional_args=args,
                    log_folder=log_folder,
                )
                channel.set_configuration_parameters(time_scale=timescale)
                print("Created UnityEnvironment for port {}".format(port_ + worker_id_))
            except mlagents_envs.exception.UnityWorkerInUseException:
                pass
            else:
                break

        # ML-Agents API version.
        self.api_version = self.unity_env.API_VERSION.split(".")
        self.api_version = [int(s) for s in self.api_version]

        # Reset entire env every this number of step calls.
        self.episode_horizon = episode_horizon
        self.soft_horizon = soft_horizon
        # Keep track of how many times we have called `step` so far.
        self.episode_timesteps = 0

        # First step is always empty, so lets run it already in here to not mess up Ray Rllib
        obs, rewards, terminated, truncated, info = self.step({})
        self._agent_ids = list(obs.keys())
        # self._agent_ids = list(self.unity_env.behavior_specs.keys())
        self.action_spaces = {}
        self.observation_spaces = {}
        for agent in self._agent_ids:
            # converted_name = agent.replace("_0", "")
            # For multiagent cases "_id" is appended to all agent names.
            # Remove the extra id, as it is not included in the behavior names:
            postfix = "team=0"
            converted_name = agent.split(postfix)  # Could also use regex here...
            converted_name = converted_name[0] + postfix
            behavior_spec = self.unity_env.behavior_specs[converted_name]
            action_spec = behavior_spec.action_spec
            observation_spec = behavior_spec.observation_specs
            # self.action_space[agent] =
            # high = np.inf  # TODO: Would be nice to have 1, but some unity examples have -inf to inf
            high = observation_high
            if action_spec.continuous_size > 0:
                size = action_spec.continuous_size
                self.action_spaces[agent] = Box(
                    -high,
                    high,
                    shape=(size,),
                    dtype=np.float32,
                )
            if len(observation_spec) == 1:
                shape = observation_spec[0].shape
                self.observation_spaces[agent] = Box(-high, high, shape=shape, dtype=np.float32)
            else:
                boxes = []
                for obs in observation_spec:
                    box = Box(-high, high, shape=obs.shape, dtype=np.float32)
                    boxes.append(box)
                self.observation_spaces[agent] = TupleSpace(boxes)
            # TODO: How to do this with multiple policies? Ray claims to like dicts, but does not like them in reality
            # self.action_spaces["default_policy"] = self.action_spaces[agent]
            # self.observation_spaces["default_policy"] = self.observation_spaces[agent]
            self.action_space = self.action_spaces[agent]
            self.observation_space = self.observation_spaces[agent]

        print("x")
        # self._action_space_in_preferred_format = False
        # self._obs_space_in_preferred_format = False

    def observation_space_sample(self, agent_ids: list = None) -> MultiEnvDict:
        samples = {}
        if agent_ids == None:
            default = self._agent_ids[0]
            return {default: self.observation_spaces[default].sample()}
        for agent in agent_ids:
            samples[agent] = self.observation_spaces[agent].sample()
        return samples

    def action_space_sample(self, agent_ids: list = None) -> MultiAgentDict:
        samples = {}
        if agent_ids == None:
            default = self._agent_ids[0]
            return {default: self.action_spaces[default].sample()}
        for agent in agent_ids:
            samples[agent] = self.action_spaces[agent].sample()
        return samples

    def observation_space_contains(self, x: MultiAgentDict) -> bool:
        for agent, value in x.items():
            space = self.observation_spaces[agent]
            if not space.contains(value):
                return False
        return True

    def action_space_contains(self, x: MultiAgentDict) -> bool:
        for agent, value in x.items():
            space = self.action_spaces[agent]
            if not space.contains(value):
                return False
        return True

    def step(
            self, action_dict: MultiAgentDict
    ) -> Tuple[
        MultiAgentDict, MultiAgentDict, MultiAgentDict, MultiAgentDict, MultiAgentDict
    ]:
        """Performs one multi-agent step through the game.
        Args:
            action_dict: Multi-agent action dict with:
                keys=agent identifier consisting of
                [MLagents behavior name, e.g. "Goalie?team=1"] + "_" +
                [Agent index, a unique MLAgent-assigned index per single agent]
        Returns:
            tuple:
                - obs: Multi-agent observation dict.
                    Only those observations for which to get new actions are
                    returned.
                - rewards: Rewards dict matching `obs`.
                - dones: Done dict with only an __all__ multi-agent entry in
                    it. __all__=True, if episode is done for all agents.
                - infos: An (empty) info dict.
        """
        from mlagents_envs.base_env import ActionTuple

        # Set only the required actions (from the DecisionSteps) in Unity3D.
        all_agents = []
        for behavior_name in self.unity_env.behavior_specs:
            # New ML-Agents API: Set all agents actions at the same time
            # via an ActionTuple. Since API v1.4.0.
            if self.api_version[0] > 1 or (
                    self.api_version[0] == 1 and self.api_version[1] >= 4
            ):
                actions = []
                for agent_id in self.unity_env.get_steps(behavior_name)[0].agent_id:
                    key = behavior_name + "_{}".format(agent_id)
                    all_agents.append(key)
                    # print(key)
                    # print(action_dict)
                    if key not in action_dict:
                        # print("nokey")
                        pass
                    else:
                        actions.append(action_dict[key])
                if actions:
                    if actions[0].dtype == np.float32:
                        action_tuple = ActionTuple(continuous=np.array(actions))
                    else:
                        action_tuple = ActionTuple(discrete=np.array(actions))
                    self.unity_env.set_actions(behavior_name, action_tuple)
            # Old behavior: Do not use an ActionTuple and set each agent's
            # action individually.
            else:
                for agent_id in self.unity_env.get_steps(behavior_name)[
                    0
                ].agent_id_to_index.keys():
                    key = behavior_name + "_{}".format(agent_id)
                    all_agents.append(key)
                    self.unity_env.set_action_for_agent(
                        behavior_name, agent_id, action_dict[key]
                    )
        # Do the step.
        self.unity_env.step()

        obs, rewards, terminateds, truncateds, infos = self._get_step_results()

        # Global horizon reached? -> Return __all__ truncated=True, so user
        # can reset. Set all agents' individual `truncated` to True as well.
        self.episode_timesteps += 1
        if self.episode_timesteps >= self.episode_horizon:
            return (
                obs,
                rewards,
                terminateds,
                dict({"__all__": True}, **{agent_id: True for agent_id in all_agents}),
                infos,
            )

        return obs, rewards, terminateds, truncateds, infos

    def reset(
            self, *, seed=None, options=None
    ) -> Tuple[MultiAgentDict, MultiAgentDict]:
        """Resets the entire Unity3D scene (a single multi-agent episode)."""
        self.episode_timesteps = 0
        if not self.soft_horizon:
            self.unity_env.reset()
        obs, _, _, _, infos = self._get_step_results()
        # print(obs)
        return obs, infos

    def _get_step_results(self):
        """Collects those agents' obs/rewards that have to act in next `step`.
        Returns:
            Tuple:
                obs: Multi-agent observation dict.
                    Only those observations for which to get new actions are
                    returned.
                rewards: Rewards dict matching `obs`.
                dones: Done dict with only an __all__ multi-agent entry in it.
                    __all__=True, if episode is done for all agents.
                infos: An (empty) info dict.
        """
        obs = {}
        rewards = {}
        terminateds = {"__all__": False}
        infos = {}
        i = 0
        for behavior_name in self.unity_env.behavior_specs:
            decision_steps, terminal_steps = self.unity_env.get_steps(behavior_name)
            # Important: Only update those sub-envs that are currently
            # available within _env_state.
            # Loop through all envs ("agents") and fill in, whatever
            # information we have.
            # print(decision_steps.agent_id_to_index.items())
            for agent_id, idx in decision_steps.agent_id_to_index.items():
                key = behavior_name + "_{}".format(agent_id)
                terminateds[key] = False
                os = tuple(o[idx] for o in decision_steps.obs)
                os = os[0] if len(os) == 1 else os
                obs[key] = os
                rewards[key] = (
                        decision_steps.reward[idx] + decision_steps.group_reward[idx]
                )
                # print(f"{key}, {rewards[key]}, {decision_steps.group_reward[idx]}")
                # print(i)
                i += 1
            for agent_id, idx in terminal_steps.agent_id_to_index.items():
                key = behavior_name + "_{}".format(agent_id)
                terminateds[key] = True
                # Only overwrite rewards (last reward in episode), b/c obs
                # here is the last obs (which doesn't matter anyways).
                # Unless key does not exist in obs.
                if key not in obs:
                    os = tuple(o[idx] for o in terminal_steps.obs)
                    obs[key] = os = os[0] if len(os) == 1 else os
                rewards[key] = (
                        terminal_steps.reward[idx] + terminal_steps.group_reward[idx]
                )


        # Only use dones if all agents are done, then we should do a reset.
        if False not in terminateds.values():
            terminateds["__all__"] = True
            # TODO: How to report that only one agent is done? RLlib seems to crash in this simple situation
        return obs, rewards, {"__all__": False}, {"__all__": False}, infos
        # return obs, rewards, terminateds, {"__all__": False}, infos

    def close(self):
        print("Closing unity env")
        self.unity_env.close()
