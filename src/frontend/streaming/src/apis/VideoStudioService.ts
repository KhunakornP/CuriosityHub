const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface VideoMetadata {
    videoId: string
    title: string
    description: string
    totalDuration: number
    resolution: string
    publisherId: string
    publishedAt: string
}

export class VideoStudioService {
    static async getPublisherVideos(publisherId: string, token: string): Promise<VideoMetadata[]> {
        const response = await fetch(`${API_URL}/metadata/publisher?publisherId=${publisherId}`, {
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
