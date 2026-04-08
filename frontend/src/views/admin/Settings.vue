<template>
  <div class="settings-page">
    <el-row :gutter="20">
      <el-col :span="12">
        <el-card>
          <template #header>
            <span>Webhook 通知配置</span>
          </template>
          
          <el-form :model="webhookForm" label-width="120px">
            <el-form-item label="启用 Webhook">
              <el-switch v-model="webhookForm.enabled" />
            </el-form-item>
            <el-form-item label="Webhook URL">
              <el-input 
                v-model="webhookForm.url" 
                placeholder="https://your-webhook-endpoint.com/alerts"
              />
            </el-form-item>
            <el-form-item label="Headers (JSON)">
              <el-input 
                v-model="webhookForm.headersJson" 
                type="textarea"
                :rows="3"
                placeholder='{"Authorization": "Bearer xxx"}'
              />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="saveWebhook">保存配置</el-button>
              <el-button @click="testWebhook">测试发送</el-button>
            </el-form-item>
          </el-form>

          <el-divider />

          <div class="webhook-preview">
            <h4>Webhook Payload 示例</h4>
            <pre>{{ webhookPayloadExample }}</pre>
          </div>
        </el-card>
      </el-col>
      
      <el-col :span="12">
        <el-card>
          <template #header>
            <span>系统信息</span>
          </template>
          
          <el-descriptions :column="1" border>
            <el-descriptions-item label="版本">1.0.0</el-descriptions-item>
            <el-descriptions-item label="环境">Production</el-descriptions-item>
            <el-descriptions-item label="数据库">PostgreSQL</el-descriptions-item>
            <el-descriptions-item label="消息队列">RabbitMQ</el-descriptions-item>
          </el-descriptions>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { ElMessage } from 'element-plus'
import { adminApi } from '../../api'

const webhookForm = reactive({
  enabled: false,
  url: '',
  headersJson: ''
})

const webhookPayloadExample = ref({
  type: 'alert',
  timestamp: new Date().toISOString(),
  alert: {
    id: '550e8400-e29b-41d4-a716-446655440000',
    accountId: 'channel-001',
    type: 'HighLoad',
    severity: 'Warning',
    message: 'Channel window usage is 85%',
    createdAt: new Date().toISOString()
  }
})

const loadWebhookConfig = async () => {
  try {
    const res = await adminApi.getWebhookConfig()
    if (res.success && res.data) {
      webhookForm.enabled = res.data.enabled || false
      webhookForm.url = res.data.url || ''
    }
  } catch (error) {
    console.error('Failed to load webhook config:', error)
  }
}

const saveWebhook = async () => {
  try {
    let headers = {}
    if (webhookForm.headersJson) {
      try {
        headers = JSON.parse(webhookForm.headersJson)
      } catch {
        ElMessage.error('Headers JSON 格式不正确')
        return
      }
    }
    
    await adminApi.configureWebhook({
      url: webhookForm.url,
      headers,
      enabled: webhookForm.enabled
    })
    ElMessage.success('Webhook 配置已保存')
  } catch (error) {
    console.error('Failed to save webhook:', error)
  }
}

const testWebhook = async () => {
  if (!webhookForm.url) {
    ElMessage.warning('请先配置 Webhook URL')
    return
  }
  ElMessage.info('测试通知已发送，请检查您的 Webhook 端点')
}

loadWebhookConfig()
</script>

<style scoped>
.webhook-preview pre {
  background: #f5f7fa;
  padding: 12px;
  border-radius: 4px;
  overflow-x: auto;
  font-size: 12px;
}
</style>
