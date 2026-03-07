import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Cell } from 'recharts'

// RBI (Regulatory Burden Index) bar chart. Color-coded by score intensity.
// Tooltip breaks down the score components so users understand what drives the number.
export default function BurdenChart({ data }) {
    if (!data?.length) return null

    const formatted = data.map(d => ({
        name: d.shortName || d.name,
        rbi: d.rbiScore,
        words: d.wordCount,
        complexity: d.complexity,
    }))

    // Color bars by RBI intensity: higher = more red
    const getBarColor = (rbi) => {
        if (rbi > 2000) return '#ef4444'
        if (rbi > 1000) return '#f97316'
        if (rbi > 500) return '#eab308'
        return '#22c55e'
    }

    return (
        <div className="bg-slate-800 rounded-lg p-5 border border-slate-700">
            <h3 className="text-sm font-semibold text-slate-300 mb-1">Regulatory Burden Index (Top 15)</h3>
            <p className="text-xs text-slate-500 mb-4">
                RBI = (words/1000) x (avg sentence length/15) x log2(staleness+1) / 100
            </p>
            <ResponsiveContainer width="100%" height={400}>
                <BarChart data={formatted} layout="vertical" margin={{ left: 60, right: 20 }}>
                    <XAxis type="number" stroke="#64748b" fontSize={12} />
                    <YAxis
                        type="category"
                        dataKey="name"
                        width={55}
                        stroke="#64748b"
                        fontSize={11}
                        tick={{ fill: '#94a3b8' }}
                    />
                    <Tooltip
                        content={({ active, payload }) => {
                            if (!active || !payload?.length) return null
                            const d = payload[0].payload
                            return (
                                <div className="bg-slate-900 border border-slate-600 rounded-lg p-3 text-sm">
                                    <p className="text-white font-semibold mb-1">{d.name}</p>
                                    <p className="text-slate-300">RBI Score: <span className="text-white">{d.rbi}</span></p>
                                    <p className="text-slate-400">Words: {d.words?.toLocaleString()}</p>
                                    <p className="text-slate-400">Avg Sentence Length: {d.complexity}</p>
                                </div>
                            )
                        }}
                    />
                    {/* Each bar colored by its RBI score using Recharts Cell pattern */}
                    <Bar dataKey="rbi" radius={[0, 4, 4, 0]}>
                        {formatted.map((entry, i) => (
                            <Cell key={i} fill={getBarColor(entry.rbi)} />
                        ))}
                    </Bar>
                </BarChart>
            </ResponsiveContainer>
        </div>
    )
}