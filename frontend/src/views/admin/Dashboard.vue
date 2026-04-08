<template>
  <div class="dashboard">
    <el-row :gutter="20">
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="stat-card">
            <el-icon class="stat-icon" color="#409EFF"><Connection /></el-icon>
            <div class="stat-content">
              <div class="stat-value">{{ stats.connectedChannels }}</div>
              <div class="stat-label">已连接通道</div>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="stat-card">
            <el-icon class="stat-icon" color="#67C23A"><<Connection /></el-icon>
            <div class="stat-content">
              <div class="stat-value">{{ stats.totalSessions }}</div>
              <div class="stat-label">活跃会话</div>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="stat-card">
            <el-icon class="stat-icon" color="#E6A23C"><Clock /></el-icon>
            <div class="stat-content">
              <div class="stat-value">{{ stats.pendingRequests }}</div>
              <div class="stat-label">待处理请求</div>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="stat-card">
            <el-icon class="stat-icon" color="#F56C6C"><Warning /></el-icon>
            <div class="stat-content">
              <div class="stat-value">{{ stats.alerts }}</div>
              <div class="stat-label">未处理告警</div>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="20" style="margin-top: 20px">
      <el-col :span="16">
        <el-card>
          <template #header>
            <span>通道状态</span>
          </template>
          <el-table :data="channelsStats" style="width: 100%">
            <el-table-column prop="accountId" label="通道ID" width="150" />
            <el-table-column prop="name" label="名称" width="150" />
            <el-table-column prop="isConnected" label="状态" width="100">
              <template #default="{ row }">
                <el-tag :type="row.isConnected ? 'success' : 'danger'">
                  {{ row.isConnected ? '已连接' : '未连接' }}
                </el-tag>
              </template>
            </el-table>
            <el-table-column prop="activeSessions" label="会话数" width="100" />
            <el-table-column prop="currentTps" label="当前TPS" width="100" />
            <el-table-column prop="windowUsagePercent" label="窗口使用率">
              <template #default="{ row }">
                <el-progress 
                  :percentage="row.windowUsagePercent || 0" 
                  :color="getProgressColor(row.windowUsagePercent)" 
                />
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card>
          <template #header>
            <span>最近告警</span>
          </template>
          <div class="alerts-list">
            <div v-for="alert in recentAlerts" :key="alert.id" class="alert-item">
              <el-tag :type="getSeverityType(alert.severity)" size="small">
                {{ alert.severity }}
              </el-tag>
              <span class="alert-message">{{ alert.message }}</span>
              <span class="alert-time">{{ formatTime(alert.createdAt) }}</span>
            </div>
            <el-empty v-if="recentAlerts.length === 0" description="暂无告警" />
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Connection, Clock, Warning } from '@element-plus/icons-vue'
import { adminApi } from '../../api'

const stats = ref({
  connectedChannels: 0,
  totalSessions: 0,
  pendingRequests: 0,
  alerts: 0
})

const channelsStats = ref<any[]>([])
const recentAlerts = ref<any[]>([])

const loadData = async () => {
  try {
    const [healthRes, alertsRes] = await Promise.all([
      adminApi.getSystemHealth(),
      adminApi.getAlerts({ unresolvedOnly: true, limit: 5 })
    ])
    
    if (healthRes.success) {
      const health = healthRes.data
      stats.value = {
        connectedChannels: health.connectedChannels || 0,
        totalSessions: health.healthySessions || 0,
        pendingRequests: health.totalPendingRequests || 0,
        alerts: health.alerts?.length || 0
      }
    }
    
    if (alertsRes.success) {
      recentAlerts.value = alertsRes.data || []
    }
    
    const statsRes = await adminApi.getAllChannelsStats()
    if (statsRes.success && statsRes.data?.stats) {
      channelsStats.value = statsRes.data.stats
    }
  } catch (error) {
    console.error('Failed to load dashboard data:', error)
  }
}

const getProgressColor = (percentage: number) => {
  if (percentage >= 80) return '#F56C6C'
  if (percentage >= 60) return '#E6A23C'
  return '#67C23A'
}

const getSeverityType = (severity: string) => {
  switch (severity) {
    case 'Critical': return 'danger'
    case 'Warning': return 'warning'
    default: return 'info'
  }
}

const formatTime = (time: string) => {
  const date = new Date(time)
  return date.toLocaleString()
}

onMounted(() => {
  loadData()
  setInterval(loadData, 30000)
})
</script>

<style scoped>
.stat-card {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-icon {
  font-size: 48px;
}

.stat-content {
  flex: 1;
}

.stat-value {
  font-size: 28px;
  font-weight: bold;
  color: #333;
}

.stat-label {
  font-size: 14px;
  color: #666;
}

.alerts-list {
  max-height: 300px;
  overflow-y: auto;
}

.alert-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 0;
  border-bottom: 1px solid #f0f0f0;
}

.alert-message {
  flex: 1;
  font-size: 13px;
}

.alert-time {
  font-size: 12px;
  color: #999;
}
</style>
