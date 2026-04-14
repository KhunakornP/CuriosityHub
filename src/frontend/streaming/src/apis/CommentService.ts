export interface Comment {
  id: string
  userId: string
  text: string
  parentCommentId: string | null
  videoId: string
  createdAt: string
}

export interface CreateCommentRequest {
  user: string
  comment: string
  parent_Comment: string | null
  video_Id: string
}

const getBaseUrl = () => import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'

export async function fetchComments(videoId: string): Promise<Comment[]> {
  const url = new URL(`${getBaseUrl()}/comment`)
  try {
    const res = await fetch(url.toString(), {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'videoId': videoId,
      },
    })
    
    if (!res.ok) {
      console.error('Failed to fetch comments', { status: res.status })
      return []
    }
    
    const data = await res.json()
    return Array.isArray(data) ? data : []
  } catch (err) {
    console.error('Error fetching comments', err)
    return []
  }
}

export async function fetchReplies(commentId: string): Promise<Comment[]> {
  const url = new URL(`${getBaseUrl()}/replies`)
  try {
    const res = await fetch(url.toString(), {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'commentId': commentId,
      },
    })
    
    if (!res.ok) {
      console.error('Failed to fetch replies', { status: res.status })
      return []
    }
    
    const data = await res.json()
    return Array.isArray(data) ? data : []
  } catch (err) {
    console.error('Error fetching replies', err)
    return []
  }
}

export async function postComment(request: CreateCommentRequest): Promise<Comment | null> {
  const url = new URL(`${getBaseUrl()}/comment`)
  try {
    const res = await fetch(url.toString(), {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'videoId': request.video_Id,
      },
      body: JSON.stringify(request),
    })
    
    if (!res.ok) {
      console.error('Failed to post comment', { status: res.status })
      return null
    }
    
    return await res.json()
  } catch (err) {
    console.error('Error posting comment', err)
    return null
  }
}
