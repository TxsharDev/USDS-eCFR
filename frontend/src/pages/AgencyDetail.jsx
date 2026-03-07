import { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { api } from '../api'
import MetricCard from '../components/MetricCard'
import ChangeTimeline from '../components/ChangeTimeline'

// Single agency deep-dive page. Shows metrics, change timeline, and checksum.
// Reached by clicking an agency row in the heatmap or a chart bar.
export default function AgencyDetail() {
  const { slug } = useParams()
  const [agency, setAgency] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    api.agency(slug)
      .then(setAgency)
      .catch(e => setError(e.message))
      .finally(() => setLoading(false))
  }, [slug])

  if (loading) return <p className="text-slate-400 p-8">Loading agency data...</p>
  if (error) return <p className="text-red-400 p-8">Error: {error}</p>
  if (!agency) return <p className="text-slate-400 p-8">Agency not found.</p>

  return (
    <div className="space-y-8">
      {/* Agency header */}
      <div>
        <h2 className="text-2xl font-bold text-white">{agency.name}</h2>
        {agency.shortName && (
          <p className="text-sm text-slate-400 mt-1">{agency.shortName}</p>
        )}
      </div>

      {/* Key metrics for this agency */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <MetricCard
          label="Word Count"
          value={agency.wordCount ? `${(agency.wordCount / 1e6).toFixed(1)}M` : '0'}
          sub="Total regulation words"
        />
        <MetricCard
          label="RBI Score"
          value={agency.rbiScore?.toLocaleString() || '0'}
          sub="Regulatory Burden Index"
        />
        <MetricCard
          label="Complexity"
          value={agency.complexity?.toFixed(1) || '0'}
          sub="Avg sentence length"
        />
        <MetricCard
          label="Total Changes"
          value={agency.totalChanges?.toLocaleString() || '0'}
          sub="All-time version history"
        />
      </div>

      {/* Change timeline chart */}
      <ChangeTimeline data={agency.monthlyChanges} />

      {/* Checksum for content verification */}
      {agency.checksum && (
        <div className="bg-slate-800 rounded-lg p-5 border border-slate-700">
          <h3 className="text-sm font-semibold text-slate-300 mb-2">Content Checksum</h3>
          <code className="text-sm font-mono text-slate-400 break-all">{agency.checksum}</code>
        </div>
      )}
    </div>
  )
}
