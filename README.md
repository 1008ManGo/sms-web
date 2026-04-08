# SMPP Gateway 短信网关管理系统

企业级 SMPP 短信网关平台，支持多通道管理、用户隔离、智能路由、计费管理和实时监控。

## 功能特性

### 核心功能
- **多通道管理**: 支持配置多个 SMPP 通道，动态启禁用
- **用户隔离**: 多用户独立 API Key、余额、权限管理
- **智能路由**: 支持权重路由、优先级路由、故障切换
- **计费管理**: 按国家/号段差异化计费
- **短信发送**: 支持单条、批量发送，长短信自动拆分
- **状态报告**: 实时 DLR 状态回执

### 高可用特性
- **弹性处理**: 熔断器 + 限流器保护系统稳定
- **自动重连**: 网络中断自动重连，指数退避
- **配置热更新**: 运行时配置修改，无需重启
- **健康检查**: 多维度健康检测，自动告警

### 可观测性
- **Prometheus 指标**: 全链路性能指标采集
- **健康检查端点**: `/healthz` 完整健康状态
- **Swagger 文档**: 在线 API 文档

---

## 系统架构

```
┌─────────────────────────────────────────────────────────────┐
│                      用户访问层                              │
│                   Web UI (Vue 3)                             │
└─────────────────────┬───────────────────────────────────────┘
                      │ HTTP
┌─────────────────────▼───────────────────────────────────────┐
│                   SmppGateway (.NET 8)                      │
│                    端口: 8080                                │
│  ┌─────────────┬──────────────┬──────────────────────────┐  │
│  │ Controllers │   Services   │    Observability        │  │
│  │ - Admin     │ - SmppClient │ - Prometheus Metrics   │  │
│  │ - User      │ - Billing    │ - Health Checks        │  │
│  │ - Sms       │ - Permission │ - Swagger/OpenAPI      │  │
│  │ - Health    │ - Alert      │                        │  │
│  └─────────────┴──────────────┴──────────────────────────┘  │
└────────┬──────────────────────┬───────────────────────────────┘
         │                      │
┌────────▼────────┐    ┌────────▼────────┐    ┌──────────────┐
│   SmppClient   │    │   SmppStorage    │    │   RabbitMQ   │
│   SMPP 协议     │    │   PostgreSQL     │    │   (可选)      │
│   端口: 2775   │    │   端口: 5432     │    │   端口: 5672 │
└────────────────┘    └──────────────────┘    └──────────────┘
```

---

## 技术栈

### 后端
- **.NET 8** - 运行时
- **ASP.NET Core 8** - Web 框架
- **Entity Framework Core 8** - ORM
- **PostgreSQL** - 主数据库
- **RabbitMQ** - 消息队列 (可选)
- **prometheus-net** - 指标采集

### 前端
- **Vue 3** - 框架
- **Element Plus** - UI 组件库
- **Pinia** - 状态管理
- **Vue Router** - 路由
- **Axios** - HTTP 客户端
- **Vite** - 构建工具

---

## 快速开始

### 环境要求
- .NET SDK 8.0+
- Node.js 18+
- PostgreSQL 14+
- pnpm 8+ (或 npm 9+)

### 后端启动

```bash
# 还原依赖
dotnet restore

# 编译
dotnet build

# 配置数据库 (编辑 config.json)
vim src/SmppGateway/config.json

# 运行
dotnet run --project src/SmppGateway/SmppGateway.csproj
```

### 前端启动

```bash
cd frontend

# 安装依赖
pnpm install

# 开发模式
pnpm dev

# 生产构建
pnpm build
```

---

## 项目结构

```
sms-web/
├── SmppClient.sln              # .NET 解决方案
├── DEPLOYMENT.md               # 部署文档
│
├── src/
│   ├── SmppGateway/           # API 网关
│   │   ├── Controllers/        # API 控制器
│   │   ├── Services/           # 业务服务
│   │   ├── Auth/               # 认证处理
│   │   ├── Configuration/      # 配置类
│   │   ├── Observability/      # 可观测性
│   │   ├── Models/             # 数据模型
│   │   └── config.json         # 配置文件
│   │
│   ├── SmppClient/             # SMPP 协议客户端
│   │   ├── Connection/          # 连接管理
│   │   ├── Protocol/            # PDU 编解码
│   │   ├── Core/                # 核心组件
│   │   ├── Services/            # 服务处理
│   │   ├── Routing/             # 路由策略
│   │   ├── Queue/               # 队列适配
│   │   └── Resilience/           # 弹性机制
│   │
│   └── SmppStorage/            # 数据存储层
│       ├── Entities/            # 实体定义
│       ├── Data/                # DbContext
│       └── Repositories/        # 仓储模式
│
├── frontend/                   # 前端项目
│   ├── src/
│   │   ├── api/                # API 调用
│   │   ├── components/          # 公共组件
│   │   ├── views/              # 页面视图
│   │   ├── router/             # 路由配置
│   │   └── stores/              # 状态管理
│   └── package.json
│
└── tests/                      # 测试项目
    └── SmppClient.Tests/
```

---

## API 文档

### 短信发送

```bash
POST /api/v1/sms/submit
X-Api-Key: <user_api_key>
Content-Type: application/json

{
  "mobile": "13800138000",
  "content": "您的验证码是1234"
}
```

### 管理员接口

```bash
# 健康检查
GET /api/v1/admin/health

# 用户管理
GET    /api/v1/admin/users
POST   /api/v1/admin/users
PUT    /api/v1/admin/users/{userId}
DELETE /api/v1/admin/users/{userId}

# 通道管理
GET    /api/v1/admin/channels
POST   /api/v1/admin/channels
PUT    /api/v1/admin/channels/{accountId}
DELETE /api/v1/admin/channels/{accountId}
POST   /api/v1/admin/channels/{accountId}/enable
POST   /api/v1/admin/channels/{accountId}/disable
```

详细 API 文档请访问: `http://localhost:8080/swagger/`

---

## 配置说明

### 配置文件

`src/SmppGateway/config.json`:

```json
{
  "Host": "0.0.0.0",
  "Port": 8080,
  "AdminApiKey": "admin-your-secure-key",
  "Database": {
    "Host": "localhost",
    "Port": 5432,
    "Username": "postgres",
    "Password": "postgres",
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
      "MaxTps": 100,
      "MaxSessions": 2,
      "Enabled": true
    }
  ]
}
```

---

## Docker 部署

```bash
# 构建所有服务
docker-compose up -d

# 查看日志
docker-compose logs -f smpp-gateway

# 停止服务
docker-compose down
```

---

## 监控指标

| 指标 | 类型 | 说明 |
|------|------|------|
| `smpp_connected_sessions` | Gauge | 当前连接数 |
| `smpp_submit_tps` | Gauge | 提交 TPS |
| `smpp_submit_success_total` | Counter | 成功数 |
| `smpp_submit_fail_total` | Counter | 失败数 |
| `smpp_dlr_delay_seconds` | Histogram | DLR 延迟 |
| `smpp_window_usage` | Gauge | 窗口使用率 |
| `smpp_circuit_breaker_state` | Gauge | 熔断状态 |

---

## 许可证

MIT License
