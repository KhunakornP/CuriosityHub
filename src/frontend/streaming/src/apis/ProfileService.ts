const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export const ProfileService = {
  async getProfile(token: string, targetId?: string) {
    const url = targetId 
      ? `${API_URL}/profile?targetId=${encodeURIComponent(targetId)}`
      : `${API_URL}/profile`;

    const res = await fetch(url, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (!res.ok) {
      throw new Error('Failed to fetch profile');
    }

    return res.json();
  },

  async updateProfile(token: string, data: { firstName: string, lastName: string, description: string, profileUrl: string }) {
    const res = await fetch(`${API_URL}/update-profile`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(data)
    });

    if (!res.ok) {
      throw new Error('Failed to update profile');
    }

    return res.json();
  }
};