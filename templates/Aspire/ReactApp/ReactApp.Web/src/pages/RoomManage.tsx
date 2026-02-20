import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Box,
  Paper,
  Typography,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  IconButton,
  Chip,
  CircularProgress,
} from '@mui/material'
import {
  Check as CheckIcon,
  Delete as DeleteIcon,
  Visibility as VisibilityIcon,
} from '@mui/icons-material'
import { useSnackbar } from 'notistack'
import { QuestionDto, RoomDto } from '@/types'
import { apiClient } from '@/services/apiClient'
import { useRoomHub } from '@/hooks/useRoomHub'

export default function RoomManage() {
  const { friendlyName } = useParams<{ friendlyName: string }>()
  const [room, setRoom] = useState<RoomDto | null>(null)
  const [questions, setQuestions] = useState<QuestionDto[]>([])
  const [loading, setLoading] = useState(true)
  const { enqueueSnackbar } = useSnackbar()
  const navigate = useNavigate()

  // SignalR connection with owner access (uses cookie authentication)
  useRoomHub(room?.id, true, {
    onQuestionSubmitted: (question) => {
      setQuestions((prev) => [...prev, question])
      enqueueSnackbar('New question received!', { variant: 'info' })
    },
    onQuestionApproved: (question) => {
      setQuestions((prev) =>
        prev.map((q) => (q.id === question.id ? { ...q, isApproved: true } : q))
      )
    },
    onQuestionAnswered: (question) => {
      setQuestions((prev) =>
        prev.map((q) => (q.id === question.id ? { ...q, isAnswered: true } : q))
      )
    },
    onQuestionDeleted: (questionId) => {
      setQuestions((prev) => prev.filter((q) => q.id !== questionId))
    },
    onCurrentQuestionChanged: (question) => {
      setRoom((prev) => (prev ? { ...prev, currentQuestionId: question?.id || undefined } : null))
    }
  })

  useEffect(() => {
    const loadRoom = async () => {
      if (!friendlyName) return

      try {
        const roomData = await apiClient.get<RoomDto>(`/api/rooms/name/${friendlyName}`)
        setRoom(roomData)

        const questionsData = await apiClient.get<QuestionDto[]>(
          `/api/rooms/${roomData.id}/questions`
        )
        setQuestions(questionsData)
      } catch {
        enqueueSnackbar('Failed to load room', { variant: 'error' })
        navigate('/my-rooms')
      } finally {
        setLoading(false)
      }
    }

    loadRoom()
  }, [friendlyName, navigate, enqueueSnackbar])

  const handleApprove = async (questionId: string) => {
    if (!room) return

    try {
      await apiClient.put(`/api/rooms/${room.id}/questions/${questionId}/approve`)
      enqueueSnackbar('Question approved', { variant: 'success' })
    } catch {
      enqueueSnackbar('Failed to approve question', { variant: 'error' })
    }
  }

  const handleMarkAnswered = async (questionId: string) => {
    if (!room) return

    try {
      await apiClient.put(`/api/rooms/${room.id}/questions/${questionId}/answer`)
      enqueueSnackbar('Question marked as answered', { variant: 'success' })
    } catch {
      enqueueSnackbar('Failed to mark question as answered', { variant: 'error' })
    }
  }

  const handleSetCurrent = async (questionId: string) => {
    if (!room) return

    try {
      await apiClient.put(`/api/rooms/${room.id}/current-question/${questionId}`)
      setRoom({ ...room, currentQuestionId: questionId })
      enqueueSnackbar('Current question updated', { variant: 'success' })
    } catch {
      enqueueSnackbar('Failed to update current question', { variant: 'error' })
    }
  }

  const handleClearCurrent = async () => {
    if (!room) return

    try {
      await apiClient.delete(`/api/rooms/${room.id}/current-question`)
      setRoom({ ...room, currentQuestionId: undefined })
      enqueueSnackbar('Current question cleared', { variant: 'success' })
    } catch {
      enqueueSnackbar('Failed to clear current question', { variant: 'error' })
    }
  }

  const handleDelete = async (questionId: string) => {
    if (!room) return
    if (!confirm('Are you sure you want to delete this question?')) return

    try {
      await apiClient.delete(`/api/rooms/${room.id}/questions/${questionId}`)
      enqueueSnackbar('Question deleted', { variant: 'success' })
    } catch {
      enqueueSnackbar('Failed to delete question', { variant: 'error' })
    }
  }

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" mt={4}>
        <CircularProgress />
      </Box>
    )
  }

  if (!room) {
    return null
  }

  const pendingQuestions = questions.filter((q) => !q.isApproved)
  const approvedQuestions = questions.filter((q) => q.isApproved)

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">Manage: {room.friendlyName}</Typography>
        <Box>
          <Button
            variant="outlined"
            startIcon={<VisibilityIcon />}
            onClick={() => navigate(`/room/${friendlyName}`)}
          >
            View Public Room
          </Button>
        </Box>
      </Box>

      <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
        <Typography variant="h6" component="h2" gutterBottom>
          Pending Questions ({pendingQuestions.length})
        </Typography>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Question</TableCell>
                <TableCell>Author</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {pendingQuestions.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={3} align="center">
                    No pending questions
                  </TableCell>
                </TableRow>
              ) : (
                pendingQuestions.map((question) => (
                  <TableRow key={question.id}>
                    <TableCell>{question.questionText}</TableCell>
                    <TableCell>{question.authorName}</TableCell>
                    <TableCell align="right">
                      <IconButton
                        color="success"
                        onClick={() => handleApprove(question.id)}
                        data-testid="approve-question-button"
                      >
                        <CheckIcon />
                      </IconButton>
                      <IconButton color="error" onClick={() => handleDelete(question.id)}>
                        <DeleteIcon />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>

      <Paper elevation={3} sx={{ p: 3 }}>
        <Typography variant="h6" component="h2" gutterBottom>
          Approved Questions ({approvedQuestions.length})
        </Typography>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Question</TableCell>
                <TableCell>Author</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {approvedQuestions.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={4} align="center">
                    No approved questions
                  </TableCell>
                </TableRow>
              ) : (
                approvedQuestions.map((question) => (
                  <TableRow
                    key={question.id}
                    sx={{
                      bgcolor: room.currentQuestionId === question.id ? 'action.selected' : 'inherit',
                    }}
                  >
                    <TableCell>{question.questionText}</TableCell>
                    <TableCell>{question.authorName}</TableCell>
                    <TableCell>
                      {question.isAnswered && <Chip label="Answered" size="small" color="success" />}
                      {room.currentQuestionId === question.id && (
                        <Chip label="Current" size="small" color="primary" sx={{ ml: 1 }} />
                      )}
                    </TableCell>
                    <TableCell align="right">
                      {!question.isAnswered && (
                        <Button size="small" onClick={() => handleMarkAnswered(question.id)}>
                          Mark Answered
                        </Button>
                      )}
                      {room.currentQuestionId === question.id ? (
                        <Button size="small" onClick={handleClearCurrent}>
                          Clear Current
                        </Button>
                      ) : (
                        <Button size="small" onClick={() => handleSetCurrent(question.id)}>
                          Set Current
                        </Button>
                      )}
                      <IconButton color="error" onClick={() => handleDelete(question.id)}>
                        <DeleteIcon />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>
    </Box>
  )
}
