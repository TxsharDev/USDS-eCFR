import { useNavigate } from 'react-router-dom'

export default function Heatmap({ data }) {
  const navigate = useNavigate()
  if (!data?.agencies?.length) return null
  const allMonths = [...new Set(data.agencies.flatMap(a => a.months.map(m => m.month)))].sort()
  const allCounts = data.agencies.flatMap(a => a.months.map(m => m.count))
  const maxCount = Math.max(...allCounts, 1)
  const getColor = (count) => {
    if (count === 0) return 'bg-slate-700'
    const ratio = count / maxCount
    if (ratio > 0.75) return 'bg-green-500'
    if (ratio > 0.5) return 'bg-green-600'
    if (ratio > 0.25) return 'bg-green-700'
    return 'bg-green-900'
  }
  return (
    <div className="bg-slate-800 rounded-lg p-5 border border-slate-700">
      <h3 className="text-sm font-semibold text-slate-300 mb-1">Change Activity Heatmap</h3>
      <p className="text-xs text-slate-500 mb-4">Top 15 most active agencies over 12 months. Click a row to view details.</p>
      <div className="overflow-x-auto">
        <table className="text-xs">
          <thead>
            <tr>
              <th className="text-left text-slate-400 pr-3 pb-2 font-normal">Agency</th>
              {allMonths.map(m => (
                <th key={m} className="text-slate-500 font-normal px-0.5 pb-2 min-w-[28px]">
                  {new Date(m + '-01').toLocaleDateString('en-US', { month: 'short' })}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.agencies.map(agency => {
              const countMap = Object.fromEntries(agency.months.map(m => [m.month, m.count]))
              return (
                <tr key={agency.slug} className="cursor-pointer hover:bg-slate-700/50"
                    onClick={() => navigate(`/agency/${agency.slug}`)}>
                  <td className="text-slate-300 pr-3 py-1 whitespace-nowrap">{agency.name}</td>
                  {allMonths.map(m => {
                    const count = countMap[m] || 0
                    return (
                      <td key={m} className="px-0.5 py-1">
                        <div className={`w-6 h-6 rounded-sm ${getColor(count)}`}
                             title={`${agency.name}: ${count} changes in ${m}`} />
                      </td>
                    )
                  })}
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
      <div className="flex items-center gap-2 mt-4 text-xs text-slate-500">
        <span>Less</span>
        <div className="w-4 h-4 rounded-sm bg-slate-700" />
        <div className="w-4 h-4 rounded-sm bg-green-900" />
        <div className="w-4 h-4 rounded-sm bg-green-700" />
        <div className="w-4 h-4 rounded-sm bg-green-600" />
        <div className="w-4 h-4 rounded-sm bg-green-500" />
        <span>More</span>
      </div>
    </div>
  )
}
