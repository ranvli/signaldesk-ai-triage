<template>
  <div class="dashboard-view">
    <h1>Dashboard</h1>

    <p v-if="loading" class="loading">Loading…</p>
    <p v-if="errorMsg" class="msg-error">{{ errorMsg }}</p>

    <template v-if="!loading && hasData">

      <!-- Summary tiles -->
      <div class="tile-row">
        <div class="tile">
          <span class="tile-value">{{ stats.total ?? 0 }}</span>
          <span class="tile-label">Total</span>
        </div>
        <div v-for="t in statusTiles" :key="t.label" class="tile">
          <span class="tile-value" :style="{ color: t.color }">{{ t.value }}</span>
          <span class="tile-label">{{ t.label }}</span>
        </div>
      </div>

      <!-- By Category -->
      <section class="panel">
        <h2>By Category</h2>
        <div class="bar-list">
          <div v-for="row in categoryRows" :key="row.key" class="bar-row">
            <span class="bar-label">{{ row.label }}</span>
            <div class="bar-track">
              <div
                class="bar-fill"
                :style="{ width: row.pct + '%', background: row.color }"
              ></div>
            </div>
            <span class="bar-count">{{ row.count }}</span>
          </div>
        </div>
      </section>

      <!-- By Priority (only when backend returns data) -->
      <section v-if="priorityRows.length" class="panel">
        <h2>By Priority</h2>
        <div class="bar-list">
          <div v-for="row in priorityRows" :key="row.key" class="bar-row">
            <span class="bar-label">{{ row.label }}</span>
            <div class="bar-track">
              <div
                class="bar-fill"
                :style="{ width: row.pct + '%', background: row.color }"
              ></div>
            </div>
            <span class="bar-count">{{ row.count }}</span>
          </div>
        </div>
      </section>

    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useFeedbackStore } from '../stores/feedback'

const store = useFeedbackStore()
const { stats } = storeToRefs(store)

const loading = ref(false)
const errorMsg = ref('')

// ── Fixed definitions ──────────────────────────────────

const CATEGORIES = [
  { key: 'bug',             label: 'Bug',            color: '#e74c3c' },
  { key: 'feature_request', label: 'Feature Request', color: '#3498db' },
  { key: 'complaint',       label: 'Complaint',       color: '#e67e22' },
  { key: 'praise',          label: 'Praise',          color: '#27ae60' },
]

const PRIORITIES = [
  { key: 'high',   label: 'High',   color: '#e74c3c' },
  { key: 'medium', label: 'Medium', color: '#f39c12' },
  { key: 'low',    label: 'Low',    color: '#27ae60' },
]

// ── Computed ─────────────────────────────────────

const hasData = computed(() => Object.keys(stats.value).length > 0)

const statusTiles = computed(() => {
  const s = stats.value.statuses ?? {}
  return [
    { label: 'Pending',   value: s.pending   ?? 0, color: '#3498db' },
    { label: 'Actioned',  value: s.actioned  ?? 0, color: '#27ae60' },
    { label: 'Dismissed', value: s.dismissed ?? 0, color: '#95a5a6' },
  ]
})

function toBarRows(definitions, source) {
  const rows = definitions.map(d => ({ ...d, count: source[d.key] ?? 0 }))
  const max = Math.max(...rows.map(r => r.count), 1)
  return rows.map(r => ({ ...r, pct: Math.round((r.count / max) * 100) }))
}

const categoryRows = computed(() =>
  toBarRows(CATEGORIES, stats.value.categories ?? {})
)

const priorityRows = computed(() => {
  const src = stats.value.priorities
  if (!src || !Object.values(src).some(v => v > 0)) return []
  return toBarRows(PRIORITIES, src)
})

// ── Lifecycle ──────────────────────────────────

onMounted(async () => {
  loading.value = true
  errorMsg.value = ''
  try {
    await store.loadStats()
  } catch (err) {
    errorMsg.value = err.message ?? 'Failed to load stats.'
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.dashboard-view {
  max-width: 760px;
  margin: 0 auto;
}

h1 {
  margin-bottom: 1.5rem;
  color: #2c3e50;
  font-size: 1.5rem;
}

h2 {
  margin-bottom: 1.25rem;
  color: #2c3e50;
  font-size: 1rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.loading {
  padding: 2.5rem;
  text-align: center;
  color: #888;
}

.msg-error {
  padding: 0.75rem 1rem;
  background: #fdf0f0;
  color: #922;
  border-radius: 4px;
  font-size: 0.9rem;
  margin-bottom: 1rem;
}

/* ── Summary tiles ────────────────────────────── */

.tile-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.tile {
  background: white;
  border-radius: 8px;
  padding: 1.25rem 1.5rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}

.tile-value {
  font-size: 2rem;
  font-weight: 700;
  color: #2c3e50;
  line-height: 1;
}

.tile-label {
  font-size: 0.78rem;
  color: #999;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

/* ── Panels ─────────────────────────────────── */

.panel {
  background: white;
  border-radius: 8px;
  padding: 1.5rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
  margin-bottom: 1.25rem;
}

/* ── Bar rows ───────────────────────────────── */

.bar-list {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.bar-row {
  display: grid;
  grid-template-columns: 130px 1fr 2rem;
  align-items: center;
  gap: 0.75rem;
}

.bar-label {
  font-size: 0.88rem;
  color: #444;
  white-space: nowrap;
}

.bar-track {
  height: 10px;
  background: #f0f0f0;
  border-radius: 5px;
  overflow: hidden;
}

.bar-fill {
  height: 100%;
  border-radius: 5px;
  transition: width 0.4s ease;
  min-width: 2px;
}

.bar-count {
  font-size: 0.88rem;
  font-weight: 600;
  color: #555;
  text-align: right;
}
</style>
