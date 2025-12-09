#!/bin/sh

echo "Installing dependencies..."
cd /blockchain || exit 1

# Check if node_modules exists
if [ ! -d "node_modules" ]; then
    echo "Running npm install..."
    npm install || exit 1
else
    echo "Dependencies already installed"
fi

echo ""
echo "Waiting for Anvil to be ready..."
sleep 30

echo "Deploying contract to Anvil..."
node scripts/deploy.js

if [ $? -eq 0 ]; then
    echo ""
    echo "Setup complete!"
    echo "Contract deployed successfully!"
else
    echo ""
    echo "ERROR: Deployment failed!"
    exit 1
fi
