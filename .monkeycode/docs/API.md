# API 文档

## 概述

SMPP Gateway 提供 RESTful API，支持短信发送、用户管理、通道管理等操作。

**Base URL**: `http://localhost:8080/api/v1`

---

## 认证

### 用户认证

使用 `X-Api-Key` Header:

```bash
curl -H "X-Api-Key: your_api_key" http://localhost:8080/api/v1/sms/balance
```

### 管理员认证

使用 `X-Admin-Key` Header:

```bash
curl -H "X-Admin-Key: admin_api_key" http://localhost:8080/api/v1/admin/users
```

---

## 短信接口

### 发送短信

**POST** `/sms/submit`

```bash
curl -X POST http://localhost:8080/api/v1/sms/submit \
  -H "X-Api-Key: your_api_key" \
  -H "Content-Type: application/json" \
  -d '{
    "mobile": "13800138000",
    "content": "您的验证码是1234",
    "ext": "12345"
  }'
```

**响应**:

```json
{
  "code": 0,
  "message": "success",
  "data": {
    "messageId": "abc123def456",
    "mobile": "13800138000"
  }
}
```

### 批量发送

**POST** `/sms/batch`

```bash
curl -X POST http://localhost:8080/api/v1/sms/batch \
  -H "X-Api-Key: your_api_key" \
  -H "Content-Type: application/json" \
  -d '{
    "list": [
      {"mobile": "13800138000", "content": "短信内容1"},
      {"mobile": "13800138001", "content": "短信内容2"}
    ]
  }'
```

**响应**:

```json
{
  "code": 0,
  "message": "success",
  "data": {
    "successCount": 2,
    "failCount": 0,
    "results": [
      {"messageId": "abc123", "mobile": "13800138000"},
      {"messageId": "def456", "mobile": "13800138001"}
    ]
  }
}
```

### 查询余额

**GET** `/sms/balance`

```bash
curl http://localhost:8080/api/v1/sms/balance \
  -H "X-Api-Key: your_api_key"
```

**响应**:

```json
{
  "code": 0,
  "message": "success",
  "data": {
    "balance": 1000.50
  }
}
```

### 查询发送历史

**GET** `/sms/history`

| 参数 | 类型 | 说明 |
|------|------|------|
| from | DateTime | 开始时间 (可选) |
| to | DateTime | 结束时间 (可选) |
| limit | int | 返回条数 (默认100) |

```bash
curl "http://localhost:8080/api/v1/sms/history?limit=10" \
  -H "X-Api-Key: your_api_key"
```

**响应**:

```json
{
  "code": 0,
  "message": "success",
  "data": [
    {
      "localId": "abc123def456",
      "mobile": "13800138000",
      "content": "您的验证码是1234",
      "status": "Delivered",
      "submitTime": "2026-04-08T10:00:00Z",
      "dlrTime": "2026-04-08T10:00:01Z",
      "errorCode": null
    }
  ]
}
```

### 查询状态

**GET** `/sms/status/{localId}`

```bash
curl http://localhost:8080/api/v1/sms/status/abc123def456 \
  -H "X-Api-Key: your_api_key"
```

**响应**:

```json
{
  "code": 0,
  "message": "success",
  "data": {
    "localId": "abc123def456",
    "mobile": "13800138000",
    "status": "Delivered",
    "submitTime": "2026-04-08T10:00:00Z",
    "dlrTime": "2026-04-08T10:00:01Z",
    "delay": 1.5
  }
}
```

### 获取支持国家

**GET** `/sms/countries`

```bash
curl http://localhost:8080/api/v1/sms/countries \
  -H "X-Api-Key: your_api_key"
```

**响应**:

```json
{
  "code": 0,
  "message": "success",
  "data": [
    {"countryCode": "86", "name": "中国", "prefix": "+86"},
    {"countryCode": "1", "name": "美国", "prefix": "+1"}
  ]
}
```

---

## 管理员接口

### 健康检查

