import { useState, useEffect } from 'react'
import { api } from '../api'
import MetricCard from '../components/MetricCard'
import StatusBadge from '../components/StatusBadge'
import WordCountChart from '../components/WordCountChart'
import BurdenChart from '../components/BurdenChart'
import ChecksumTable from '../components/ChecksumTable'
import Heatmap from '../components/Heatmap'

// Main dashboard. All 4 required metrics visible: word count, changes, checksums, + RBI.
export default function Dashboard() {
    const [health, setHealth] = useState(null)
    const [wordCounts, setWordCounts] = useState([])
    const [burdenIndex, setBurdenIndex] = useState([])
    const [checksums, setChecksums] = useState([])
    const [heatmap, setHeatmap] = useState(null)
    const [loading, setLoading] = useState(true)

    useEffect(() => {
        // Fetch all dashboard data in parallel
        Promise.all([
            api.health(),
            api.wordCounts(15),
            api.burdenIndex(15),
            api.checksums(),
            api.heatmap(12),
        ]).then(([h, wc, bi, cs, hm]) => {
            setHealth(h)
            setWordCounts(wc)
            setBurdenIndex(bi)
            setChecksums(cs)
            setHeatmap(hm)
        }).finally(() => setLoading(false))
    }, [])

    const [syncing, setSyncing] = useState(false)
    const [syncMsg, setSyncMsg] = useState(null)

    const handleSync = () => {
        setSyncing(true)
        setSyncMsg('Sync started. This runs in the background and may take several minutes.')
        api.triggerSync()
            .then(res => {
                setSyncing(false)
                setSyncMsg(res.status === 'running' ? 'Sync is already running in the background.' : 'Sync triggered successfully.')
                setTimeout(() => setSyncMsg(null), 8000)
            })
            .catch(() => {
                setSyncing(false)
                setSyncMsg('Failed to trigger sync.')
                setTimeout(() => setSyncMsg(null), 5000)
            })
    }

    if (loading) return <p className="text-slate-400 p-8">Loading dashboard...</p>

    return (
        <div className="space-y-8">
            {/* Status bar */}
            <div className="flex items-center justify-between">
                <StatusBadge health={health} />
                <div className="flex items-center gap-4">
                    {health?.lastSync && (
                        <span className="text-xs text-slate-500">
                            Last sync: {new Date(health.lastSync).toLocaleString()}
                        </span>
                    )}
                    <button
                        onClick={handleSync}
                        disabled={syncing}
                        className="text-xs px-3 py-1.5 bg-blue-600 hover:bg-blue-500 disabled:bg-slate-600 text-white rounded transition-colors"
                    >
                        {syncing ? 'Syncing...' : 'Sync Now'}
                    </button>
                </div>
            </div>
            {syncMsg && (
                <p className="text-xs text-blue-400 bg-blue-900/30 border border-blue-800 rounded px-3 py-2">{syncMsg}</p>
            )}

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
                    value={
                        wordCounts[0] ? (
                            <span title={wordCounts[0].wordCount.toLocaleString() + ' words'}>
                                {(wordCounts[0].wordCount / 1e6).toFixed(1)}M
                            </span>
                        ) : '—'
                    }
                    sub={wordCounts[0]?.shortName || wordCounts[0]?.name}
                />
                <MetricCard
                    label="Highest RBI"
                    value={burdenIndex[0]?.rbiScore?.toLocaleString() || '—'}
                    sub={burdenIndex[0]?.shortName || burdenIndex[0]?.name}
                />
            </div>

            {/* Word count and RBI charts side by side on large screens */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
                <WordCountChart data={wordCounts} />
                <BurdenChart data={burdenIndex} />
            </div>

            {/* GitHub-style heatmap showing change activity. Click a row to drill in. */}
            <Heatmap data={heatmap} />

            {/* Full checksums list with copy buttons for verification */}
            <ChecksumTable data={checksums} />
        </div>
    )
}