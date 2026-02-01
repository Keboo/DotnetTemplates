class ApiClient {
  private baseUrl = ''

  async get<T>(url: string): Promise<T> {
    const response = await fetch(this.baseUrl + url, {
      credentials: 'include',
    })
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`)
    }
    
    return response.json()
  }

  async post<T = void>(url: string, data?: unknown): Promise<T> {
    const response = await fetch(this.baseUrl + url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'include',
      body: data ? JSON.stringify(data) : undefined,
    })
    
    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || `HTTP error! status: ${response.status}`)
    }
    
    if (response.status === 204) {
      return undefined as T
    }
    
    return response.json()
  }

  async delete<T = void>(url: string): Promise<T> {
    const response = await fetch(this.baseUrl + url, {
      method: 'DELETE',
      credentials: 'include',
    })
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`)
    }
    
    if (response.status === 204) {
      return undefined as T
    }
    
    return response.json()
  }

  async put<T = void>(url: string, data?: unknown): Promise<T> {
    const response = await fetch(this.baseUrl + url, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'include',
      body: data ? JSON.stringify(data) : undefined,
    })
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`)
    }
    
    if (response.status === 204) {
      return undefined as T
    }
    
    return response.json()
  }
}

export const apiClient = new ApiClient()
