const getBaseUrl = () => import.meta.env.VITE_ADMIN_API_BASE_URL ?? 'http://localhost:5001'

export async function fetchVideos(page: number, pageSize: number): Promise<any[]> {
  const url = new URL(`${getBaseUrl()}/videos`)
  url.searchParams.append('page', page.toString())
  url.searchParams.append('pageSize', pageSize.toString())
  
  try {
    const res = await fetch(url.toString(), {
      method: 'GET',
      headers: { 'Content-Type': 'application/json' },
    })
    
    if (!res.ok) {
      console.error('Failed to fetch videos', { status: res.status })
      return []
    }
    
    const data = await res.json()
    console.log(data)
    return Array.isArray(data) ? data : []
  } catch (err) {
    console.error('Error fetching videos', err)
    return []
  }
}

export async function updateVideoMetadata(videoId: string, title: string, description: string): Promise<boolean> {
  const url = new URL(`${getBaseUrl()}/video-metadata`)
  try {
    const res = await fetch(url.toString(), {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ videoId, title, description }),
    })
    return res.ok;
  } catch (err) {
    console.error('Error updating video metadata', err)
    return false
  }
}

export async function deleteVideo(videoId: string): Promise<boolean> {
  const url = new URL(`${getBaseUrl()}/video?videoId=${videoId}`)
  try {
    const res = await fetch(url.toString(), {
      method: 'DELETE'
    })
    return res.ok;
  } catch (err) {
    console.error('Error deleting video', err)
    return false
  }
}
