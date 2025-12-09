FROM ghcr.io/foundry-rs/foundry:latest

WORKDIR /blockchain

# Install Node.js and npm
# Foundry image is based on Alpine Linux
RUN apk update && apk add --no-cache nodejs npm bash

# Copy package files
COPY package.json ./

# Install dependencies
RUN npm install

# Copy all blockchain files
COPY . .

# Make scripts executable
RUN chmod +x /blockchain/scripts/setup.sh

# Keep container running
CMD ["tail", "-f", "/dev/null"]
