# SMPP Gateway 部署说明

## 目录

1. [系统架构](#系统架构)
2. [环境要求](#环境要求)
3. [后端部署](#后端部署)
4. [前端部署](#前端部署)
5. [配置说明](#配置说明)
6. [Docker 部署](#docker-部署)
7. [验证部署](#验证部署)

---

## 系统架构

```
┌─────────────────────────────────────────────────────────────────┐
│                         用户访问层                                │
│                    Browser (Web UI)                              │
└─────────────────────────┬───────────────────────────────────────┘
                          │ HTTP/HTTPS
┌─────────────────────────▼───────────────────────────────────────┐
│                       前端服务                                    │
│              Vue 3 + Element Plus + Vite                          │
│                   端口: 3000 (开发) / 80 (生产)                    │
└─────────────────────────┬───────────────────────────────────────┘
                          │ /api/*
┌─────────────────────────▼───────────────────────────────────────┐
│                     后端服务 (.NET 8)                            │
│                       SmppGateway                                  │
│                      端口: 8080                                    │
│  ┌──────────────────┬──────────────────┬──────────────────────┐  │
│  │   Controllers    │    Services     │    Observability    │  │
│  │  - AdminController│  - SmppClient   │  - Prometheus       │  │
│  │  - UserController │  - Billing      │  - Health Checks    │  │
│  │  - SmsController  │  - Permission   │                     │  │
│  │  - HealthController│  - Alert       │                     │  │
│  └──────────────────┴──────────────────┴──────────────────────┘  │
└────────┬──────────────────────┬──────────────────────────────-───┘
         │                      │
┌────────▼────────┐    ┌────────▼────────┐    ┌──────────────────┐
│   SmppClient    │    │   SmppStorage    │    │    RabbitMQ      │
│   SMPP 协议      │    │   PostgreSQL     │    │    (消息队列)      │
│   端口: 2775     │    │   端口: 5432     │    │    端口: 5672     │
└─────────────────┘    └──────────────────┘    └──────────────────┘
```

## 环境要求

### 后端环境
- **.NET SDK**: 8.0 或更高
- **PostgreSQL**: 14.x 或更高
- **RabbitMQ**: 3.x (可选，用于异步消息处理)
- **操作系统**: Linux / Windows / macOS

### 前端环境
- **Node.js**: 18.x 或更高
- **pnpm**: 8.x (推荐) 或 npm 9.x

### 基础设施
- **Redis**: 可选 (用于缓存)
- **SMPP SMSC**: 第三方短信中心服务器

---

## 后端部署

### 1. 编译项目

```bash
# 克隆代码
cd /workspace

# 还原依赖
dotnet restore SmppClient.sln

# 编译 (Debug)
dotnet build SmppClient.sln -c Debug

# 编译 (Release)
dotnet build SmppClient.sln -c Release
```

### 2. 配置数据库

```bash
# 登录 PostgreSQL
psql -U postgres

# 创建数据库
CREATE DATABASE smpp_gateway;
CREATE USER smpp_user WITH ENCRYPTED PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE smpp_gateway TO smpp_user;
\q

# 修改 config.json 中的数据库连接
```

### 3. 修改配置文件

编辑 `src/SmppGateway/config.json`:

```json
{
  "Host": "0.0.0.0",
  "Port": 8080,
  "AdminApiKey": "your-secure-admin-key",
  "Database": {
    "Host": "localhost",
    "Port": 5432,
    "Username": "smpp_user",
    "Password": "your_password",
    "Database": "smpp_gateway"
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
      "Name": "运营商1",
      "Host": "smsc.example.com",
      "Port": 2775,
      "SystemId": "your_system_id",
      "Password": "your_password",
      "SystemType": "SMPP",
      "Weight": 100,
      "Priority": 1,
      "MaxTps": 100,
      "MaxSessions": 2,
      "Enabled": true
    }
  ]
}
```

### 4. 运行服务

```bash
# 发布后运行
dotnet run --project src/SmppGateway/SmppGateway.csproj -c Release

# 或使用已编译的输出
dotnet src/SmppGateway/bin/Release/net8.0/SmppGateway.dll
```

### 5. 守护进程 (Systemd)

创建服务文件 `/etc/systemd/system/smpp-gateway.service`:

```ini
[Unit]
Description=SMPP Gateway Service
After=network.target postgresql.service rabbitmq.service

[Service]
Type=simple
User=www-data
WorkingDirectory=/opt/smpp-gateway
ExecStart=/usr/bin/dotnet /opt/smpp-gateway/SmppGateway.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable smpp-gateway
sudo systemctl start smpp-gateway
sudo systemctl status smpp-gateway
```

---

## 前端部署

### 1. 安装依赖

```bash
cd frontend
pnpm install
```

### 2. 配置 API 地址

在开发环境，Vite 代理配置已将 `/api` 请求转发到后端。

如需修改，编辑 `vite.config.ts`:

```typescript
export default defineConfig({
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:8080',
        changeOrigin: true
      }
    },
    allowedHosts: ['.monkeycode-ai.online']
  }
})
```

### 3. 编译生产版本

```bash
pnpm build
```

构建产物在 `frontend/dist` 目录。

### 4. 部署到 Nginx

```nginx
server {
    listen 80;
    server_name your-domain.com;

    root /var/www/smpp-gateway/dist;
    index index.html;

    # 前端路由
    location / {
        try_files $uri $uri/ /index.html;
    }

    # API 代理
    location /api/ {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    # Swagger UI
    location /swagger/ {
        proxy_pass http://localhost:8080;
    }
}
```

```bash
sudo nginx -t
sudo systemctl reload nginx
```

---

## 配置说明

### 环境变量

| 变量名 | 说明 | 默认值 |
|--------|------|--------|
| `ASPNETCORE_ENVIRONMENT` | 运行环境 | `Development` |
| `CONFIG_PATH` | 配置文件路径 | `config.json` |

### 配置项详解

#### Database (数据库)
```json
"Database": {
  "Host": "localhost",      // PostgreSQL 主机
  "Port": 5432,             // PostgreSQL 端口
  "Username": "postgres",   // 数据库用户名
  "Password": "postgres",   // 数据库密码
  "Database": "smpp_gateway" // 数据库名
}
```

#### RabbitMQ (可选)
```json
"RabbitMq": {
  "Host": "localhost",      // RabbitMQ 主机
  "Port": 5672,             // RabbitMQ 端口
  "Username": "guest",      // 用户名
  "Password": "guest"       // 密码
}
```

#### Accounts (SMPP 通道)
```json
"Accounts": [{
  "Id": "account-1",        // 通道唯一标识
  "Name": "运营商1",         // 通道名称
  "Host": "127.0.0.1",      // SMSC IP 地址
  "Port": 2775,             // SMPP 端口
  "SystemId": "smppclient", // SMPP System ID
  "Password": "password",  // SMPP 密码
  "SystemType": "SMPP",     // 系统类型
  "Weight": 100,            // 负载权重
  "Priority": 1,            // 优先级 (1-10)
  "MaxTps": 100,            // 最大 TPS
  "MaxSessions": 2,         // 最大会话数
  "Enabled": true           // 是否启用
}]
```

---

## Docker 部署

### Dockerfile (后端)

```dockerfile
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

# 健康检查
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/healthz || exit 1

EXPOSE 8080
ENTRYPOINT ["dotnet", "SmppGateway.dll"]
```

### Docker Compose

```yaml
version: '3.8'

services:
  smpp-gateway:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    volumes:
      - ./src/SmppGateway/config.json:/app/config.json:ro
    depends_on:
      - postgres
      - rabbitmq
    restart: unless-stopped
    networks:
      - smpp-network

  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_USER: smpp_user
      POSTGRES_PASSWORD: your_password
      POSTGRES_DB: smpp_gateway
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    restart: unless-stopped
    networks:
      - smpp-network

  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports:
      - "5672:5672"
      - "15672:15672"
    restart: unless-stopped
    networks:
      - smpp-network

  nginx:
    image: nginx:alpine
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
```

### 部署命令

```bash
# 构建并启动所有服务
docker-compose up -d

# 查看服务状态
docker-compose ps

# 查看日志
docker-compose logs -f smpp-gateway

# 停止服务
docker-compose down
```

---

## 验证部署

### 1. 健康检查

```bash
# 检查后端健康状态
curl http://localhost:8080/healthz

# 检查 Prometheus 指标
curl http://localhost:8080/metrics
```

### 2. Swagger API 文档

访问 `http://localhost:8080/swagger/` 查看 API 文档。

### 3. 前端页面

访问 `http://localhost:80` 或 `http://your-domain.com`

### 4. 管理员登录

- **管理员密钥**: `admin-a1b2c3d4e5f678901234567890123456` (默认配置)
- **登录地址**: `http://your-domain.com/login`
- 选择"管理员登录"模式

### 5. 用户使用

1. 管理员创建用户账号
2. 设置用户 API Key 和余额
3. 分配可发送的国家/地区权限
4. 用户使用 API Key 登录发送短信

---

## 目录结构

```
/workspace/
├── SmppClient.sln              # 解决方案文件
├── src/
│   ├── SmppGateway/            # API 网关项目
│   │   ├── Controllers/        # 控制器
│   │   ├── Services/           # 业务服务
│   │   ├── Auth/               # 认证处理
│   │   ├── Configuration/      # 配置类
│   │   ├── Observability/      # 可观测性
│   │   ├── Models/             # 数据模型
│   │   ├── config.json         # 配置文件
│   │   └── SmppGateway.csproj
│   ├── SmppClient/             # SMPP 协议客户端
│   │   ├── Connection/         # 连接管理
│   │   ├── Protocol/           # PDU 编解码
│   │   ├── Core/               # 核心组件
│   │   ├── Services/           # 服务处理
│   │   ├── Routing/            # 路由策略
│   │   ├── Queue/              # 队列适配器
│   │   ├── Resilience/         # 弹性机制
│   │   └── SmppClient.csproj
│   └── SmppStorage/            # 数据存储层
│       ├── Entities/           # 实体定义
│       ├── Data/               # DbContext
│       ├── Repositories/      # 仓储模式
│       └── SmppStorage.csproj
├── frontend/                   # 前端项目
│   ├── src/
│   │   ├── api/                # API 调用
│   │   ├── components/         # 公共组件
│   │   ├── views/              # 页面视图
│   │   ├── router/             # 路由配置
│   │   ├── stores/             # 状态管理
│   │   └── main.ts
│   ├── package.json
│   └── vite.config.ts
└── tests/                      # 测试项目
    └── SmppClient.Tests/
```

---

## 功能清单

### 后端功能 ✅

| 模块 | 功能 | 状态 |
|------|------|------|
| **用户管理** | 创建/编辑/删除用户 | ✅ |
| | 用户余额管理 | ✅ |
| | 用户启用/禁用 | ✅ |
| **通道管理** | SMPP 通道配置 | ✅ |
| | 通道动态启禁用 | ✅ |
| | 通道状态监控 | ✅ |
| **短信发送** | 单条短信提交 | ✅ |
| | 批量短信提交 | ✅ |
| | 长短信自动拆分 | ✅ |
| | 短信状态查询 | ✅ |
| **计费** | 按国家/号段计费 | ✅ |
| | 余额扣减 | ✅ |
| **权限** | 国家/地区发送权限 | ✅ |
| | API Key 认证 | ✅ |
| **告警** | 通道异常告警 | ✅ |
| | 余额不足告警 | ✅ |
| | 告警历史记录 | ✅ |
| **可观测性** | Prometheus 指标 | ✅ |
| | 健康检查 | ✅ |
| | Swagger API 文档 | ✅ |
| **配置热更新** | 配置文件监控 | ✅ |
| | 运行时配置重载 | ✅ |

### 前端功能 ✅

| 模块 | 功能 | 状态 |
|------|------|------|
| **登录** | 用户登录 (API Key) | ✅ |
| | 管理员登录 | ✅ |
| **仪表盘** | 系统概览统计 | ✅ |
| | 通道状态列表 | ✅ |
| | 告警信息展示 | ✅ |
| **用户管理** | 用户列表 | ✅ |
| | 创建/编辑用户 | ✅ |
| | 用户充值 | ✅ |
| | 国家权限分配 | ✅ |
| **通道管理** | 通道列表 | ✅ |
| | 添加/编辑通道 | ✅ |
| | 启用/禁用通道 | ✅ |
| | 通道统计详情 | ✅ |
| | 会话管理 | ✅ |
| **短信发送** | 发送短信 | ✅ |
| | 余额显示 | ✅ |
| | 发送历史 | ✅ |
| **告警管理** | 告警列表 | ✅ |
| | 告警处理 | ✅ |
| **设置** | Webhook 配置 | ✅ |

---

## 注意事项

1. **安全建议**
   - 生产环境务必修改 `AdminApiKey`
   - 使用 HTTPS 传输
   - API Key 妥善保管

2. **性能调优**
   - 根据实际 TPS 调整 `MaxTps`
   - 合理设置 `MaxSessions` 数量
   - 监控数据库连接池

3. **高可用**
   - 建议使用负载均衡
   - 配置多通道冗余
   - 定期备份数据库
