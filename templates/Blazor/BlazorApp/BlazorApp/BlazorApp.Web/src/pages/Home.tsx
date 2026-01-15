import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Box, TextField, Button, Typography, Paper } from '@mui/material'
import { useSnackbar } from 'notistack'

export default function Home() {
  const [friendlyName, setFriendlyName] = useState('')
  const navigate = useNavigate()
  const { enqueueSnackbar } = useSnackbar()

  const handleJoinRoom = (e: React.FormEvent) => {
    e.preventDefault()
    if (!friendlyName.trim()) {
      enqueueSnackbar('Please enter a room name', { variant: 'error' })
      return
    }
    navigate(`/room/${friendlyName}`)
  }

  return (
    <Box sx={{ maxWidth: 600, mx: 'auto', mt: 4 }}>
      <Paper elevation={3} sx={{ p: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Join a Q&A Room
        </Typography>
        <Typography variant="body1" color="text.secondary" paragraph>
          Enter a room name to join or create a new Q&A session.
        </Typography>
        <Box component="form" onSubmit={handleJoinRoom} sx={{ mt: 3 }}>
          <TextField
            fullWidth
            label="Room Name"
            value={friendlyName}
            onChange={(e) => setFriendlyName(e.target.value)}
            placeholder="my-awesome-room"
            autoFocus
            data-testid="room-name-input"
          />
          <Button
            type="submit"
            variant="contained"
            size="large"
            fullWidth
            sx={{ mt: 2 }}
            data-testid="join-room-button"
          >
            Join Room
          </Button>
        </Box>
      </Paper>
    </Box>
  )
}
