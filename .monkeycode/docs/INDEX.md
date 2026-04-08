# 项目文档索引

## 文档结构

```
.monkeycode/
├── docs/
│   ├── INDEX.md        # 文档索引 (本文件)
│   ├── ARCHITECTURE.md # 系统架构文档
│   └── API.md         # API 接口文档
│
└── specs/
    └── 2026-04-07-smpp-client-sdk/
        └── design.md   # 技术设计方案
```

---

## 文档说明

### 架构文档 (ARCHITECTURE.md)

系统架构文档，详细描述了：

- 系统概述与设计目标
- 整体架构图
- 核心组件说明 (SmppGateway, SmppClient, SmppStorage)
- 路由策略 (权重路由、故障切换)
- 弹性机制 (滑动窗口、熔断器、限流器)
- 协议支持 (PDU 类型、长短信拆分)
- 监控指标
- 健康检查
- 配置热更新
- 安全性

### API 文档 (API.md)

完整的 API 接口文档，包含：

- 认证方式 (API Key / Admin Key)
- 短信接口 (/sms/*)
  - 发送短信
  - 批量发送
  - 查询余额
  - 查询历史
  - 查询状态
  - 获取支持国家
- 管理员接口 (/admin/*)
  - 健康检查
  - 用户管理 (CRUD)
  - 通道管理 (CRUD)
  - 批量操作
  - 国家权限
  - 告警管理
  - Webhook 配置
- 错误码说明
- 状态值说明

### 部署文档 (DEPLOYMENT.md)

位于项目根目录，包含：

- 系统架构
- 环境要求
- 后端部署步骤
- 前端部署步骤
- 配置说明
- Docker 部署
- 验证部署
- 功能清单

### 技术设计文档 (specs/*/design.md)

详细的技术设计方案，描述了：

- 完整架构设计
- 核心流程 (发送短信、DLR 处理)
- 组件设计细节
- 数据模型
- API 设计
- 技术选型
- 项目结构
- 正确性属性
- 错误处理
- 部署架构
- 测试策略
- 安全与审计
- 性能演练计划

---

## 快速链接

- [架构文档](./ARCHITECTURE.md)
- [API 文档](./API.md)
- [部署文档](../DEPLOYMENT.md)
- [技术设计](../specs/2026-04-07-smpp-client-sdk/design.md)
