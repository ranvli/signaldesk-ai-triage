<template>
  <div class="queue-view">
    <h1>Feedback Queue</h1>

    <p v-if="store.loading && !feedbackQueue.length" class="loading">Loading…</p>
    <p v-if="errorMsg" class="msg-error">{{ errorMsg }}</p>
    <p v-if="!store.loading && !feedbackQueue.length && !errorMsg" class="empty-state">
      No feedback items yet.
    </p>

    <ul v-if="feedbackQueue.length" class="item-list">
      <li
        v-for="item in feedbackQueue"
        :key="item.id"
        class="item-card"
        :class="item.status.toLowerCase()"
      >
        <div class="item-meta">
          <div class="item-badges">
            <span class="badge category">{{ item.category }}</span>
            <span class="badge status" :class="item.status.toLowerCase()">{{ item.status }}</span>
            <span class="badge priority" :class="item.priority.toLowerCase()">{{ item.priority }}</span>
          </div>
          <time class="created-at">{{ formatDate(item.createdAt) }}</time>
        </div>

        <p class="summary">{{ item.summary }}</p>

        <button v-if="item.text" class="btn-toggle" @click="toggleText(item.id)">
          {{ expandedIds[item.id] ? 'Hide full feedback' : 'View full feedback' }}
        </button>
        <p v-if="expandedIds[item.id]" class="full-text">{{ item.text }}</p>

        <div v-if="item.status !== 'Actioned' && item.status !== 'Dismissed'" class="item-actions">
          <button
            class="btn-action"
            :disabled="pendingId === item.id"
            @click="handleAction(store.markActioned, item.id)"
          >
            {{ pendingId === item.id ? '…' : 'Mark Actioned' }}
          </button>
          <button
            class="btn-dismiss"
            :disabled="pendingId === item.id"
            @click="handleAction(store.markDismissed, item.id)"
          >
            Dismiss
          </button>
        </div>
      </li>
    </ul>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useFeedbackStore } from '../stores/feedback'

const store = useFeedbackStore()
const { feedbackQueue } = storeToRefs(store)

const pendingId = ref(null)
const errorMsg = ref('')
const expandedIds = ref({})

function toggleText(id) {
  expandedIds.value[id] = !expandedIds.value[id]
}

onMounted(() => store.loadFeedbackQueue())

async function handleAction(actionFn, id) {
  pendingId.value = id
  errorMsg.value = ''
  try {
    await actionFn(id)
  } catch (err) {
    errorMsg.value = err.message ?? 'Action failed. Please try again.'
  } finally {
    pendingId.value = null
  }
}

function formatDate(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString(undefined, {
    year: 'numeric', month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit'
  })
}
</script>

<style scoped>
.queue-view {
  max-width: 860px;
  margin: 0 auto;
}

h1 {
  margin-bottom: 1.5rem;
  color: #2c3e50;
  font-size: 1.5rem;
}

.loading,
.empty-state {
  padding: 2.5rem;
  text-align: center;
  color: #888;
  background: white;
  border-radius: 8px;
}

.msg-error {
  padding: 0.75rem 1rem;
  margin-bottom: 1rem;
  background: #fdf0f0;
  color: #922;
  border-radius: 4px;
  font-size: 0.9rem;
}

/* ── List ─────────────────────────────────────────── */

.item-list {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

/* ── Card ─────────────────────────────────────────── */

.item-card {
  background: white;
  border-radius: 8px;
  padding: 1.25rem 1.5rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
  border-left: 4px solid transparent;
  transition: opacity 0.2s;
}

.item-card.open {
  border-left-color: #3498db;
}

.item-card.actioned {
  border-left-color: #27ae60;
  opacity: 0.65;
}

.item-card.dismissed {
  border-left-color: #bdc3c7;
  opacity: 0.5;
}

/* ── Meta row ─────────────────────────────────────── */

.item-meta {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
  flex-wrap: wrap;
}

.item-badges {
  display: flex;
  gap: 0.4rem;
  flex-wrap: wrap;
}

.created-at {
  font-size: 0.8rem;
  color: #999;
  white-space: nowrap;
}

/* ── Badges ───────────────────────────────────────── */

.badge {
  padding: 0.2rem 0.6rem;
  border-radius: 10px;
  font-size: 0.78rem;
  font-weight: 600;
  text-transform: capitalize;
}

.badge.category {
  background: #e8f4fd;
  color: #1a6fa8;
}

/* status */
.badge.status.open      { background: #fff4e0; color: #b86e00; }
.badge.status.actioned  { background: #e6f9ee; color: #1a7a40; }
.badge.status.dismissed { background: #f0f0f0; color: #777;    }

/* priority */
.badge.priority.high   { background: #fde8e8; color: #a82020; }
.badge.priority.medium { background: #fff4e0; color: #b86e00; }
.badge.priority.low    { background: #edf7ed; color: #2e7d32; }

/* ── Summary ──────────────────────────────────────── */

.summary {
  font-size: 0.95rem;
  color: #333;
  line-height: 1.55;
  margin-bottom: 0.5rem;
}

.btn-toggle {
  background: none;
  border: none;
  color: #3498db;
  font-size: 0.8rem;
  padding: 0;
  margin-bottom: 0.75rem;
  cursor: pointer;
  display: block;
}

.btn-toggle:hover {
  text-decoration: underline;
}

.full-text {
  font-size: 0.88rem;
  color: #555;
  line-height: 1.6;
  background: #f8f9fa;
  border-left: 3px solid #ddd;
  padding: 0.6rem 0.75rem;
  border-radius: 0 4px 4px 0;
  margin-bottom: 0.75rem;
  white-space: pre-wrap;
}

.item-card.dismissed .summary {
  text-decoration: line-through;
  color: #aaa;
}

/* ── Actions ──────────────────────────────────────── */

.item-actions {
  display: flex;
  gap: 0.5rem;
}

.btn-action,
.btn-dismiss {
  padding: 0.4rem 1rem;
  border: none;
  border-radius: 4px;
  font-size: 0.85rem;
  font-weight: 500;
  transition: background 0.2s;
  min-width: 6rem;
}

.btn-action {
  background: #27ae60;
  color: white;
}

.btn-action:hover:not(:disabled) {
  background: #219150;
}

.btn-dismiss {
  background: #f0f0f0;
  color: #555;
}

.btn-dismiss:hover:not(:disabled) {
  background: #e0e0e0;
}

.btn-action:disabled,
.btn-dismiss:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
