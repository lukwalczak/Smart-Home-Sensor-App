// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "@openzeppelin/contracts/token/ERC20/ERC20.sol";
import "@openzeppelin/contracts/access/Ownable.sol";

contract SensorRewardToken is ERC20, Ownable {
    uint256 public rewardPerMessage;

    mapping(address => uint256) public totalRewardsReceived;

    mapping(address => uint256) public messageCount;

    event SensorRewarded(
        address indexed sensorAddress,
        string sensorId,
        uint256 amount,
        uint256 timestamp
    );

    event RewardAmountUpdated(uint256 oldAmount, uint256 newAmount);

    constructor(
        uint256 initialSupply,
        uint256 _rewardPerMessage
    ) ERC20("SensorRewardToken", "SRT") Ownable(msg.sender) {
        _mint(msg.sender, initialSupply);
        rewardPerMessage = _rewardPerMessage;
    }

    function rewardSensor(address sensorAddress, string memory sensorId) external onlyOwner {
        require(sensorAddress != address(0), "Invalid sensor address");
        require(balanceOf(owner()) >= rewardPerMessage, "Insufficient tokens in contract");

        _transfer(owner(), sensorAddress, rewardPerMessage);

        totalRewardsReceived[sensorAddress] += rewardPerMessage;
        messageCount[sensorAddress] += 1;

        emit SensorRewarded(sensorAddress, sensorId, rewardPerMessage, block.timestamp);
    }

    function updateRewardAmount(uint256 newRewardAmount) external onlyOwner {
        uint256 oldAmount = rewardPerMessage;
        rewardPerMessage = newRewardAmount;
        emit RewardAmountUpdated(oldAmount, rewardPerMessage);
    }

    function getSensorStats(address sensorAddress) external view returns (
        uint256 balance,
        uint256 totalRewards,
        uint256 messages
    ) {
        return (
            balanceOf(sensorAddress),
            totalRewardsReceived[sensorAddress],
            messageCount[sensorAddress]
        );
    }

    function mint(uint256 amount) external onlyOwner {
        _mint(owner(), amount);
    }
}
