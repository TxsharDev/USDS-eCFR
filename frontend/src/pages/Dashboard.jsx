import { useState, useEffect } from 'react'
import { api } from '../api'

// Main dashboard page. Fetches health on mount, charts will be added in next commits.
export default function Dashboard() {
  const [health, setHealth] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    api.health().then(setHealth).finally(() => setLoading(false))
  }, [])

  if (loading) return <p className="text-slate-400">Loading...</p>

  return (
    <div>
      <div className="flex items-center gap-3 mb-8">
        {/* Green dot = backend is up, red = something's wrong */}
        <div className={`h-3 w-3 rounded-full ${health?.status === 'healthy' ? 'bg-green-500' : 'bg-red-500'}`} />
        <span className="text-sm text-slate-400">
          {health?.agencyCount} agencies | {health?.changeCount?.toLocaleString()} changes
          {health?.lastSync && ` | Last sync: ${new Date(health.lastSync).toLocaleString()}`}
        </span>
      </div>
      <p className="text-slate-500">Dashboard charts coming next...</p>
    </div>
  )
}