**GET** `/admin/health`

```bash
curl http://localhost:8080/api/v1/admin/health \
  -H "X-Admin-Key: admin_api_key"
```

**响应**:

```json
{
  "code": 0,
  "message": "success",
  "data": {
    "status": "Healthy",
    "timestamp": "2026-04-08T10:00:00Z",
    "duration": 5.2,
    "checks": [
      {"name": "smpp_sessions", "status": "Healthy", "duration": 1.2},
      {"name": "database", "status": "Healthy", "duration": 2.5},
      {"name": "queue", "status": "Healthy", "duration": 1.5}
    ],
    "connectedChannels": 2,
    "healthySessions": 4,
    "totalPendingRequests": 10,
    "alerts": []
  }
}
```

### 用户管理

#### 获取用户列表

**GET** `/admin/users`

```bash
curl http://localhost:8080/api/v1/admin/users \
  -H "X-Admin-Key: admin_api_key"
```

#### 创建用户

**POST** `/admin/users`

```bash
curl -X POST http://localhost:8080/api/v1/admin/users \
  -H "X-Admin-Key: admin_api_key" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "password": "password123",
    "initialBalance": 100
  }'
```

#### 更新用户

**PUT** `/admin/users/{userId}`

```bash
curl -X PUT http://localhost:8080/api/v1/admin/users/550e8400-e29b-41d4-a716-446655440000 \
  -H "X-Admin-Key: admin_api_key" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "updateduser",
    "status": "Active"
  }'
```

#### 删除用户

**DELETE** `/admin/users/{userId}`

```bash
curl -X DELETE http://localhost:8080/api/v1/admin/users/550e8400-e29b-41d4-a716-446655440000 \
  -H "X-Admin-Key: admin_api_key"
```

#### 用户充值

**POST** `/admin/users/{userId}/recharge`

```bash
curl -X POST http://localhost:8080/api/v1/admin/users/550e8400-e29b-41d4-a716-446655440000/recharge \
  -H "X-Admin-Key: admin_api_key" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 500,
    "description": "充值"
  }'
```

### 通道管理

#### 获取通道列表

**GET** `/admin/channels`

```bash
curl http://localhost:8080/api/v1/admin/channels \
  -H "X-Admin-Key: admin_api_key"
```

#### 创建通道

**POST** `/admin/channels`

```bash
curl -X POST http://localhost:8080/api/v1/admin/channels \
  -H "X-Admin-Key: admin_api_key" \
  -H "Content-Type: application/json" \
  -d '{
    "accountId": "channel-1",
    "name": "运营商1",
    "host": "smsc.example.com",
    "port": 2775,
    "systemId": "smppclient",
    "password": "password",
    "maxTps": 100,
    "maxSessions": 2,
    "weight": 100,
    "enabled": true
  }'
```

#### 更新通道

**PUT** `/admin/channels/{accountId}`

```bash
curl -X PUT http://localhost:8080/api/v1/admin/channels/channel-1 \
  -H "X-Admin-Key: admin_api_key" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "运营商1_更新",
    "maxTps": 200
  }'
```

#### 删除通道

**DELETE** `/admin/channels/{accountId}`

```bash
curl -X DELETE "http://localhost:8080/api/v1/admin/channels/channel-1?hard=true" \
  -H "X-Admin-Key: admin_api_key"
```

#### 启用通道

**POST** `/admin/channels/{accountId}/enable`

```bash
curl -X POST http://localhost:8080/api/v1/admin/channels/channel-1/enable \
  -H "X-Admin-Key: admin_api_key"
```

#### 禁用通道

**POST** `/admin/channels/{accountId}/disable`

```bash
curl -X POST http://localhost:8080/api/v1/admin/channels/channel-1/disable \
  -H "X-Admin-Key: admin_api_key"
```

#### 获取通道统计

**GET** `/admin/channels/{accountId}/stats`

```bash
curl http://localhost:8080/api/v1/admin/channels/channel-1/stats \
  -H "X-Admin-Key: admin_api_key"
```

