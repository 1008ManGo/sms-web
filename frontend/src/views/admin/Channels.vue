<template>
  <div class="channels-page">
    <el-card>
      <template #header>
        <div class="header-actions">
          <span>通道管理</span>
          <div>
            <el-button type="primary" @click="showAddDialog = true">
              <el-icon><Plus /></el-icon> 添加通道
            </el-button>
            <el-button @click="loadChannels">
              <el-icon><Refresh /></el-icon> 刷新
            </el-button>
          </div>
        </div>
      </template>

      <el-table :data="channels" style="width: 100%">
        <el-table-column prop="accountId" label="通道ID" width="150" />
        <el-table-column prop="name" label="名称" width="150" />
        <el-table-column prop="host" label="主机" width="150" />
        <el-table-column prop="port" label="端口" width="80" />
        <el-table-column prop="maxTps" label="最大TPS" width="100" />
        <el-table-column prop="maxSessions" label="会话数" width="100" />
        <el-table-column prop="weight" label="权重" width="80" />
        <el-table-column prop="enabled" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.enabled ? 'success' : 'info'">
              {{ row.enabled ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="280">
          <template #default="{ row }">
            <el-button size="small" @click="viewStats(row)">统计</el-button>
            <el-button size="small" type="primary" @click="editChannel(row)">编辑</el-button>
            <el-button 
              size="small" 
              :type="row.enabled ? 'warning' : 'success'" 
              @click="toggleChannel(row)"
            >
              {{ row.enabled ? '禁用' : '启用' }}
            </el-button>
            <el-button size="small" type="danger" @click="deleteChannel(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- Add/Edit Dialog -->
    <el-dialog v-model="showAddDialog" :title="editingChannel ? '编辑通道' : '添加通道'" width="600px">
      <el-form :model="channelForm" label-width="100px">
        <el-form-item label="通道ID">
          <el-input v-model="channelForm.accountId" :disabled="!!editingChannel" />
        </el-form-item>
        <el-form-item label="名称">
          <el-input v-model="channelForm.name" />
        </el-form-item>
        <el-form-item label="主机">
          <el-input v-model="channelForm.host" />
        </el-form-item>
        <el-form-item label="端口">
          <el-input-number v-model="channelForm.port" :min="1" :max="65535" />
        </el-form-item>
        <el-form-item label="System ID">
          <el-input v-model="channelForm.systemId" />
        </el-form-item>
        <el-form-item label="密码">
          <el-input v-model="channelForm.password" type="password" show-password />
        </el-form-item>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="最大TPS">
              <el-input-number v-model="channelForm.maxTps" :min="1" :max="1000" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="会话数">
              <el-input-number v-model="channelForm.maxSessions" :min="1" :max="10" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="权重">
              <el-input-number v-model="channelForm.weight" :min="1" :max="100" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="优先级">
              <el-input-number v-model="channelForm.priority" :min="1" :max="10" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="启用">
          <el-switch v-model="channelForm.enabled" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showAddDialog = false">取消</el-button>
        <el-button type="primary" @click="saveChannel">保存</el-button>
      </template>
    </el-dialog>

    <!-- Stats Dialog -->
    <el-dialog v-model="showStatsDialog" title="通道统计" width="700px">
      <el-descriptions :column="2" border v-if="currentStats">
        <el-descriptions-item label="通道ID">{{ currentStats.accountId }}</el-descriptions-item>
        <el-descriptions-item label="名称">{{ currentStats.name }}</el-descriptions-item>
        <el-descriptions-item label="连接状态">
          <el-tag :type="currentStats.isConnected ? 'success' : 'danger'">
            {{ currentStats.isConnected ? '已连接' : '未连接' }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="活跃会话">{{ currentStats.activeSessions }}</el-descriptions-item>
        <el-descriptions-item label="当前TPS">{{ currentStats.currentTps }}</el-descriptions-item>
        <el-descriptions-item label="最大TPS">{{ currentStats.maxTps }}</el-descriptions-item>
        <el-descriptions-item label="窗口使用率" :span="2">
          <el-progress :percentage="currentStats.windowUsagePercent || 0" />
        </el-descriptions-item>
        <el-descriptions-item label="待处理请求">{{ currentStats.pendingRequests }}</el-descriptions-item>
        <el-descriptions-item label="总会话数">{{ currentStats.totalSessions }}</el-descriptions-item>
      </el-descriptions>
      <el-table :data="currentStats?.sessions || []" style="margin-top: 20px" v-if="currentStats?.sessions?.length">
        <el-table-column prop="sessionId" label="会话ID" width="200" />
        <el-table-column prop="isConnected" label="已连接" width="100">
          <template #default="{ row }">
            <el-tag :type="row.isConnected ? 'success' : 'danger'" size="small">
              {{ row.isConnected ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="pendingCount" label="待处理" width="100" />
        <el-table-column prop="windowUsagePercent" label="窗口使用率">
          <template #default="{ row }">
            <el-progress :percentage="row.windowUsagePercent || 0" />
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Refresh } from '@element-plus/icons-vue'
import { adminApi } from '../../api'

const channels = ref<any[]>([])
const showAddDialog = ref(false)
const showStatsDialog = ref(false)
const editingChannel = ref<any>(null)
const currentStats = ref<any>(null)

const channelForm = reactive({
  accountId: '',
  name: '',
  host: '',
  port: 2775,
  systemId: '',
  password: '',
  maxTps: 50,
  maxSessions: 1,
  weight: 100,
  priority: 1,
  enabled: true
})

const loadChannels = async () => {
  try {
    const res = await adminApi.getChannels()
    if (res.success) {
      channels.value = res.data
    }
  } catch (error) {
    console.error('Failed to load channels:', error)
  }
}

const viewStats = async (channel: any) => {
  try {
    const res = await adminApi.getChannelStats(channel.accountId)
    if (res.success) {
      currentStats.value = res.data
      showStatsDialog.value = true
    }
  } catch (error) {
    console.error('Failed to load stats:', error)
  }
}

const editChannel = (channel: any) => {
  editingChannel.value = channel
  Object.assign(channelForm, channel)
  showAddDialog.value = true
}

const toggleChannel = async (channel: any) => {
  try {
    if (channel.enabled) {
      await adminApi.disableChannel(channel.accountId)
      ElMessage.success('通道已禁用')
    } else {
      await adminApi.enableChannel(channel.accountId)
      ElMessage.success('通道已启用')
    }
    loadChannels()
  } catch (error) {
    console.error('Failed to toggle channel:', error)
  }
}

const deleteChannel = async (channel: any) => {
  try {
    await ElMessageBox.confirm('确定要删除该通道吗？', '警告', {
      type: 'warning'
    })
    
    await adminApi.deleteChannel(channel.accountId, true)
    ElMessage.success('通道已删除')
    loadChannels()
  } catch (error: any) {
    if (error !== 'cancel') {
      console.error('Failed to delete channel:', error)
    }
  }
}

const saveChannel = async () => {
  try {
    if (editingChannel.value) {
      await adminApi.updateChannel(channelForm.accountId, channelForm)
      ElMessage.success('通道已更新')
    } else {
      await adminApi.createChannel(channelForm)
      ElMessage.success('通道已创建')
    }
    showAddDialog.value = false
    editingChannel.value = null
    loadChannels()
  } catch (error) {
    console.error('Failed to save channel:', error)
  }
}

onMounted(() => {
  loadChannels()
})
</script>

<style scoped>
.header-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
