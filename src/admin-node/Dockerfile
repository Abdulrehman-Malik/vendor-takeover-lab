# Build context = repo root:
#   docker build -f src/admin-node/Dockerfile -t localhost:5000/admin-node:1 .
FROM node:24-alpine
WORKDIR /app
COPY src/admin-node/package.json src/admin-node/server.js ./
USER node
EXPOSE 3000
CMD ["node", "server.js"]
