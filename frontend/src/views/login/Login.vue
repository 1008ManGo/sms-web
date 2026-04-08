<template>
  <div class="login-container">
    <el-card class="login-card">
      <template #header>
        <div class="card-header">
          <h2>SMPP Gateway</h2>
          <p>短信网关管理系统</p>
        </div>
      </template>
      
      <el-form :model="form" :rules="rules" ref="formRef" label-position="top">
        <el-form-item label="登录类型">
          <el-radio-group v-model="loginType">
            <el-radio label="user">用户登录</el-radio>
            <el-radio label="admin">管理员登录</el-radio>
          </el-radio-group>
        </el-form-item>
        
        <el-form-item label="用户名" v-if="loginType === 'admin'">
          <el-input v-model="form.username" placeholder="请输入用户名" />
        </el-form-item>
        
        <el-form-item label="API Key" v-if="loginType === 'user'">
          <el-input v-model="form.apiKey" placeholder="请输入API Key" />
        </el-form-item>
        
        <el-form-item label="密码" v-if="loginType === 'admin'">
          <el-input v-model="form.password" type="password" placeholder="请输入密码" show-password />
        </el-form-item>
        
        <el-form-item label="管理员密钥" v-if="loginType === 'admin'">
          <el-input v-model="form.adminKey" placeholder="请输入管理员密钥" />
        </el-form-item>
        
        <el-form-item>
          <el-button type="primary" :loading="loading" style="width: 100%" @click="handleLogin">
            登录
          </el-button>
        </el-form-item>
      </el-form>
      
      <div class="tips">
        <p v-if="loginType === 'user'">* 用户请使用API Key登录</p>
        <p v-else>* 管理员请使用用户名密码或管理员密钥登录</p>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useUserStore } from '../../stores/user'
import { userApi, adminApi } from '../../api'

const router = useRouter()
const userStore = useUserStore()

const loginType = ref<'user' | 'admin'>('user')
const loading = ref(false)
const formRef = ref()

const form = reactive({
  username: '',
  password: '',
  apiKey: '',
  adminKey: ''
})

const rules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }],
  apiKey: [{ required: true, message: '请输入API Key', trigger: 'blur' }],
  adminKey: [{ required: true, message: '请输入管理员密钥', trigger: 'blur' }]
}

const handleLogin = async () => {
  loading.value = true
  
  try {
    if (loginType.value === 'user') {
      if (!form.apiKey) {
        ElMessage.warning('请输入API Key')
        return
      }
      userStore.setUser({ apiKey: form.apiKey })
      ElMessage.success('用户登录成功')
      router.push('/sms')
    } else {
      if (form.adminKey) {
        userStore.setAdminKey(form.adminKey)
        ElMessage.success('管理员登录成功')
        router.push('/dashboard')
      } else if (form.username && form.password) {
        // In real app, call login API
        ElMessage.success('管理员登录成功')
        router.push('/dashboard')
      } else {
        ElMessage.warning('请输入管理员密钥或用户名密码')
      }
    }
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.login-card {
  width: 400px;
}

.card-header {
  text-align: center;
}

.card-header h2 {
  margin: 0 0 8px 0;
  color: #333;
}

.card-header p {
  margin: 0;
  color: #666;
}

.tips {
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid #eee;
  font-size: 12px;
  color: #999;
}
</style>
