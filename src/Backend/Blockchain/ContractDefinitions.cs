using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Backend.Blockchain;

[Function("rewardSensor")]
public class RewardSensorFunction : FunctionMessage
{
    [Parameter("address", "sensorAddress", 1)]
    public string SensorAddress { get; set; } = string.Empty;

    [Parameter("string", "sensorId", 2)]
    public string SensorId { get; set; } = string.Empty;
}

[Function("getSensorStats", typeof(SensorStatsOutput))]
public class GetSensorStatsFunction : FunctionMessage
{
    [Parameter("address", "sensorAddress", 1)]
    public string SensorAddress { get; set; } = string.Empty;
}

[FunctionOutput]
public class SensorStatsOutput : IFunctionOutputDTO
{
    [Parameter("uint256", "balance", 1)]
    public BigInteger Balance { get; set; }

    [Parameter("uint256", "totalRewards", 2)]
    public BigInteger TotalRewards { get; set; }

    [Parameter("uint256", "messages", 3)]
    public BigInteger Messages { get; set; }
}

[Function("balanceOf", "uint256")]
public class BalanceOfFunction : FunctionMessage
{
    [Parameter("address", "_owner", 1)]
    public string Owner { get; set; } = string.Empty;
}

[Function("totalSupply", "uint256")]
public class TotalSupplyFunction : FunctionMessage
{
}

[Function("rewardPerMessage", "uint256")]
public class RewardPerMessageFunction : FunctionMessage
{
}

[Event("SensorRewarded")]
public class SensorRewardedEvent : IEventDTO
{
    [Parameter("address", "sensorAddress", 1, true)]
    public string SensorAddress { get; set; } = string.Empty;

    [Parameter("string", "sensorId", 2, false)]
    public string SensorId { get; set; } = string.Empty;

    [Parameter("uint256", "amount", 3, false)]
    public BigInteger Amount { get; set; }

    [Parameter("uint256", "timestamp", 4, false)]
    public BigInteger Timestamp { get; set; }
}
