const { ethers } = require('ethers');
const fs = require('fs');
const path = require('path');
require('dotenv').config();

async function deploy() {
    const rpcUrl = process.env.ANVIL_RPC_URL || 'http://anvil:8545';
    console.log('🚀 Deploying SensorRewardToken contract to Anvil...');
    console.log('RPC URL:', rpcUrl);
    console.log('');

    const provider = new ethers.JsonRpcProvider(rpcUrl);
    const wallet = new ethers.Wallet(process.env.ADMIN_PRIVATE_KEY, provider);

    console.log('Admin address:', wallet.address);
    const balance = await provider.getBalance(wallet.address);
    console.log('Admin balance:', ethers.formatEther(balance), 'ETH\n');

    const contractPath = path.join(__dirname, '../contract-compiled.json');
    const contractJson = JSON.parse(fs.readFileSync(contractPath, 'utf8'));

    const factory = new ethers.ContractFactory(
        contractJson.abi,
        '0x' + contractJson.bytecode,
        wallet
    );

    const initialSupply = process.env.INITIAL_SUPPLY || '1000000';
    const rewardPerMessage = process.env.REWARD_PER_MESSAGE || '10';

    console.log(`Deploying with parameters:`);
    console.log(`- Initial Supply: ${initialSupply} SRT`);
    console.log(`- Reward per Message: ${rewardPerMessage} SRT\n`);

    const initialSupplyWei = ethers.parseUnits(initialSupply, 18);
    const rewardPerMessageWei = ethers.parseUnits(rewardPerMessage, 18);

    const contract = await factory.deploy(initialSupplyWei, rewardPerMessageWei);
    await contract.waitForDeployment();

    const contractAddress = await contract.getAddress();
    console.log('✅ Contract deployed at:', contractAddress);

    const deploymentInfo = {
        contractAddress,
        adminAddress: wallet.address,
        network: 'anvil',
        deployedAt: new Date().toISOString(),
        initialSupply,
        rewardPerMessage,
        abi: contractJson.abi
    };

    const deploymentPath = path.join(__dirname, '../deployment.json');
    fs.writeFileSync(deploymentPath, JSON.stringify(deploymentInfo, null, 2));
    console.log('📄 Deployment info saved to deployment.json\n');

    const totalSupply = await contract.totalSupply();
    const ownerBalance = await contract.balanceOf(wallet.address);
    console.log('Contract verification:');
    console.log('- Total Supply:', ethers.formatEther(totalSupply), 'SRT');
    console.log('- Owner Balance:', ethers.formatEther(ownerBalance), 'SRT');
    console.log('- Reward per Message:', ethers.formatEther(await contract.rewardPerMessage()), 'SRT');

    return deploymentInfo;
}

if (require.main === module) {
    deploy()
        .then(() => process.exit(0))
        .catch((error) => {
            console.error('❌ Deployment failed:', error);
            process.exit(1);
        });
}

module.exports = { deploy };
