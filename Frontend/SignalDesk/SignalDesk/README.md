# SignalDesk Frontend

Minimal Vue 3 Composition API frontend for the SignalDesk internal SaaS tool.

## Tech Stack

- Vue 3 with Composition API
- Vue Router for navigation
- Pinia for state management
- Vite for build tooling

## Features

### 1. Submit Feedback View (`/`)
- Simple textarea for entering feedback
- Submit button to send feedback to backend

### 2. Queue View (`/queue`)
- Lists all feedback items
- Shows: summary, category, status, priority
- Actions: Mark as Actioned or Dismissed

### 3. Dashboard View (`/dashboard`)
- Displays category counts
- Shows statistics by status and priority
- Total feedback count

## Getting Started

1. **Install dependencies:**
   ```bash
   npm install
   ```

2. **Run development server:**
   ```bash
   npm run dev
   ```

3. **Build for production:**
   ```bash
   npm run build
   ```

## Backend API

The frontend expects a backend API running at `http://localhost:5000` with the following endpoints:

- `POST /api/feedback` - Submit new feedback
- `GET /api/feedback/queue` - Get all feedback items
- `PATCH /api/feedback/:id/status` - Update feedback status
- `GET /api/feedback/stats` - Get dashboard statistics

## Project Structure

```
src/
├── assets/          # Global styles
├── components/      # Reusable components (NavigationBar)
├── router/          # Vue Router configuration
├── services/        # API service layer
├── stores/          # Pinia stores
├── views/           # Page components
├── App.vue          # Root component
└── main.js          # App entry point
```

## Development

The app will run on the port configured in `vite.config.js` (currently 61814).

Access the app at: `http://localhost:61814`
