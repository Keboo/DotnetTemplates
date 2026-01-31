import { Outlet } from 'react-router-dom'
import { AppBar, Toolbar, Typography, Button, Box, IconButton, Container } from '@mui/material'
import { Brightness4, Brightness7 } from '@mui/icons-material'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import { useTheme } from '@/contexts/ThemeContext'

export default function Layout() {
  const navigate = useNavigate()
  const { user, logout } = useAuth()
  const { mode, toggleTheme } = useTheme()

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <AppBar position="static">
        <Toolbar>
          <Typography
            variant="h6"
            component="div"
            sx={{ flexGrow: 1, cursor: 'pointer' }}
            onClick={() => navigate('/')}
          >
            Q&A Rooms
          </Typography>

          <IconButton 
            sx={{ ml: 1 }} 
            onClick={toggleTheme} 
            color="inherit"
            aria-label={mode === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
          >
            {mode === 'dark' ? <Brightness7 /> : <Brightness4 />}
          </IconButton>

          {user?.isAuthenticated ? (
            <>
              <Button color="inherit" onClick={() => navigate('/my-rooms')}>
                My Rooms
              </Button>
              <Button color="inherit" onClick={handleLogout}>
                Logout
              </Button>
            </>
          ) : (
            <>
              <Button color="inherit" onClick={() => navigate('/login')}>
                Login
              </Button>
              <Button color="inherit" onClick={() => navigate('/register')}>
                Register
              </Button>
            </>
          )}
        </Toolbar>
      </AppBar>

      <Container component="main" sx={{ flex: 1, py: 3 }}>
        <Outlet />
      </Container>

      <Box component="footer" sx={{ py: 2, px: 2, mt: 'auto', backgroundColor: 'background.paper' }}>
        <Typography variant="body2" color="text.secondary" align="center">
          © {new Date().getFullYear()} ReactApp
        </Typography>
      </Box>
    </Box>
  )
}
