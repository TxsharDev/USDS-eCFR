import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts'

// Line chart showing an agency's change volume over time (monthly).
// Used on the agency detail page to visualize regulatory activity trends.
export default function ChangeTimeline({ data }) {
  if (!data?.length) return <p className="text-slate-500 text-sm">No change history available.</p>

  const formatted = data.map(d => ({
    month: d.month,
    // Short label for x-axis like "Mar"
    label: new Date(d.month + '-01').toLocaleDateString('en-US', { month: 'short' }),
    count: d.count,
  }))

  return (
    <div className="bg-slate-800 rounded-lg p-5 border border-slate-700">
      <h3 className="text-sm font-semibold text-slate-300 mb-4">Changes Over Time (Last 12 Months)</h3>
      <ResponsiveContainer width="100%" height={250}>
        <LineChart data={formatted} margin={{ left: 10, right: 10 }}>
          <XAxis
            dataKey="label"
            stroke="#64748b"
            fontSize={11}
            tick={{ fill: '#94a3b8' }}
          />
          <YAxis stroke="#64748b" fontSize={11} />
          <Tooltip
            contentStyle={{ background: '#1e293b', border: '1px solid #334155', borderRadius: 8 }}
            labelStyle={{ color: '#e2e8f0' }}
            formatter={v => [v + ' changes', 'Count']}
          />
          <Line
            type="monotone"
            dataKey="count"
            stroke="#3b82f6"
            strokeWidth={2}
            dot={{ fill: '#3b82f6', r: 3 }}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  )
}
