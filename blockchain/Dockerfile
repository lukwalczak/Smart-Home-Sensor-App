FROM ghcr.io/foundry-rs/foundry:latest

WORKDIR /blockchain

RUN apk update && apk add --no-cache nodejs npm bash

COPY package.json ./

RUN npm install

COPY . .

RUN chmod +x /blockchain/scripts/setup.sh

CMD ["tail", "-f", "/dev/null"]
