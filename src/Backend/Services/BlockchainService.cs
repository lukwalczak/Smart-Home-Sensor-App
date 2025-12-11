using System.Numerics;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Backend.Blockchain;

namespace Backend.Services;

public class BlockchainService
{
    private readonly ILogger<BlockchainService> _logger;
    private readonly Web3 _web3;
    private readonly Account _adminAccount;
    private string _contractAddress;
    private Contract? _contract;
    private bool _isInitialized = false;

    public BlockchainService(IConfiguration config, ILogger<BlockchainService> logger)
    {
        _logger = logger;

        var rpcUrl = config["Blockchain:RpcUrl"] ?? "http://localhost:8545";
        var privateKey = config["Blockchain:AdminPrivateKey"]
            ?? "0xac0974bec39a17e36ba4a6b4d238ff944bacb478cbed5efcae784d7bf4f2ff80";
        var chainId = config.GetValue<int>("Blockchain:ChainId", 31337);

        _adminAccount = new Account(privateKey, chainId);
        _web3 = new Web3(_adminAccount, rpcUrl);

        _contractAddress = config["Blockchain:ContractAddress"] ?? string.Empty;

        if (!string.IsNullOrEmpty(_contractAddress))
        {
            _contract = _web3.Eth.GetContract(GetContractAbi(), _contractAddress);
            _isInitialized = true;
            _logger.LogInformation("BlockchainService initialized with contract at {Address}", _contractAddress);
        }
        else
        {
            _logger.LogWarning("Contract address not configured. Blockchain features will be unavailable.");
        }
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            var deploymentPath = "/blockchain/deployment.json";
            if (File.Exists(deploymentPath))
            {
                var json = await File.ReadAllTextAsync(deploymentPath);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var deployment = System.Text.Json.JsonSerializer.Deserialize<DeploymentInfo>(json, options);
                if (deployment != null && !string.IsNullOrEmpty(deployment.ContractAddress))
                {
                    _contractAddress = deployment.ContractAddress;
                    _contract = _web3.Eth.GetContract(GetContractAbi(), _contractAddress);
                    _isInitialized = true;
                    _logger.LogInformation("Contract loaded from deployment file: {Address}", _contractAddress);
                }
                else
                {
                    _logger.LogError("Deployment file loaded but ContractAddress is empty");
                }
            }
            else
            {
                _logger.LogError("Deployment file not found at {Path}", deploymentPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize blockchain service");
        }
    }

    public async Task<bool> RewardSensorAsync(string sensorId)
    {
        if (!_isInitialized)
        {
            _logger.LogWarning("Blockchain not initialized, skipping reward for {SensorId}", sensorId);
            return false;
        }

        try
        {
            var walletAddress = SensorWalletConfig.GetWalletAddress(sensorId);
            if (string.IsNullOrEmpty(walletAddress))
            {
                _logger.LogWarning("No wallet address configured for sensor {SensorId}", sensorId);
                return false;
            }

            var rewardFunction = _contract.GetFunction("rewardSensor");

            var gas = await rewardFunction.EstimateGasAsync(
                _adminAccount.Address,
                null,
                null,
                walletAddress,
                sensorId
            );

            var receipt = await rewardFunction.SendTransactionAndWaitForReceiptAsync(
                _adminAccount.Address,
                gas,
                new HexBigInteger(0),
                null,
                walletAddress,
                sensorId
            );

            _logger.LogInformation(
                "Rewarded sensor {SensorId} at address {Address}. TxHash: {TxHash}",
                sensorId,
                walletAddress,
                receipt.TransactionHash
            );

            return receipt.Status.Value == 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reward sensor {SensorId}", sensorId);
            return false;
        }
    }

    public async Task<SensorTokenInfo?> GetSensorTokenInfoAsync(string sensorId)
    {
        if (!_isInitialized)
            return null;

        try
        {
            var walletAddress = SensorWalletConfig.GetWalletAddress(sensorId);
            if (string.IsNullOrEmpty(walletAddress))
                return null;

            var statsFunction = _contract.GetFunction("getSensorStats");
            var stats = await statsFunction.CallDeserializingToObjectAsync<SensorStatsOutput>(walletAddress);

            return new SensorTokenInfo
            {
                SensorId = sensorId,
                WalletAddress = walletAddress,
                Balance = Web3.Convert.FromWei(stats.Balance),
                TotalRewards = Web3.Convert.FromWei(stats.TotalRewards),
                MessageCount = (long)stats.Messages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get token info for sensor {SensorId}", sensorId);
            return null;
        }
    }

    public async Task<List<SensorTokenInfo>> GetAllSensorTokenInfoAsync()
    {
        if (!_isInitialized)
            return new List<SensorTokenInfo>();

        var results = new List<SensorTokenInfo>();

        foreach (var (sensorId, _) in SensorWalletConfig.SensorWallets)
        {
            var info = await GetSensorTokenInfoAsync(sensorId);
            if (info != null)
                results.Add(info);
        }

        return results;
    }

    public async Task<ContractInfo?> GetContractInfoAsync()
    {
        if (!_isInitialized)
            return null;

        try
        {
            var totalSupplyFunction = _contract.GetFunction("totalSupply");
            var rewardPerMessageFunction = _contract.GetFunction("rewardPerMessage");
            var balanceFunction = _contract.GetFunction("balanceOf");

            var totalSupply = await totalSupplyFunction.CallAsync<BigInteger>();
            var rewardPerMessage = await rewardPerMessageFunction.CallAsync<BigInteger>();
            var adminBalance = await balanceFunction.CallAsync<BigInteger>(_adminAccount.Address);

            return new ContractInfo
            {
                ContractAddress = _contractAddress,
                AdminAddress = _adminAccount.Address,
                TotalSupply = Web3.Convert.FromWei(totalSupply),
                RewardPerMessage = Web3.Convert.FromWei(rewardPerMessage),
                AdminBalance = Web3.Convert.FromWei(adminBalance)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get contract info");
            return null;
        }
    }

    private string GetContractAbi()
    {
        return @"[
            {""inputs"":[{""internalType"":""uint256"",""name"":""initialSupply"",""type"":""uint256""},{""internalType"":""uint256"",""name"":""_rewardPerMessage"",""type"":""uint256""}],""stateMutability"":""nonpayable"",""type"":""constructor""},
            {""inputs"":[{""internalType"":""address"",""name"":""spender"",""type"":""address""},{""internalType"":""uint256"",""name"":""allowance"",""type"":""uint256""},{""internalType"":""uint256"",""name"":""needed"",""type"":""uint256""}],""name"":""ERC20InsufficientAllowance"",""type"":""error""},
            {""inputs"":[{""internalType"":""address"",""name"":""sender"",""type"":""address""},{""internalType"":""uint256"",""name"":""balance"",""type"":""uint256""},{""internalType"":""uint256"",""name"":""needed"",""type"":""uint256""}],""name"":""ERC20InsufficientBalance"",""type"":""error""},
            {""inputs"":[{""internalType"":""address"",""name"":""approver"",""type"":""address""}],""name"":""ERC20InvalidApprover"",""type"":""error""},
            {""inputs"":[{""internalType"":""address"",""name"":""receiver"",""type"":""address""}],""name"":""ERC20InvalidReceiver"",""type"":""error""},
            {""inputs"":[{""internalType"":""address"",""name"":""sender"",""type"":""address""}],""name"":""ERC20InvalidSender"",""type"":""error""},
            {""inputs"":[{""internalType"":""address"",""name"":""spender"",""type"":""address""}],""name"":""ERC20InvalidSpender"",""type"":""error""},
            {""inputs"":[{""internalType"":""address"",""name"":""owner"",""type"":""address""}],""name"":""OwnableInvalidOwner"",""type"":""error""},
            {""inputs"":[{""internalType"":""address"",""name"":""account"",""type"":""address""}],""name"":""OwnableUnauthorizedAccount"",""type"":""error""},
            {""anonymous"":false,""inputs"":[{""indexed"":true,""internalType"":""address"",""name"":""owner"",""type"":""address""},{""indexed"":true,""internalType"":""address"",""name"":""spender"",""type"":""address""},{""indexed"":false,""internalType"":""uint256"",""name"":""value"",""type"":""uint256""}],""name"":""Approval"",""type"":""event""},
            {""anonymous"":false,""inputs"":[{""indexed"":true,""internalType"":""address"",""name"":""previousOwner"",""type"":""address""},{""indexed"":true,""internalType"":""address"",""name"":""newOwner"",""type"":""address""}],""name"":""OwnershipTransferred"",""type"":""event""},
            {""anonymous"":false,""inputs"":[{""indexed"":false,""internalType"":""uint256"",""name"":""oldAmount"",""type"":""uint256""},{""indexed"":false,""internalType"":""uint256"",""name"":""newAmount"",""type"":""uint256""}],""name"":""RewardAmountUpdated"",""type"":""event""},
            {""anonymous"":false,""inputs"":[{""indexed"":true,""internalType"":""address"",""name"":""sensorAddress"",""type"":""address""},{""indexed"":false,""internalType"":""string"",""name"":""sensorId"",""type"":""string""},{""indexed"":false,""internalType"":""uint256"",""name"":""amount"",""type"":""uint256""},{""indexed"":false,""internalType"":""uint256"",""name"":""timestamp"",""type"":""uint256""}],""name"":""SensorRewarded"",""type"":""event""},
            {""anonymous"":false,""inputs"":[{""indexed"":true,""internalType"":""address"",""name"":""from"",""type"":""address""},{""indexed"":true,""internalType"":""address"",""name"":""to"",""type"":""address""},{""indexed"":false,""internalType"":""uint256"",""name"":""value"",""type"":""uint256""}],""name"":""Transfer"",""type"":""event""},
            {""inputs"":[{""internalType"":""address"",""name"":""owner"",""type"":""address""},{""internalType"":""address"",""name"":""spender"",""type"":""address""}],""name"":""allowance"",""outputs"":[{""internalType"":""uint256"",""name"":"""",""type"":""uint256""}],""stateMutability"":""view"",""type"":""function""},
            {""inputs"":[{""internalType"":""address"",""name"":""spender"",""type"":""address""},{""internalType"":""uint256"",""name"":""value"",""type"":""uint256""}],""name"":""approve"",""outputs"":[{""internalType"":""bool"",""name"":"""",""type"":""bool""}],""stateMutability"":""nonpayable"",""type"":""function""},
            {""inputs"":[{""internalType"":""address"",""name"":""account"",""type"":""address""}],""name"":""balanceOf"",""outputs"":[{""internalType"":""uint256"",""name"":"""",""type"":""uint256""}],""stateMutability"":""view"",""type"":""function""},
            {""inputs"":[],""name"":""decimals"",""outputs"":[{""internalType"":""uint8"",""name"":"""",""type"":""uint8""}],""stateMutability"":""view"",""type"":""function""},
            {""inputs"":[{""internalType"":""address"",""name"":""sensorAddress"",""type"":""address""}],""name"":""getSensorStats"",""outputs"":[{""internalType"":""uint256"",""name"":""balance"",""type"":""uint256""},{""internalType"":""uint256"",""name"":""totalRewards"",""type"":""uint256""},{""internalType"":""uint256"",""name"":""messages"",""type"":""uint256""}],""stateMutability"":""view"",""type"":""function""},
            {""inputs"":[{""internalType"":""address"",""name"":"""",""type"":""address""}],""name"":""messageCount"",""outputs"":[{""internalType"":""uint256"",""name"":"""",""type"":""uint256""}],""stateMutability"":""view"",""type"":""function""},
            {""inputs"":[{""internalType"":""uint256"",""name"":""amount"",""type"":""uint256""}],""name"":""mint"",""outputs"":[],""stateMutability"":""nonpayable"",""type"":""function""},
            {""inputs"":[],""name"":""name"",""outputs"":[{""internalType"":""string"",""name"":"""",""type"":""string""}],""stateMutability"":""view"",""type"":""function""},
            {""inputs"":[],""name"":""owner"",""outputs"":[{""internalType"":""address"",""name"":"""",""type"":""address""}],""stateMutability"":""view"",""type"":""function""},
            {""inputs"":[],""name"":""renounceOwnership"",""outputs"":[],""stateMutability"":""nonpayable"",""type"":""function""},
            {""inputs"":[{""internalType"":""address"",""name"":""sensorAddress"",""type"":""address""},{""internalType"":""string"",""name"":""sensorId"",""type"":""string""}],""name"":""rewardSensor"",""outputs"":[],""stateMutability"":""nonpayable"",""type"":""function""},
            {""inputs"":[],""name"":""rewardPerMessage"",""outputs"":[{""internalType"":""uint256"",""name"":"""",""type"":""uint256""}],""stateMutability"":""view"",""type"":""function""},
            {""inputs"":[],""name"":""symbol"",""outputs"":[{""internalType"":""string"",""name"":"""",""type"":""string""}],""stateMutability"":""view"",""type"":""function""},
            {""inputs"":[{""internalType"":""address"",""name"":"""",""type"":""address""}],""name"":""totalRewardsReceived"",""outputs"":[{""internalType"":""uint256"",""name"":"""",""type"":""uint256""}],""stateMutability"":""view"",""type"":""function""},
            {""inputs"":[],""name"":""totalSupply"",""outputs"":[{""internalType"":""uint256"",""name"":"""",""type"":""uint256""}],""stateMutability"":""view"",""type"":""function""},
            {""inputs"":[{""internalType"":""address"",""name"":""to"",""type"":""address""},{""internalType"":""uint256"",""name"":""value"",""type"":""uint256""}],""name"":""transfer"",""outputs"":[{""internalType"":""bool"",""name"":"""",""type"":""bool""}],""stateMutability"":""nonpayable"",""type"":""function""},
            {""inputs"":[{""internalType"":""address"",""name"":""from"",""type"":""address""},{""internalType"":""address"",""name"":""to"",""type"":""address""},{""internalType"":""uint256"",""name"":""value"",""type"":""uint256""}],""name"":""transferFrom"",""outputs"":[{""internalType"":""bool"",""name"":"""",""type"":""bool""}],""stateMutability"":""nonpayable"",""type"":""function""},
            {""inputs"":[{""internalType"":""address"",""name"":""newOwner"",""type"":""address""}],""name"":""transferOwnership"",""outputs"":[],""stateMutability"":""nonpayable"",""type"":""function""},
            {""inputs"":[{""internalType"":""uint256"",""name"":""newRewardAmount"",""type"":""uint256""}],""name"":""updateRewardAmount"",""outputs"":[],""stateMutability"":""nonpayable"",""type"":""function""}
        ]";
    }

    private class DeploymentInfo
    {
        public string ContractAddress { get; set; } = string.Empty;
    }
}

public class SensorTokenInfo
{
    public string SensorId { get; set; } = string.Empty;
    public string WalletAddress { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal TotalRewards { get; set; }
    public long MessageCount { get; set; }
}

public class ContractInfo
{
    public string ContractAddress { get; set; } = string.Empty;
    public string AdminAddress { get; set; } = string.Empty;
    public decimal TotalSupply { get; set; }
    public decimal RewardPerMessage { get; set; }
    public decimal AdminBalance { get; set; }
}
