import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts'

export default function WordCountChart({ data }) {
  if (!data?.length) return null
  const formatted = data.map(d => ({ name: d.shortName || d.name, words: d.wordCount }))
  return (
    <div className="bg-slate-800 rounded-lg p-5 border border-slate-700">
      <h3 className="text-sm font-semibold text-slate-300 mb-4">Word Count by Agency (Top 15)</h3>
      <ResponsiveContainer width="100%" height={400}>
        <BarChart data={formatted} layout="vertical" margin={{ left: 60, right: 20 }}>
          <XAxis type="number" tickFormatter={v => `${(v / 1e6).toFixed(0)}M`} stroke="#64748b" fontSize={12} />
          <YAxis type="category" dataKey="name" width={55} stroke="#64748b" fontSize={11} tick={{ fill: '#94a3b8' }} />
          <Tooltip
            formatter={v => [v.toLocaleString() + ' words', 'Word Count']}
            contentStyle={{ background: '#1e293b', border: '1px solid #334155', borderRadius: 8 }}
            labelStyle={{ color: '#e2e8f0' }}
          />
          <Bar dataKey="words" fill="#3b82f6" radius={[0, 4, 4, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
