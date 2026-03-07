import { useState, useEffect } from 'react'
import { api } from '../api'
import MetricCard from '../components/MetricCard'
import StatusBadge from '../components/StatusBadge'
import WordCountChart from '../components/WordCountChart'

// Main dashboard. Loads health and word counts on mount, more charts added in next commits.
export default function Dashboard() {
  const [health, setHealth] = useState(null)
  const [wordCounts, setWordCounts] = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    // Fetch all dashboard data in parallel
    Promise.all([
      api.health(),
      api.wordCounts(15),
    ]).then(([h, wc]) => {
      setHealth(h)
      setWordCounts(wc)
    }).finally(() => setLoading(false))
  }, [])

  if (loading) return <p className="text-slate-400 p-8">Loading dashboard...</p>

  return (
    <div className="space-y-8">
      {/* Status bar */}
      <div className="flex items-center justify-between">
        <StatusBadge health={health} />
        {health?.lastSync && (
          <span className="text-xs text-slate-500">
            Last sync: {new Date(health.lastSync).toLocaleString()}
          </span>
        )}
      </div>

      {/* Key stats at a glance. 4 columns on desktop, 2 on mobile */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <MetricCard
          label="Agencies"
          value={health?.agencyCount?.toLocaleString()}
          sub="Total tracked"
        />
        <MetricCard
          label="Changes"
          value={health?.changeCount?.toLocaleString()}
          sub="Version history records"
        />
        <MetricCard
          label="Top Word Count"
          value={wordCounts[0] ? `${(wordCounts[0].wordCount / 1e6).toFixed(1)}M` : '—'}
          sub={wordCounts[0]?.shortName || wordCounts[0]?.name}
        />
        <MetricCard
          label="API Status"
          value={health?.status === 'healthy' ? 'Online' : 'Offline'}
          sub={`v${health?.version}`}
        />
      </div>

      {/* Word count chart */}
      <WordCountChart data={wordCounts} />
    </div>
  )
}
