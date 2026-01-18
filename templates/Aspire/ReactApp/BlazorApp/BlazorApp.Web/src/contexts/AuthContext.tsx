import { createContext, useContext, useState, useEffect, ReactNode } from 'react'
import { UserInfo, LoginRequest, RegisterRequest } from '@/types'
import { apiClient } from '@/services/apiClient'

interface AuthContextType {
  user: UserInfo | null
  loading: boolean
  login: (credentials: LoginRequest) => Promise<void>
  register: (data: RegisterRequest) => Promise<void>
  logout: () => Promise<void>
  refreshUser: () => Promise<void>
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserInfo | null>(null)
  const [loading, setLoading] = useState(true)

  const refreshUser = async () => {
    try {
      const userData = await apiClient.get<UserInfo>('/api/auth/user')
      setUser(userData)
    } catch {
      setUser(null)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    refreshUser()
  }, [])

  const login = async (credentials: LoginRequest) => {
    const userData = await apiClient.post<UserInfo>('/api/auth/login', credentials)
    setUser(userData)
  }

  const register = async (data: RegisterRequest) => {
    const userData = await apiClient.post<UserInfo>('/api/auth/register', data)
    setUser(userData)
  }

  const logout = async () => {
    await apiClient.post('/api/auth/logout')
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, loading, login, register, logout, refreshUser }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
