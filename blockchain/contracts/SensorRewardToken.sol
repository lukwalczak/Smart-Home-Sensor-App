// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "@openzeppelin/contracts/token/ERC20/ERC20.sol";
import "@openzeppelin/contracts/access/Ownable.sol";

/**
 * @title SensorRewardToken
 * @dev ERC-20 token that rewards IoT sensors for sending messages
 */
contract SensorRewardToken is ERC20, Ownable {
    // Reward amount per message (in wei, 18 decimals)
    uint256 public rewardPerMessage;
    
    // Mapping to track total rewards for each sensor
    mapping(address => uint256) public totalRewardsReceived;
    
    // Mapping to track message count for each sensor
    mapping(address => uint256) public messageCount;
    
    // Event emitted when a sensor is rewarded
    event SensorRewarded(
        address indexed sensorAddress,
        string sensorId,
        uint256 amount,
        uint256 timestamp
    );
    
    // Event emitted when reward amount is updated
    event RewardAmountUpdated(uint256 oldAmount, uint256 newAmount);
    
    /**
     * @dev Constructor to initialize the token
     * @param initialSupply Initial token supply (in wei, 18 decimals)
     * @param _rewardPerMessage Reward amount per message (in wei, 18 decimals)
     */
    constructor(
        uint256 initialSupply,
        uint256 _rewardPerMessage
    ) ERC20("SensorRewardToken", "SRT") Ownable(msg.sender) {
        _mint(msg.sender, initialSupply);
        rewardPerMessage = _rewardPerMessage;
    }
    
    /**
     * @dev Reward a sensor for sending a message
     * @param sensorAddress The wallet address of the sensor
     * @param sensorId The sensor identifier (for logging)
     */
    function rewardSensor(address sensorAddress, string memory sensorId) external onlyOwner {
        require(sensorAddress != address(0), "Invalid sensor address");
        require(balanceOf(owner()) >= rewardPerMessage, "Insufficient tokens in contract");
        
        // Transfer tokens from owner to sensor
        _transfer(owner(), sensorAddress, rewardPerMessage);
        
        // Update statistics
        totalRewardsReceived[sensorAddress] += rewardPerMessage;
        messageCount[sensorAddress] += 1;
        
        emit SensorRewarded(sensorAddress, sensorId, rewardPerMessage, block.timestamp);
    }
    
    /**
     * @dev Update the reward amount per message
     * @param newRewardAmount New reward amount (in wei, 18 decimals)
     */
    function updateRewardAmount(uint256 newRewardAmount) external onlyOwner {
        uint256 oldAmount = rewardPerMessage;
        rewardPerMessage = newRewardAmount;
        emit RewardAmountUpdated(oldAmount, rewardPerMessage);
    }
    
    /**
     * @dev Get sensor statistics
     * @param sensorAddress The wallet address of the sensor
     * @return balance Current token balance
     * @return totalRewards Total rewards received
     * @return messages Total messages sent
     */
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
    
    /**
     * @dev Mint additional tokens (only owner)
     * @param amount Amount to mint (in wei, 18 decimals)
     */
    function mint(uint256 amount) external onlyOwner {
        _mint(owner(), amount);
    }
}
