import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Box, TextField, Button, Typography, Paper } from '@mui/material'
import { useSnackbar } from 'notistack'

interface JoinRoomFormProps {
  title?: string
  description?: string
  autoFocus?: boolean
}

export default function JoinRoomForm({ 
  title = 'Join a Q&A Room', 
  description = 'Enter a room name to join or create a new Q&A session.',
  autoFocus = false 
}: JoinRoomFormProps) {
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
    <Paper elevation={3} sx={{ p: 4 }}>
      <Typography variant="h5" component="h1" gutterBottom>
        {title}
      </Typography>
      <Typography variant="body1" color="text.secondary" paragraph>
        {description}
      </Typography>
      <Box component="form" onSubmit={handleJoinRoom} sx={{ mt: 3 }}>
        <TextField
          fullWidth
          label="Room Name"
          value={friendlyName}
          onChange={(e) => setFriendlyName(e.target.value)}
          placeholder="my-awesome-room"
          autoFocus={autoFocus}
          data-testid="join-room-name-input"
        />
        <Button
          type="submit"
          variant="contained"
          size="large"
          fullWidth
          sx={{ mt: 2 }}
          data-testid="join-existing-room-button"
        >
          Join Room
        </Button>
      </Box>
    </Paper>
  )
}
