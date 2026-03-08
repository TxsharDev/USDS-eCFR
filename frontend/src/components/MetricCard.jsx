export default function MetricCard({ label, value, sub }) {
  return (
    <div className="bg-slate-800 rounded-lg p-5 border border-slate-700">
      <p className="text-sm text-slate-400 mb-1">{label}</p>
      <p className="text-2xl font-bold text-white">{value}</p>
      {sub && <p className="text-xs text-slate-500 mt-1">{sub}</p>}
    </div>
  )
}
