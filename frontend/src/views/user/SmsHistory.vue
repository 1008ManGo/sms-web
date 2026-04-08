<template>
  <div class="sms-history-page">
    <el-card>
      <template #header>
        <span>发送历史</span>
      </template>

      <el-form :inline="true" :model="queryForm" class="query-form">
        <el-form-item label="手机号">
          <el-input v-model="queryForm.mobile" placeholder="搜索手机号" clearable />
        </el-form-item>
        <el-form-item label="时间范围">
          <el-date-picker
            v-model="queryForm.dateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleQuery">查询</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>

      <el-table :data="historyList" style="width: 100%" v-loading="loading">
        <el-table-column prop="localId" label="本地ID" width="150" />
        <el-table-column prop="mobile" label="手机号" width="150" />
        <el-table-column prop="content" label="内容" />
        <el-table-column prop="segmentCount" label="分段数" width="80" />
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="submitTime" label="提交时间" width="180">
          <template #default="{ row }">
            {{ formatTime(row.submitTime) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120">
          <template #default="{ row }">
            <el-button size="small" @click="viewDetail(row)">详情</el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-pagination
        v-model:current-page="pagination.page"
        v-model:page-size="pagination.pageSize"
        :total="pagination.total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="handleSizeChange"
        @current-change="handlePageChange"
        style="margin-top: 20px; justify-content: flex-end"
      />
    </el-card>

    <!-- Detail Dialog -->
    <el-dialog v-model="showDetailDialog" title="短信详情" width="600px">
      <el-descriptions :column="2" border v-if="currentRecord">
        <el-descriptions-item label="本地ID" :span="2">{{ currentRecord.localId }}</el-descriptions-item>
        <el-descriptions-item label="手机号">{{ currentRecord.mobile }}</el-descriptions-item>
        <el-descriptions-item label="分段数">{{ currentRecord.segmentCount }}</el-descriptions-item>
        <el-descriptions-item label="内容" :span="2">{{ currentRecord.content }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="getStatusType(currentRecord.status)">
            {{ getStatusText(currentRecord.status) }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="提交时间">
          {{ formatTime(currentRecord.submitTime) }}
        </el-descriptions-item>
        <el-descriptions-item label="DLR状态" :span="2" v-if="currentRecord.dlrStatus">
          {{ currentRecord.dlrStatus }}
        </el-descriptions-item>
        <el-descriptions-item label="错误码" :span="2" v-if="currentRecord.errorCode">
          {{ currentRecord.errorCode }}
        </el-descriptions-item>
      </el-descriptions>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { userApi } from '../../api'

const loading = ref(false)
const showDetailDialog = ref(false)
const currentRecord = ref<any>(null)
const historyList = ref<any[]>([])

const queryForm = reactive({
  mobile: '',
  dateRange: null as any
})

const pagination = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

const loadHistory = async () => {
  loading.value = true
  try {
    const res = await userApi.getHistory({
      page: pagination.page,
      pageSize: pagination.pageSize
    })
    
    if (res.success) {
      const data = res.data
      if (Array.isArray(data)) {
        historyList.value = data
        pagination.total = data.length
      } else if (data && typeof data === 'object') {
        historyList.value = data.records || data.list || Object.values(data)[0] || []
        pagination.total = data.total || historyList.value.length
      } else {
        historyList.value = []
        pagination.total = 0
      }
    }
  } catch (error) {
    console.error('Failed to load history:', error)
  } finally {
    loading.value = false
  }
}

const handleQuery = () => {
  pagination.page = 1
  loadHistory()
}

const handleReset = () => {
  queryForm.mobile = ''
  queryForm.dateRange = null
  handleQuery()
}

const handleSizeChange = () => {
  loadHistory()
}

const handlePageChange = () => {
  loadHistory()
}

const viewDetail = (row: any) => {
  currentRecord.value = row
  showDetailDialog.value = true
}

const getStatusType = (status: string) => {
  switch (status) {
    case 'Submitted': return 'primary'
    case 'Delivered': return 'success'
    case 'Failed': return 'danger'
    default: return 'info'
  }
}

const getStatusText = (status: string) => {
  const map: Record<string, string> = {
    Submitted: '已提交',
    Delivered: '已送达',
    Failed: '失败',
    Pending: '待处理',
    Expired: '过期',
    Unknown: '未知'
  }
  return map[status] || status
}

const formatTime = (time: string) => {
  return time ? new Date(time).toLocaleString() : '-'
}

onMounted(() => {
  loadHistory()
})
</script>

<style scoped>
.query-form {
  margin-bottom: 20px;
}
</style>
