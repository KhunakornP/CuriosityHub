import type { Video } from '../components/VideoCard.vue'

const getBaseUrl = () => import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'

export async function fetchRecentVideos(): Promise<Video[]> {
  const url = new URL(`${getBaseUrl()}/recent-videos`)
  try {
    const res = await fetch(url.toString(), {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    })
    
    if (!res.ok) {
      console.error('Failed to fetch recent videos', { status: res.status })
      return []
    }
    
    const data = await res.json()
    return Array.isArray(data) ? data : []
  } catch (err) {
    console.error('Error fetching recent videos', err)
    return []
  }
}

export async function fetchVideoDetails(videoId: string): Promise<any> {
  const url = new URL(`${getBaseUrl()}/video-details`)
  try {
    const res = await fetch(url.toString(), {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'videoId': videoId,
      },
    })
    
    if (!res.ok) {
      console.error('Failed to fetch video details', { status: res.status })
      return null
    }
    
    return await res.json()
  } catch (err) {
    console.error('Error fetching video details', err)
    return null
  }
}

export async function fetchVideoStreamBlobUrl(videoId: string): Promise<string | null> {
  const url = new URL(`${getBaseUrl()}/video`)
  try {
    const res = await fetch(url.toString(), {
      method: 'GET',
      headers: {
        'videoId': videoId,
      },
    })
    
    if (!res.ok) {
      console.error('Failed to fetch video stream', { status: res.status })
      return null
    }
    
    const blob = await res.blob()
    return URL.createObjectURL(blob)
  } catch (err) {
    console.error('Error fetching video stream', err)
    return null
  }
}

export async function uploadVideo(formData: FormData, token: string): Promise<boolean> {
  const url = new URL(`${getBaseUrl()}/upload`)
  try {
    const res = await fetch(url.toString(), {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`
      },
      body: formData,
      // Note: Don't set 'Content-Type' header manually when sending FormData, 
      // the browser will automatically set it to 'multipart/form-data' with the correct boundary.
    })
    
    if (!res.ok) {
      console.error('Failed to upload video', { status: res.status })
      return false
    }
    
    return true
  } catch (err) {
    console.error('Error uploading video', err)
    return false
  }
}
