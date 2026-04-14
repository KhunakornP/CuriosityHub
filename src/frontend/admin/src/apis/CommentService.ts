const getBaseUrl = () => import.meta.env.VITE_ADMIN_API_BASE_URL ?? 'http://localhost:5001'

export async function fetchComments(page: number, pageSize: number): Promise<any[]> {
  const url = new URL(`${getBaseUrl()}/comments`)
  url.searchParams.append('page', page.toString())
  url.searchParams.append('pageSize', pageSize.toString())
  
  try {
    const res = await fetch(url.toString(), {
      method: 'GET',
      headers: { 'Content-Type': 'application/json' },
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

export async function updateComment(id: string, text: string): Promise<boolean> {
  const url = new URL(`${getBaseUrl()}/comment`)
  try {
    const res = await fetch(url.toString(), {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ id, text }),
    })
    return res.ok;
  } catch (err) {
    console.error('Error updating comment', err)
    return false
  }
}

export async function deleteComment(commentId: string): Promise<boolean> {
  const url = new URL(`${getBaseUrl()}/comment?commentId=${commentId}`)
  try {
    const res = await fetch(url.toString(), {
      method: 'DELETE'
    })
    return res.ok;
  } catch (err) {
    console.error('Error deleting comment', err)
    return false
  }
}
