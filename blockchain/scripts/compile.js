const solc = require('solc');
const fs = require('fs');
const path = require('path');

function findImports(importPath) {
    if (importPath.startsWith('@openzeppelin/')) {
        const contractPath = path.join(__dirname, '../node_modules', importPath);
        try {
            return { contents: fs.readFileSync(contractPath, 'utf8') };
        } catch (e) {
            return { error: 'File not found: ' + importPath };
        }
    }
    return { error: 'File not found' };
}

console.log("📦 Compiling SensorRewardToken.sol...");

const contractPath = path.join(__dirname, '../contracts/SensorRewardToken.sol');
const source = fs.readFileSync(contractPath, 'utf8');

const input = {
    language: 'Solidity',
    sources: {
        'SensorRewardToken.sol': {
            content: source
        }
    },
    settings: {
        optimizer: {
            enabled: true,
            runs: 200
        },
        outputSelection: {
            '*': {
                '*': ['abi', 'evm.bytecode']
            }
        }
    }
};

const output = JSON.parse(solc.compile(JSON.stringify(input), { import: findImports }));

if (output.errors) {
    const errors = output.errors.filter(e => e.severity === 'error');
    if (errors.length > 0) {
        console.error("❌ Compilation errors:");
        errors.forEach(err => console.error(err.formattedMessage));
        process.exit(1);
    }
}

const contract = output.contracts['SensorRewardToken.sol']['SensorRewardToken'];

const compiled = {
    abi: contract.abi,
    bytecode: contract.evm.bytecode.object
};

const outputPath = path.join(__dirname, '../contract-compiled.json');
fs.writeFileSync(outputPath, JSON.stringify(compiled, null, 2));

console.log("✅ Contract compiled successfully!");
console.log("📄 Bytecode size:", contract.evm.bytecode.object.length / 2, "bytes");
console.log("📄 Output saved to contract-compiled.json");
