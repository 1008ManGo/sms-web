import axios from 'axios'
import { ElMessage } from 'element-plus'

const api = axios.create({
  baseURL: '/api/v1',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
})

api.interceptors.request.use((config) => {
  const userKey = localStorage.getItem('user_api_key')
  const adminKey = localStorage.getItem('admin_api_key')
  
  if (userKey && config.url?.startsWith('/sms')) {
    config.headers['X-Api-Key'] = userKey
  } else if (adminKey && config.url?.startsWith('/admin')) {
    config.headers['X-Admin-Key'] = adminKey
  }
  
  return config
})

api.interceptors.response.use(
  (response) => response.data,
  (error) => {
    const message = error.response?.data?.message || error.message || 'Request failed'
    ElMessage.error(message)
    return Promise.reject(error)
  }
)

export default api

export const userApi = {
  submitSms: (data: { mobile: string; content: string; idempotencyKey?: string }) => 
    api.post('/sms/submit', data),
  
  batchSubmit: (data: { messages: Array<{ mobile: string; content: string; idempotencyKey?: string }> }) => 
    api.post('/sms/batch', data),
  
  getStatus: (localId: string) => 
    api.get(`/sms/status/${localId}`),
  
  getHistory: (params?: { page?: number; pageSize?: number }) => 
    api.get('/sms/history', { params }),
  
  getBalance: () => 
    api.get('/sms/balance'),
  
  getCountries: () => 
    api.get('/sms/countries')
}

export const adminApi = {
  getUsers: () => api.get('/admin/users'),
  getUser: (userId: string) => api.get(`/admin/users/${userId}`),
  createUser: (data: { username: string; password: string; initialBalance?: number }) => 
    api.post('/admin/users', data),
  rechargeUser: (userId: string, data: { amount: number; description?: string }) => 
    api.post(`/admin/users/${userId}/recharge`, data),
  getUserBalance: (userId: string) => api.get(`/admin/users/${userId}/balance`),
  
  getCountries: () => api.get('/admin/countries'),
  assignCountries: (userId: string, countryCodes: string[]) => 
    api.post(`/admin/users/${userId}/countries`, { countryCodes }),
  removeCountry: (userId: string, countryCode: string) => 
    api.delete(`/admin/users/${userId}/countries/${countryCode}`),
  getUserCountries: (userId: string) => api.get(`/admin/users/${userId}/countries`),
  
  getChannels: () => api.get('/admin/channels'),
  createChannel: (data: any) => api.post('/admin/channels', data),
  updateChannel: (accountId: string, data: any) => api.put(`/admin/channels/${accountId}`, data),
  deleteChannel: (accountId: string, hard?: boolean) => 
    api.delete(`/admin/channels/${accountId}`, { params: { hard } }),
  enableChannel: (accountId: string) => api.post(`/admin/channels/${accountId}/enable`),
  disableChannel: (accountId: string) => api.post(`/admin/channels/${accountId}/disable`),
  updateChannelTps: (accountId: string, data: { maxTps?: number; maxSessions?: number }) => 
    api.put(`/admin/channels/${accountId}/tps`, data),
  getChannelStats: (accountId: string) => api.get(`/admin/channels/${accountId}/stats`),
  getAllChannelsStats: () => api.get('/admin/channels/stats'),
  
  addChannelSession: (accountId: string) => api.post(`/admin/channels/${accountId}/sessions/add`),
  removeChannelSession: (accountId: string) => api.post(`/admin/channels/${accountId}/sessions/remove`),
  getChannelSessions: (accountId: string) => api.get(`/admin/channels/${accountId}/sessions`),
  
  batchEnableChannels: (accountIds: string[]) => 
    api.post('/admin/channels/batch/enable', { accountIds }),
  batchDisableChannels: (accountIds: string[]) => 
    api.post('/admin/channels/batch/disable', { accountIds }),
  batchUpdateTps: (updates: Array<{ accountId: string; maxTps?: number; maxSessions?: number }>) =>
    api.put('/admin/channels/batch/tps', { updates }),
  batchEnableUsers: (userIds: string[]) => 
    api.post('/admin/users/batch/enable', { userIds }),
  batchDisableUsers: (userIds: string[]) => 
    api.post('/admin/users/batch/disable', { userIds }),
  batchAssignCountries: (userIds: string[], countryCodes: string[]) =>
    api.post('/admin/users/batch/countries', { userIds, countryCodes }),
  batchAssignChannels: (userIds: string[], channels: Array<{ accountId: string; maxTps: number }>) =>
    api.post('/admin/users/batch/channels', { userIds, channels }),
  
  getAlerts: (params?: { accountId?: string; unresolvedOnly?: boolean; limit?: number }) => 
    api.get('/admin/alerts', { params }),
  resolveAlert: (alertId: string) => api.post(`/admin/alerts/${alertId}/resolve`),
  resolveChannelAlerts: (accountId: string, alertType: string) => 
    api.post(`/admin/channels/${accountId}/alerts/resolve`, { alertType }),
  
  getSystemHealth: () => api.get('/admin/health'),
  
  getWebhookConfig: () => api.get('/admin/webhook/config'),
  configureWebhook: (data: { url: string; headers?: Record<string, string>; enabled?: boolean }) => 
    api.post('/admin/webhook/config', data),
  enableWebhook: () => api.post('/admin/webhook/enable'),
  disableWebhook: () => api.post('/admin/webhook/disable')
}
