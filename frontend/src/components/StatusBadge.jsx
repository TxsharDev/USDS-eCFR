// Health indicator dot with label. Green = backend healthy, red = down.
export default function StatusBadge({ health }) {
  const ok = health?.status === 'healthy'
  return (
    <div className="flex items-center gap-2">
      <div className={`h-2.5 w-2.5 rounded-full ${ok ? 'bg-green-500' : 'bg-red-500'}`} />
      <span className="text-sm text-slate-400">
        {ok ? 'System Healthy' : 'System Down'}
      </span>
    </div>
  )
}
