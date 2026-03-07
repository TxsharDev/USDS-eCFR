import { useState } from 'react'

// Table of MD5 checksums per agency. Lets users verify regulation content integrity.
// Copy button puts the hash on the clipboard for easy comparison.
export default function ChecksumTable({ data }) {
    const [copied, setCopied] = useState(null)

    if (!data?.length) return null

    // Simple "Copied!" feedback then reset after 1.5s
    const handleCopy = (checksum, slug) => {
        navigator.clipboard.writeText(checksum)
        setCopied(slug)
        setTimeout(() => setCopied(null), 1500)
    }

    return (
        <div className="bg-slate-800 rounded-lg p-5 border border-slate-700">
            <h3 className="text-sm font-semibold text-slate-300 mb-4">Agency Checksums (MD5)</h3>
            <div className="overflow-x-auto max-h-96 overflow-y-auto">
                <table className="w-full text-sm">
                    <thead className="sticky top-0 bg-slate-800">
                        <tr className="text-left text-slate-400 border-b border-slate-700">
                            <th className="py-2 pr-4">Agency</th>
                            <th className="py-2 pr-4">Checksum</th>
                            <th className="py-2 w-16"></th>
                        </tr>
                    </thead>
                    <tbody>
                        {data.map(d => (
                            <tr key={d.slug} className="border-b border-slate-700/50 hover:bg-slate-700/30">
                                <td className="py-2 pr-4 text-slate-300">{d.shortName || d.name}</td>
                                <td className="py-2 pr-4 font-mono text-xs text-slate-400">{d.checksum}</td>
                                <td className="py-2">
                                    <button
                                        onClick={() => handleCopy(d.checksum, d.slug)}
                                        className="text-xs text-blue-400 hover:text-blue-300 bg-transparent border-none cursor-pointer"
                                    >
                                        {copied === d.slug ? 'Copied!' : 'Copy'}
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </div>
    )
}