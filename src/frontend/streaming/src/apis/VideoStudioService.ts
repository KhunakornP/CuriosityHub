const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface VideoMetadata {
    id: string
    title: string
    description?: string
    thumbnailUrl: string
    channelName: string
    views: number
    publishedAt: string
    // kept for backwards compat with original UI
    videoId?: string
    publisherId?: string
}

export class VideoStudioService {
    static async getPublisherVideos(publisherId: string, token: string, page: number = 1, pageSize: number = 10): Promise<{ videos: VideoMetadata[], totalCount: number, page: number, pageSize: number }> {
        const response = await fetch(`${API_URL}/user-videos/${publisherId}?page=${page}&pageSize=${pageSize}`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        })
        if (!response.ok) {
            throw new Error('Failed to fetch publisher videos')
        }
        return response.json()
    }

    static async updateMetadata(videoId: string, title: string, description: string, token: string): Promise<void> {
        const response = await fetch(`${API_URL}/update`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ videoId, title, description })
        })
        if (!response.ok) {
            throw new Error('Failed to update metadata')
        }
    }

    static async updateVideoFile(videoId: string, file: File, token: string): Promise<void> {
        const response = await fetch(`${API_URL}/video`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'videoId': videoId,
                'Content-Type': file.type
            },
            body: file
        })
        if (!response.ok) {
            throw new Error('Failed to update video file')
        }
    }

    static async updateThumbnailFile(videoId: string, file: File, token: string): Promise<void> {
        const response = await fetch(`${API_URL}/thumbnail`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'videoId': videoId,
                'Content-Type': file.type
            },
            body: file
        })
        if (!response.ok) {
            throw new Error('Failed to update thumbnail file')
        }
    }
}
