import { useState } from 'react'
import HomePage from './pages/HomePage'
import RunHistoryPage from './pages/RunHistoryPage'
import './App.css'

type Page = 'home' | 'history'

function App() {
  const [page, setPage] = useState<Page>('home')

  return (
    <div className="app-shell">
      <nav className="app-nav">
        <button
          className={`nav-link ${page === 'home' ? 'active' : ''}`}
          onClick={() => setPage('home')}
        >
          Run Scenario
        </button>
        <button
          className={`nav-link ${page === 'history' ? 'active' : ''}`}
          onClick={() => setPage('history')}
        >
          Run History
        </button>
      </nav>
      {page === 'home' && <HomePage />}
      {page === 'history' && <RunHistoryPage />}
    </div>
  )
}

export default App
