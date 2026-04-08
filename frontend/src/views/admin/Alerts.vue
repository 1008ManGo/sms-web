<template>
  <div class="alerts-page">
    <el-card>
      <template #header>
        <div class="header-actions">
          <span>告警管理</span>
          <el-button @click="loadAlerts">
            <el-icon><Refresh /></el-icon> 刷新
          </el-button>
        </div>
      </template>

      <el-tabs v-model="activeTab">
        <el-tab-pane label="未处理" name="unresolved">
          <el-table :data="alerts" style="width: 100%">
            <el-table-column prop="accountId" label="通道" width="150" />
            <el-table-column prop="type" label="类型" width="120">
              <template #default="{ row }">
                <el-tag>{{ formatAlertType(row.type) }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="severity" label="严重度" width="100">
              <template #default="{ row }">
                <el-tag :type="getSeverityType(row.severity)">{{ row.severity }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="message" label="消息" />
            <el-table-column prop="createdAt" label="时间" width="180">
              <template #default="{ row }">
                {{ formatTime(row.createdAt) }}
              </template>
            </el-table-column>
            <el-table-column label="操作" width="150">
              <template #default="{ row }">
                <el-button size="small" type="primary" @click="resolveAlert(row)">
                  处理
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
        <el-tab-pane label="全部" name="all">
          <el-table :data="allAlerts" style="width: 100%">
            <el-table-column prop="accountId" label="通道" width="150" />
            <el-table-column prop="type" label="类型" width="120">
              <template #default="{ row }">
                <el-tag>{{ formatAlertType(row.type) }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="severity" label="严重度" width="100">
              <template #default="{ row }">
                <el-tag :type="getSeverityType(row.severity)">{{ row.severity }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="message" label="消息" />
            <el-table-column prop="isResolved" label="状态" width="100">
              <template #default="{ row }">
                <el-tag :type="row.isResolved ? 'info' : 'warning'">
                  {{ row.isResolved ? '已处理' : '未处理' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="createdAt" label="时间" width="180">
              <template #default="{ row }">
                {{ formatTime(row.createdAt) }}
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Refresh } from '@element-plus/icons-vue'
import { adminApi } from '../../api'

const activeTab = ref('unresolved')
const alerts = ref<any[]>([])
const allAlerts = ref<any[]>([])

const loadAlerts = async () => {
  try {
    const [unresolvedRes, allRes] = await Promise.all([
      adminApi.getAlerts({ unresolvedOnly: true }),
      adminApi.getAlerts({ limit: 100 })
    ])
    
    if (unresolvedRes.success) {
      alerts.value = unresolvedRes.data || []
    }
    if (allRes.success) {
      allAlerts.value = allRes.data || []
    }
  } catch (error) {
    console.error('Failed to load alerts:', error)
  }
}

const resolveAlert = async (alert: any) => {
  try {
    await adminApi.resolveAlert(alert.id)
    ElMessage.success('告警已处理')
    loadAlerts()
  } catch (error) {
    console.error('Failed to resolve alert:', error)
  }
}

const formatAlertType = (type: string) => {
  const map: Record<string, string> = {
    ChannelUp: '通道上线',
    ChannelDown: '通道下线',
    HighLoad: '高负载',
    QueueBacklog: '队列积压',
    ConnectionLost: '连接丢失',
    ReconnectSuccess: '重连成功',
    ReconnectFailed: '重连失败'
  }
  return map[type] || type
}

const getSeverityType = (severity: string) => {
  switch (severity) {
    case 'Critical': return 'danger'
    case 'Warning': return 'warning'
    default: return 'info'
  }
}

const formatTime = (time: string) => {
  return new Date(time).toLocaleString()
}

onMounted(() => {
  loadAlerts()
})
</script>

<style scoped>
.header-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
