<template>
  <div class="sms-send-page">
    <el-row :gutter="20">
      <el-col :span="16">
        <el-card>
          <template #header>
            <span>发送短信</span>
          </template>
          
          <el-form :model="form" :rules="rules" ref="formRef" label-width="100px">
            <el-form-item label="手机号" prop="mobile">
              <el-input 
                v-model="form.mobile" 
                placeholder="请输入手机号，多个用逗号分隔"
                @keyup.enter="handleSubmit"
              />
            </el-form-item>
            <el-form-item label="短信内容" prop="content">
              <el-input 
                v-model="form.content" 
                type="textarea"
                :rows="5"
                :maxlength="1600"
                show-word-limit
                placeholder="请输入短信内容"
                @keyup.enter.ctrl="handleSubmit"
              />
            </el-form-item>
            <el-form-item label="余额">
              <span style="color: #67C23A; font-weight: bold; font-size: 18px">
                ¥{{ balance.toFixed(2) }}
              </span>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" size="large" :loading="loading" @click="handleSubmit">
                发送短信
              </el-button>
              <el-button size="large" @click="handleReset">重置</el-button>
            </el-form-item>
          </el-form>
        </el-card>
      </el-col>
      
      <el-col :span="8">
        <el-card style="margin-bottom: 20px">
          <template #header>
            <span>可发送国家</span>
          </template>
          <div class="countries-list">
            <el-tag v-for="c in allowedCountries" :key="c.code" size="small" style="margin: 4px">
              {{ c.code }} - {{ c.name }}
            </el-tag>
          </div>
        </el-card>
        
        <el-card>
          <template #header>
            <span>发送记录</span>
          </template>
          <div class="recent-results" v-if="recentResults.length">
            <div v-for="(r, i) in recentResults.slice(0, 5)" :key="i" class="result-item">
              <el-tag :type="r.success ? 'success' : 'danger'" size="small">
                {{ r.success ? '成功' : '失败' }}
              </el-tag>
              <span class="result-mobile">{{ r.mobile }}</span>
              <span class="result-id">{{ r.localId }}</span>
            </div>
          </div>
          <el-empty v-else description="暂无发送记录" />
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { userApi } from '../../api'

const formRef = ref()
const loading = ref(false)
const balance = ref(0)
const allowedCountries = ref<any[]>([])
const recentResults = ref<any[]>([])

const form = reactive({
  mobile: '',
  content: ''
})

const rules = {
  mobile: [{ required: true, message: '请输入手机号', trigger: 'blur' }],
  content: [{ required: true, message: '请输入短信内容', trigger: 'blur' }]
}

const loadData = async () => {
  try {
    const [balanceRes, countriesRes] = await Promise.all([
      userApi.getBalance(),
      userApi.getCountries()
    ])
    
    if (balanceRes.success) {
      balance.value = balanceRes.data?.balance || 0
    }
    if (countriesRes.success) {
      allowedCountries.value = countriesRes.data || []
    }
  } catch (error) {
    console.error('Failed to load data:', error)
  }
}

const handleSubmit = async () => {
  if (!form.mobile || !form.content) {
    ElMessage.warning('请填写手机号和内容')
    return
  }
  
  loading.value = true
  
  try {
    const mobiles = form.mobile.split(',').map(m => m.trim()).filter(m => m)
    
    for (const mobile of mobiles) {
      const res = await userApi.submitSms({
        mobile,
        content: form.content
      })
      
      recentResults.value.unshift({
        success: res.success,
        mobile,
        localId: res.data?.localId || res.messageId || 'N/A'
      })
      
      if (res.success) {
        ElMessage.success(`短信提交成功: ${mobile}`)
      } else {
        ElMessage.error(`短信提交失败: ${mobile} - ${res.message}`)
      }
    }
    
    // Refresh balance
    const balanceRes = await userApi.getBalance()
    if (balanceRes.success) {
      balance.value = balanceRes.data?.balance || 0
    }
    
    handleReset()
  } catch (error: any) {
    ElMessage.error(error.message || '提交失败')
  } finally {
    loading.value = false
  }
}

const handleReset = () => {
  form.mobile = ''
  form.content = ''
  formRef.value?.clearValidate()
}

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.countries-list {
  max-height: 200px;
  overflow-y: auto;
}

.result-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 0;
  border-bottom: 1px solid #f0f0f0;
}

.result-mobile {
  flex: 1;
  font-size: 13px;
}

.result-id {
  font-size: 11px;
  color: #999;
}
</style>
