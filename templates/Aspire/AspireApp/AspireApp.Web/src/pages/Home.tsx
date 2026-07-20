import { Box } from '@mui/material'
import JoinRoomForm from '@/components/JoinRoomForm'

export default function Home() {
  return (
    <Box sx={{ maxWidth: 600, mx: 'auto', mt: 4 }}>
      <JoinRoomForm 
        title="Join a Q&A Room"
        description="Enter a room name to join or create a new Q&A session."
        autoFocus
      />
    </Box>
  )
}