**响应**:

```json
{
  "code": 0,
  "message": "success",
  "data": {
    "accountId": "channel-1",
    "name": "运营商1",
    "isConnected": true,
    "activeSessions": 2,
    "currentTps": 45.5,
    "maxTps": 100,
    "windowUsagePercent": 45,
    "pendingRequests": 5,
    "totalSessions": 2
  }
}
```

### 批量操作

#### 批量启用通道

**POST** `/admin/channels/batch/enable`

```bash
curl -X POST http://localhost:8080/api/v1/admin/channels/batch/enable \
  -H "X-Admin-Key: admin_api_key" \
  -H "Content-Type: application/json" \
  -d '{
    "accountIds": ["channel-1", "channel-2"]
  }'
```

#### 批量禁用通道

**POST** `/admin/channels/batch/disable`

```bash
curl -X POST http://localhost:8080/api/v1/admin/channels/batch/disable \
  -H "X-Admin-Key: admin_api_key" \
  -H "Content-Type: application/json" \
  -d '{
    "accountIds": ["channel-1", "channel-2"]
  }'
```

### 国家权限

#### 获取国家列表

**GET** `/admin/countries`

```bash
curl http://localhost:8080/api/v1/admin/countries \
  -H "X-Admin-Key: admin_api_key"
```

#### 分配国家权限

**POST** `/admin/users/{userId}/countries`

```bash
curl -X POST http://localhost:8080/api/v1/admin/users/550e8400-e29b-41d4-a716-446655440000/countries \
  -H "X-Admin-Key: admin_api_key" \
  -H "Content-Type: application/json" \
  -d '{
    "countryCodes": ["86", "1", "44"]
  }'
```

### 告警管理

#### 获取告警列表

**GET** `/admin/alerts`

| 参数 | 类型 | 说明 |
|------|------|------|
| accountId | string | 通道 ID (可选) |
| unresolvedOnly | bool | 仅未解决 (默认 false) |
| limit | int | 返回条数 (默认 100) |

```bash
curl "http://localhost:8080/api/v1/admin/alerts?unresolvedOnly=true&limit=10" \
  -H "X-Admin-Key: admin_api_key"
```

#### 处理告警

**POST** `/admin/alerts/{alertId}/resolve`

```bash
curl -X POST http://localhost:8080/api/v1/admin/alerts/alert-123/resolve \
  -H "X-Admin-Key: admin_api_key"
```

### Webhook 配置

#### 获取配置

**GET** `/admin/webhook/config`

```bash
curl http://localhost:8080/api/v1/admin/webhook/config \
  -H "X-Admin-Key: admin_api_key"
```

#### 配置 Webhook

**POST** `/admin/webhook/config`

```bash
curl -X POST http://localhost:8080/api/v1/admin/webhook/config \
  -H "X-Admin-Key: admin_api_key" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://your-server.com/webhook",
    "headers": {
      "Authorization": "Bearer token"
    },
    "enabled": true
  }'
```

---

## 错误码

| 错误码 | 说明 |
|--------|------|
| 0 | 成功 |
| 1 | 通用错误 |
| 2 | 权限不足 |
| 3 | 余额不足 |
| 4 | 通道不可用 |
| 5 | 发送超时 |
| 401 | 未授权 |
| 404 | 资源不存在 |
| 500 | 服务器内部错误 |

---

## 状态值

### 用户状态

| 状态 | 说明 |
|------|------|
| Active | 正常 |
| Suspended | 暂停 |
| Deleted | 已删除 |

### 短信状态

| 状态 | 说明 |
|------|------|
| Pending | 待发送 |
| Submitted | 已提交 |
| Delivered | 已送达 |
| Failed | 发送失败 |
| Unknown | 未知 |

### 告警级别

| 级别 | 说明 |
|------|------|
| Info | 信息 |
| Warning | 警告 |
| Critical | 严重 |
