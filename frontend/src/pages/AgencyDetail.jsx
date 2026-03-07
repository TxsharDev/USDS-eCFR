import { useParams } from 'react-router-dom'

export default function AgencyDetail() {
  const { slug } = useParams()

  return (
    <div>
      <h2 className="text-xl font-bold mb-4">{slug}</h2>
      <p className="text-slate-500">Agency detail view coming next...</p>
    </div>
  )
}
