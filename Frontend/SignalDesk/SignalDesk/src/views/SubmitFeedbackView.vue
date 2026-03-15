<template>
  <div class="submit-view">
    <h1>Submit Feedback</h1>

    <div class="form-card">
      <form @submit.prevent="handleSubmit">
        <div class="form-group">
          <label for="feedback">Your Feedback</label>
          <textarea
            id="feedback"
            v-model="text"
            rows="6"
            placeholder="Describe the issue or suggestion..."
            :disabled="loading"
            required
          ></textarea>
        </div>

        <button type="submit" class="btn-submit" :disabled="loading || !text.trim()">
          {{ loading ? 'Submitting…' : 'Submit Feedback' }}
        </button>
      </form>

      <p v-if="submitted" class="msg-success">Feedback submitted successfully.</p>
      <p v-if="errorMsg" class="msg-error">{{ errorMsg }}</p>
    </div>
  </div>
</template>

<script setup>
import { ref, onUnmounted } from 'vue'
import { useFeedbackStore } from '../stores/feedback'

const store = useFeedbackStore()

const text = ref('')
const loading = ref(false)
const submitted = ref(false)
const errorMsg = ref('')

let successTimer = null

async function handleSubmit() {
  submitted.value = false
  errorMsg.value = ''
  loading.value = true

  try {
    await store.submitFeedback({ text: text.value.trim() })
    text.value = ''
    submitted.value = true
    successTimer = setTimeout(() => { submitted.value = false }, 4000)
  } catch (err) {
    errorMsg.value = err.message ?? 'Submission failed. Please try again.'
  } finally {
    loading.value = false
  }
}

onUnmounted(() => clearTimeout(successTimer))
</script>

<style scoped>
.submit-view {
  max-width: 560px;
  margin: 0 auto;
}

h1 {
  margin-bottom: 1.5rem;
  color: #2c3e50;
  font-size: 1.5rem;
}

.form-card {
  background: white;
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.1);
}

.form-group {
  margin-bottom: 1.25rem;
}

label {
  display: block;
  margin-bottom: 0.4rem;
  font-weight: 500;
  font-size: 0.9rem;
  color: #2c3e50;
}

textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.95rem;
  resize: vertical;
  transition: border-color 0.2s;
}

textarea:focus {
  outline: none;
  border-color: #3498db;
}

textarea:disabled {
  background: #f9f9f9;
  color: #999;
}

.btn-submit {
  background: #3498db;
  color: white;
  padding: 0.65rem 1.75rem;
  border: none;
  border-radius: 4px;
  font-size: 0.95rem;
  font-weight: 500;
  transition: background 0.2s;
}

.btn-submit:hover:not(:disabled) {
  background: #2980b9;
}

.btn-submit:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.msg-success,
.msg-error {
  margin-top: 1rem;
  padding: 0.75rem 1rem;
  border-radius: 4px;
  font-size: 0.9rem;
}

.msg-success {
  background: #eaf7ec;
  color: #276749;
}

.msg-error {
  background: #fdf0f0;
  color: #922;
}
</style>
