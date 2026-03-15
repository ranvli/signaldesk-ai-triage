const BASE_URL = 'http://localhost:5000'

async function request(endpoint, options = {}) {
  const response = await fetch(`${BASE_URL}${endpoint}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options
  })

  if (!response.ok) {
    throw new Error(`${options.method ?? 'GET'} ${endpoint} failed — ${response.status}`)
  }

  return response.json()
}

export const api = {
  submitFeedback(payload) {
    return request('/api/feedback', {
      method: 'POST',
      body: JSON.stringify(payload)
    })
  },

  getFeedbackItems() {
    return request('/api/feedback')
  },

  markActioned(id) {
    return request(`/api/feedback/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status: 'actioned' })
    })
  },

  markDismissed(id) {
    return request(`/api/feedback/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status: 'dismissed' })
    })
  },

  getStats() {
    return request('/api/feedback/stats')
  }
}
