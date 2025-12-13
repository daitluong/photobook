FROM node:18-alpine

WORKDIR /app

# Copy package files first
COPY package*.json ./

# Install dependencies (this creates node_modules)
RUN npm install

# Copy all source files
COPY . .

# Install global http-server
RUN npm install -g http-server

# Expose port
EXPOSE 4200

# Start server
CMD ["http-server", "-p", "4200", "-c-1", "."]
