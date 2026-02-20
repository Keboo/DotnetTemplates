import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Box,
  Button,
  Card,
  CardContent,
  CardActions,
  Typography,
  Grid2,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  CircularProgress,
} from '@mui/material'
import { Add as AddIcon, Delete as DeleteIcon, Edit as EditIcon } from '@mui/icons-material'
import { useSnackbar } from 'notistack'
import { RoomDto } from '@/types'
import { apiClient } from '@/services/apiClient'
import JoinRoomForm from '@/components/JoinRoomForm'

export default function MyRooms() {
  const [rooms, setRooms] = useState<RoomDto[]>([])
  const [loading, setLoading] = useState(true)
  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [newRoomName, setNewRoomName] = useState('')
  const navigate = useNavigate()
  const { enqueueSnackbar } = useSnackbar()

  const loadRooms = useCallback(async () => {
    try {
      const data = await apiClient.get<RoomDto[]>('/api/rooms/my')
      setRooms(data)
    } catch {
      enqueueSnackbar('Failed to load rooms', { variant: 'error' })
    } finally {
      setLoading(false)
    }
  }, [enqueueSnackbar])

  useEffect(() => {
    loadRooms()
  }, [loadRooms])

  const handleCreateRoom = async () => {
    if (!newRoomName.trim()) {
      enqueueSnackbar('Room name is required', { variant: 'error' })
      return
    }

    try {
      const room = await apiClient.post<RoomDto>('/api/rooms', { friendlyName: newRoomName })
      setRooms([...rooms, room])
      setCreateDialogOpen(false)
      setNewRoomName('')
      enqueueSnackbar('Room created successfully', { variant: 'success' })
    } catch (error) {
      enqueueSnackbar(error instanceof Error ? error.message : 'Failed to create room', { variant: 'error' })
    }
  }

  const handleDeleteRoom = async (roomId: string) => {
    if (!confirm('Are you sure you want to delete this room?')) {
      return
    }

    try {
      await apiClient.delete(`/api/rooms/${roomId}`)
      setRooms(rooms.filter((r) => r.id !== roomId))
      enqueueSnackbar('Room deleted successfully', { variant: 'success' })
    } catch {
      enqueueSnackbar('Failed to delete room', { variant: 'error' })
    }
  }

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" mt={4}>
        <CircularProgress />
      </Box>
    )
  }

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">
          My Rooms
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => setCreateDialogOpen(true)}
          data-testid="create-room-button"
        >
          Create Room
        </Button>
      </Box>

      {rooms.length === 0 ? (
        <Typography variant="body1" color="text.secondary">
          You haven't created any rooms yet.
        </Typography>
      ) : (
        <Grid2 container spacing={3}>
          {rooms.map((room) => (
            <Grid2 size={{ xs: 12, sm: 6, md: 4 }} key={room.id}>
              <Card>
                <CardContent>
                  <Typography variant="h6" component="h2" gutterBottom>
                    {room.friendlyName}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Room ID: {room.id.substring(0, 8)}...
                  </Typography>
                </CardContent>
                <CardActions>
                  <Button
                    size="small"
                    startIcon={<EditIcon />}
                    onClick={() => navigate(`/room/${room.friendlyName}/manage`)}
                  >
                    Manage
                  </Button>
                  <Button
                    size="small"
                    color="error"
                    startIcon={<DeleteIcon />}
                    onClick={() => handleDeleteRoom(room.id)}
                  >
                    Delete
                  </Button>
                </CardActions>
              </Card>
            </Grid2>
          ))}
        </Grid2>
      )}

      <Box sx={{ mt: 6, maxWidth: 600, mx: 'auto' }}>
        <JoinRoomForm 
          title="Join an Existing Room"
          description="Enter a room name to join a Q&A session."
        />
      </Box>

      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)}>
        <DialogTitle>Create New Room</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="Room Name"
            fullWidth
            value={newRoomName}
            onChange={(e) => setNewRoomName(e.target.value)}
            placeholder="my-awesome-room"
            data-testid="room-name-dialog-input"
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleCreateRoom} variant="contained" data-testid="create-room-dialog-button">
            Create
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
