const BASE_URL = 'http://localhost:5180'

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
    return request('/feedback', {
      method: 'POST',
      body: JSON.stringify(payload)
    })
  },

  getFeedbackItems() {
    return request('/feedback')
  },

  markActioned(id) {
    return request(`/feedback/${id}/action`, {
      method: 'PATCH'
    })
  },

  markDismissed(id) {
    return request(`/feedback/${id}/dismiss`, {
      method: 'PATCH'
    })
  },

  getStats() {
    return request('/stats')
  }
}
