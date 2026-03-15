import { defineStore } from 'pinia'
import { ref } from 'vue'
import { api } from '../services/api'

export const useFeedbackStore = defineStore('feedback', () => {
  const feedbackQueue = ref([])
  const stats = ref({})
  const loading = ref(false)
  const error = ref(null)

  async function loadFeedbackQueue() {
    loading.value = true
    error.value = null
    try {
      feedbackQueue.value = await api.getFeedbackItems()
    } catch (err) {
      error.value = err.message
      throw err
    } finally {
      loading.value = false
    }
  }

  async function submitFeedback(feedbackData) {
    loading.value = true
    error.value = null
    try {
      return await api.submitFeedback(feedbackData)
    } catch (err) {
      error.value = err.message
      throw err
    } finally {
      loading.value = false
    }
  }

  async function markActioned(id) {
    loading.value = true
    error.value = null
    try {
      await api.markActioned(id)
      const item = feedbackQueue.value.find(i => i.id === id)
      if (item) item.status = 'actioned'
    } catch (err) {
      error.value = err.message
      throw err
    } finally {
      loading.value = false
    }
  }

  async function markDismissed(id) {
    loading.value = true
    error.value = null
    try {
      await api.markDismissed(id)
      const item = feedbackQueue.value.find(i => i.id === id)
      if (item) item.status = 'dismissed'
    } catch (err) {
      error.value = err.message
      throw err
    } finally {
      loading.value = false
    }
  }

  async function loadStats() {
    loading.value = true
    error.value = null
    try {
      stats.value = await api.getStats()
    } catch (err) {
      error.value = err.message
      throw err
    } finally {
      loading.value = false
    }
  }

  return {
    feedbackQueue,
    stats,
    loading,
    error,
    loadFeedbackQueue,
    submitFeedback,
    markActioned,
    markDismissed,
    loadStats
  }
})
