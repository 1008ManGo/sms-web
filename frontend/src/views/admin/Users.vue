<template>
  <div class="users-page">
    <el-card>
      <template #header>
        <div class="header-actions">
          <span>用户管理</span>
          <div>
            <el-button type="primary" @click="showAddDialog = true">
              <el-icon><Plus /></el-icon> 创建用户
            </el-button>
            <el-button @click="loadUsers">
              <el-icon><Refresh /></el-icon> 刷新
            </el-button>
          </div>
        </div>
      </template>

      <el-table :data="users" style="width: 100%">
        <el-table-column prop="username" label="用户名" width="150" />
        <el-table-column prop="balance" label="余额" width="120">
          <template #default="{ row }">
            <span style="color: #67C23A; font-weight: bold">¥{{ row.balance?.toFixed(2) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.status === 'Active' ? 'success' : 'warning'">
              {{ row.status === 'Active' ? '正常' : '暂停' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="allowedCountries" label="国家" width="200">
          <template #default="{ row }">
            <el-tag v-for="c in (row.allowedCountries || []).slice(0, 3)" :key="c" size="small" style="margin-right: 4px">
              {{ c }}
            </el-tag>
            <span v-if="(row.allowedCountries || []).length > 3">+{{ row.allowedCountries.length - 3 }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="allowedChannels" label="通道" width="150">
          <template #default="{ row }">
            {{ row.allowedChannels?.length || 0 }} 个通道
          </template>
        </el-table-column>
        <el-table-column label="操作" width="350">
          <template #default="{ row }">
            <el-button size="small" @click="viewUser(row)">详情</el-button>
            <el-button size="small" type="primary" @click="rechargeUser(row)">充值</el-button>
            <el-button size="small" @click="assignCountries(row)">分配国家</el-button>
            <el-button size="small" @click="assignChannels(row)">分配通道</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- Create User Dialog -->
    <el-dialog v-model="showAddDialog" title="创建用户" width="500px">
      <el-form :model="userForm" label-width="100px">
        <el-form-item label="用户名">
          <el-input v-model="userForm.username" />
        </el-form-item>
        <el-form-item label="密码">
          <el-input v-model="userForm.password" type="password" show-password />
        </el-form-item>
        <el-form-item label="初始余额">
          <el-input-number v-model="userForm.initialBalance" :min="0" :precision="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showAddDialog = false">取消</el-button>
        <el-button type="primary" @click="createUser">创建</el-button>
      </template>
    </el-dialog>

    <!-- User Detail Dialog -->
    <el-dialog v-model="showDetailDialog" title="用户详情" width="700px">
      <el-descriptions :column="2" border v-if="currentUser">
        <el-descriptions-item label="用户名">{{ currentUser.username }}</el-descriptions-item>
        <el-descriptions-item label="用户ID">{{ currentUser.id }}</el-descriptions-item>
        <el-descriptions-item label="余额">
          <span style="color: #67C23A; font-weight: bold">¥{{ currentUser.balance?.toFixed(2) }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="状态">{{ currentUser.status }}</el-descriptions-item>
        <el-descriptions-item label="允许国家" :span="2">
          <el-tag v-for="c in currentUser.allowedCountries" :key="c" size="small" style="margin-right: 4px">
            {{ c }}
          </el-tag>
          <span v-if="!currentUser.allowedCountries?.length">未分配</span>
        </el-descriptions-item>
        <el-descriptions-item label="允许通道" :span="2">
          <span v-for="ch in currentUser.allowedChannels" :key="ch.accountId" style="margin-right: 8px">
            {{ ch.accountName || ch.accountId }}
          </span>
          <span v-if="!currentUser.allowedChannels?.length">未分配</span>
        </el-descriptions-item>
        <el-descriptions-item label="自定义价格" :span="2">
          <el-tag v-for="p in currentUser.customPrices" :key="p.countryCode" size="small" style="margin-right: 4px">
            {{ p.countryCode }}: ¥{{ p.pricePerSegment }}
          </el-tag>
          <span v-if="!currentUser.customPrices?.length">使用默认价格</span>
        </el-descriptions-item>
      </el-descriptions>
    </el-dialog>

    <!-- Recharge Dialog -->
    <el-dialog v-model="showRechargeDialog" title="用户充值" width="400px">
      <el-form :model="rechargeForm" label-width="80px">
        <el-form-item label="用户名">
          <el-input :value="currentUser?.username" disabled />
        </el-form-item>
        <el-form-item label="充值金额">
          <el-input-number v-model="rechargeForm.amount" :min="0.01" :precision="2" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="rechargeForm.description" placeholder="可选" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showRechargeDialog = false">取消</el-button>
        <el-button type="primary" @click="doRecharge">充值</el-button>
      </template>
    </el-dialog>

    <!-- Assign Countries Dialog -->
    <el-dialog v-model="showCountriesDialog" title="分配国家" width="500px">
      <el-transfer
        v-model="selectedCountries"
        :data="availableCountries"
        :props="{ key: 'code', label: 'name' }"
        filterable
        filter-placeholder="搜索国家"
        titles="可用国家"
        selected-country="已选择"
      />
      <template #footer>
        <el-button @click="showCountriesDialog = false">取消</el-button>
        <el-button type="primary" @click="doAssignCountries">保存</el-button>
      </template>
    </el-dialog>

    <!-- Assign Channels Dialog -->
    <el-dialog v-model="showChannelsDialog" title="分配通道" width="500px">
      <el-checkbox-group v-model="selectedChannelIds">
        <el-checkbox 
          v-for="ch in availableChannels" 
          :key="ch.accountId" 
          :value="ch.accountId"
          style="display: block; margin-bottom: 8px"
        >
          {{ ch.accountName }} ({{ ch.accountId }})
        </el-checkbox>
      </el-checkbox-group>
      <template #footer>
        <el-button @click="showChannelsDialog = false">取消</el-button>
        <el-button type="primary" @click="doAssignChannels">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Plus, Refresh } from '@element-plus/icons-vue'
import { adminApi } from '../../api'

const users = ref<any[]>([])
const channels = ref<any[]>([])
const availableCountries = ref<any[]>([])

const showAddDialog = ref(false)
const showDetailDialog = ref(false)
const showRechargeDialog = ref(false)
const showCountriesDialog = ref(false)
const showChannelsDialog = ref(false)

const currentUser = ref<any>(null)
const selectedCountries = ref<string[]>([])
const selectedChannelIds = ref<string[]>([])

const userForm = reactive({
  username: '',
  password: '',
  initialBalance: 0
})

const rechargeForm = reactive({
  amount: 0,
  description: ''
})

const loadUsers = async () => {
  try {
    const res = await adminApi.getUsers()
    if (res.success) {
      users.value = res.data || []
    }
  } catch (error) {
    console.error('Failed to load users:', error)
  }
}

const loadChannels = async () => {
  try {
    const res = await adminApi.getChannels()
    if (res.success) {
      channels.value = res.data || []
    }
  } catch (error) {
    console.error('Failed to load channels:', error)
  }
}

const loadCountries = async () => {
  try {
    const res = await adminApi.getCountries()
    if (res.success) {
      availableCountries.value = res.data || []
    }
  } catch (error) {
    console.error('Failed to load countries:', error)
  }
}

const createUser = async () => {
  try {
    await adminApi.createUser(userForm)
    ElMessage.success('用户创建成功')
    showAddDialog.value = false
    userForm.username = ''
    userForm.password = ''
    userForm.initialBalance = 0
    loadUsers()
  } catch (error) {
    console.error('Failed to create user:', error)
  }
}

const viewUser = async (user: any) => {
  try {
    const res = await adminApi.getUser(user.id)
    if (res.success) {
      currentUser.value = res.data
      showDetailDialog.value = true
    }
  } catch (error) {
    console.error('Failed to load user details:', error)
  }
}

const rechargeUser = (user: any) => {
  currentUser.value = user
  rechargeForm.amount = 0
  rechargeForm.description = ''
  showRechargeDialog.value = true
}

const doRecharge = async () => {
  try {
    await adminApi.rechargeUser(currentUser.value.id, rechargeForm)
    ElMessage.success('充值成功')
    showRechargeDialog.value = false
    loadUsers()
  } catch (error) {
    console.error('Failed to recharge:', error)
  }
}

const assignCountries = async (user: any) => {
  currentUser.value = user
  try {
    const res = await adminApi.getUserCountries(user.id)
    selectedCountries.value = res.data || []
  } catch {
    selectedCountries.value = []
  }
  showCountriesDialog.value = true
}

const doAssignCountries = async () => {
  try {
    await adminApi.assignCountries(currentUser.value.id, selectedCountries.value)
    ElMessage.success('国家分配成功')
    showCountriesDialog.value = false
    loadUsers()
  } catch (error) {
    console.error('Failed to assign countries:', error)
  }
}

const assignChannels = (user: any) => {
  currentUser.value = user
  selectedChannelIds.value = user.allowedChannels?.map((c: any) => c.accountId) || []
  showChannelsDialog.value = true
}

const doAssignChannels = async () => {
  try {
    const channelAssignments = selectedChannelIds.value.map((id: string) => ({ accountId: id, maxTps: 100 }))
    await adminApi.batchAssignChannels([currentUser.value.id], channelAssignments)
    ElMessage.success('通道分配成功')
    showChannelsDialog.value = false
    loadUsers()
  } catch (error) {
    console.error('Failed to assign channels:', error)
  }
}

onMounted(() => {
  loadUsers()
  loadChannels()
  loadCountries()
})
</script>

<style scoped>
.header-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
