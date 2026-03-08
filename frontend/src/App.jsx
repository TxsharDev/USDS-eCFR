import { Routes, Route, Link, useLocation } from 'react-router-dom'
import Dashboard from './pages/Dashboard'
import AgencyDetail from './pages/AgencyDetail'

function App() {
  const isDetail = useLocation().pathname.startsWith('/agency/')
  return (
    <div className="min-h-screen">
      <header className="bg-slate-800 border-b border-slate-700 px-6 py-4">
        <div className="max-w-7xl mx-auto flex items-center justify-between">
          <Link to="/" className="flex items-center gap-3 no-underline">
            <img src="/usds-logo.svg" alt="USDS" className="h-8 w-8" />
            <div>
              <h1 className="text-lg font-bold text-white leading-tight">eCFR Regulatory Analyzer</h1>
              <p className="text-xs text-slate-400">U.S. Digital Service</p>
            </div>
          </Link>
          {isDetail && <Link to="/" className="text-sm text-blue-400 hover:text-blue-300">Back to Dashboard</Link>}
        </div>
      </header>
      <main className="max-w-7xl mx-auto px-6 py-8">
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/agency/:slug" element={<AgencyDetail />} />
        </Routes>
      </main>
    </div>
  )
}

export default App
