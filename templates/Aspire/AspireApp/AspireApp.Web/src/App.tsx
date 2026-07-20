import { Routes, Route, Navigate } from 'react-router-dom'
import { CssBaseline } from '@mui/material'
import { SnackbarProvider } from 'notistack'
import { AuthProvider } from './contexts/AuthContext'
import { ThemeProvider } from './contexts/ThemeContext'
import Layout from './components/Layout'
import ProtectedRoute from './components/ProtectedRoute'

// Pages
import Home from './pages/Home'
import Login from './pages/Login'
import Register from './pages/Register'
import MyRooms from './pages/MyRooms'
import Room from './pages/Room'
import RoomManage from './pages/RoomManage'
import QuestionDisplay from './pages/QuestionDisplay'

function App() {
  return (
    <ThemeProvider>
      <CssBaseline />
      <SnackbarProvider maxSnack={3}>
        <AuthProvider>
          <Routes>
            <Route path="/" element={<Layout />}>
              <Route index element={<Home />} />
              <Route path="login" element={<Login />} />
              <Route path="register" element={<Register />} />
              <Route path="room/:friendlyName" element={<Room />} />
              <Route path="room/:friendlyName/display" element={<QuestionDisplay />} />
              
              {/* Protected routes */}
              <Route path="my-rooms" element={
                <ProtectedRoute>
                  <MyRooms />
                </ProtectedRoute>
              } />
              <Route path="room/:friendlyName/manage" element={
                <ProtectedRoute>
                  <RoomManage />
                </ProtectedRoute>
              } />
              
              <Route path="*" element={<Navigate to="/" replace />} />
            </Route>
          </Routes>
        </AuthProvider>
      </SnackbarProvider>
    </ThemeProvider>
  )
}

export default App
