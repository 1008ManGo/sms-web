#!/bin/bash
# SMPP Gateway 一键 Docker 部署脚本

set -e

echo "=========================================="
echo "  SMPP Gateway 一键部署脚本 (Docker)"
echo "=========================================="

# 检查 Docker
if ! command -v docker &> /dev/null; then
    echo "正在安装 Docker..."
    curl -fsSL https://get.docker.com | sh
    systemctl start docker
    systemctl enable docker
fi

# 检查 Docker Compose
if ! command -v docker-compose &> /dev/null; then
    echo "正在安装 Docker Compose..."
    curl -L "https://github.com/docker/compose/releases/download/v2.24.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    chmod +x /usr/local/bin/docker-compose
fi

# 创建目录
echo "创建部署目录..."
mkdir -p /opt/smpp-gateway
cd /opt/smpp-gateway

# 克隆代码
if [ ! -d ".git" ]; then
    echo "克隆项目代码..."
    git clone https://github.com/1008ManGo/sms-web.git .
else
    echo "更新项目代码..."
    git pull origin main
fi

# 创建 Dockerfile
cat > Dockerfile << 'EOF'
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SmppClient.sln .
COPY src/SmppClient/SmppClient.csproj src/SmppClient/
COPY src/SmppStorage/SmppStorage.csproj src/SmppStorage/
COPY src/SmppGateway/SmppGateway.csproj src/SmppGateway/

RUN dotnet restore
COPY . .
RUN dotnet publish src/SmppGateway/SmppGateway.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/healthz || exit 1

EXPOSE 8080
ENTRYPOINT ["dotnet", "SmppGateway.dll"]
EOF

# 创建 Docker Compose 配置
cat > docker-compose.yml << 'EOF'
version: '3.8'

services:
  smpp-gateway:
    build: .
    container_name: smpp-gateway
    ports:
      - "8080:8080"
    volumes:
      - ./src/SmppGateway/config.json:/app/config.json:ro
    depends_on:
      postgres:
        condition: service_healthy
    restart: unless-stopped
    networks:
      - smpp-network

  postgres:
    image: postgres:15-alpine
    container_name: smpp-postgres
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres123
      POSTGRES_DB: smpp_gateway
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5
    restart: unless-stopped
    networks:
      - smpp-network

  nginx:
    image: nginx:alpine
    container_name: smpp-nginx
    ports:
      - "80:80"
    volumes:
      - ./frontend/dist:/usr/share/nginx/html:ro
      - ./nginx.conf:/etc/nginx/conf.d/default.conf:ro
    depends_on:
      - smpp-gateway
    restart: unless-stopped
    networks:
      - smpp-network

volumes:
  postgres_data:

networks:
  smpp-network:
    driver: bridge
EOF

# 创建 Nginx 配置
cat > nginx.conf << 'EOF'
server {
    listen 80;
    server_name _;

    root /usr/share/nginx/html;
    index index.html;

    # 前端路由
    location / {
        try_files $uri $uri/ /index.html;
    }

    # API 代理
    location /api/ {
        proxy_pass http://smpp-gateway:8080;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }

    # Swagger
    location /swagger/ {
        proxy_pass http://smpp-gateway:8080;
    }

    # 健康检查
    location /healthz {
        proxy_pass http://smpp-gateway:8080;
    }
}
EOF

# 创建默认配置文件
cat > src/SmppGateway/config.json << 'EOF'
{
  "Host": "0.0.0.0",
  "Port": 8080,
  "AdminApiKey": "admin-a1b2c3d4e5f678901234567890123456",
  "Database": {
    "Host": "postgres",
    "Port": 5432,
    "Username": "postgres",
    "Password": "postgres123",
    "Database": "spp_gateway"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest"
  },
  "Accounts": [
    {
      "Id": "account-1",
      "Name": "默认通道",
      "Host": "127.0.0.1",
      "Port": 2775,
      "SystemId": "smppclient",
      "Password": "password",
      "SystemType": "SMPP",
      "Weight": 100,
      "Priority": 1,
      "MaxTps": 100,
      "MaxSessions": 2,
      "Enabled": true
    }
  ]
}
EOF

# 构建前端
echo "构建前端..."
cd /opt/smpp-gateway/frontend
npm install
npm run build

# 返回部署目录
cd /opt/smpp-gateway

# 启动服务
echo "启动 Docker 服务..."
docker-compose up -d --build

# 等待服务就绪
echo "等待服务就绪..."
sleep 10

# 检查状态
echo ""
echo "=========================================="
echo "  部署完成！"
echo "=========================================="
echo ""
docker-compose ps
echo ""
echo "访问地址："
echo "  前端界面: http://你的服务器IP"
echo "  API文档:  http://你的服务器IP/swagger/"
echo "  健康检查: http://你的服务器IP/healthz"
echo ""
echo "管理员密钥: admin-a1b2c3d4e5f678901234567890123456"
echo ""
