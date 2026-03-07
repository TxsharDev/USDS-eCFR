// All API calls go through the Vite proxy in dev, same origin in prod
const BASE = '/api';

async function get(path) {
  const res = await fetch(`${BASE}${path}`);
  if (!res.ok) throw new Error(`API ${res.status}: ${path}`);
  return res.json();
}

export const api = {
  health: () => get('/health'),
  agencies: (sort, limit) => {
    const params = new URLSearchParams();
    if (sort) params.set('sort', sort);
    if (limit) params.set('limit', limit);
    return get(`/agencies?${params}`);
  },
  agency: (slug) => get(`/agencies/${slug}`),
  wordCounts: (limit) => get(`/analytics/word-counts?limit=${limit || 15}`),
  burdenIndex: (limit) => get(`/analytics/burden-index?limit=${limit || 15}`),
  heatmap: (months) => get(`/analytics/heatmap?months=${months || 12}`),
  checksums: () => get('/analytics/checksums'),
};
