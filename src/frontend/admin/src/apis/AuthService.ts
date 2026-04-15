const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001';

export const AuthService = {
  async login(email: string, password: string) {
    const res = await fetch(`${API_URL}/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ email, password })
    });
    
    if (!res.ok) {
      throw new Error('Invalid login credentials');
    }
    
    return res.json();
  }
};
