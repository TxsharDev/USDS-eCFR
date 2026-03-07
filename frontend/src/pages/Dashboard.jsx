import { useState, useEffect } from 'react'
import { api } from '../api'
import MetricCard from '../components/MetricCard'
import StatusBadge from '../components/StatusBadge'
import WordCountChart from '../components/WordCountChart'
import BurdenChart from '../components/BurdenChart'
import ChecksumTable from '../components/ChecksumTable'

// Main dashboard. All 4 required metrics visible: word count, changes, checksums, + RBI.
export default function Dashboard() {
    const [health, setHealth] = useState(null)
    const [wordCounts, setWordCounts] = useState([])
    const [burdenIndex, setBurdenIndex] = useState([])
    const [checksums, setChecksums] = useState([])
    const [loading, setLoading] = useState(true)

    useEffect(() => {
        // Fetch all dashboard data in parallel
        Promise.all([
            api.health(),
            api.wordCounts(15),
            api.burdenIndex(15),
            api.checksums(),
        ]).then(([h, wc, bi, cs]) => {
            setHealth(h)
            setWordCounts(wc)
            setBurdenIndex(bi)
            setChecksums(cs)
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

            {/* Full checksums list with copy buttons for verification */}
            <ChecksumTable data={checksums} />
        </div>
    )
}