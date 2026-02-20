import { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import {
  Box,
  Paper,
  Typography,
  TextField,
  Button,
  List,
  ListItem,
  ListItemText,
  CircularProgress,
  Divider,
} from '@mui/material'
import { Send as SendIcon } from '@mui/icons-material'
import { useSnackbar } from 'notistack'
import { QuestionDto, RoomDto } from '@/types'
import { apiClient } from '@/services/apiClient'
import { useRoomHub } from '@/hooks/useRoomHub'

export default function Room() {
  const { friendlyName } = useParams<{ friendlyName: string }>()
  const [room, setRoom] = useState<RoomDto | null>(null)
  const [questions, setQuestions] = useState<QuestionDto[]>([])
  const [loading, setLoading] = useState(true)
  const [questionText, setQuestionText] = useState('')
  const [authorName, setAuthorName] = useState(() => {
    return localStorage.getItem('authorName') || ''
  })
  const { enqueueSnackbar } = useSnackbar()

  const refetchQuestions = async () => {
    if (!room?.id) return
    try {
      const questionsData = await apiClient.get<QuestionDto[]>(
        `/api/rooms/${room.id}/questions/approved`
      )
      setQuestions(questionsData)
    } catch (error) {
      console.error('Failed to refetch questions:', error)
    }
  }

  const updateOrRefetch = (question: QuestionDto, updater: (q: QuestionDto) => QuestionDto) => {
    setQuestions((prev) => {
      const exists = prev.some((q) => q.id === question.id)
      if (!exists) {
        refetchQuestions()
        return prev
      }
      return prev.map((q) => (q.id === question.id ? updater(q) : q))
    })
  }

  // SignalR connection for real-time updates
  useRoomHub(room?.id, false, {
    onQuestionSubmitted: (question) => {
      setQuestions((prev) =>
        prev.map((q) => (q.id === question.id ? { ...q, isApproved: true } : q))
      )
    },
    onQuestionAnswered: (question) => {
      updateOrRefetch(question, (q) => ({ ...q, isAnswered: true }))
    },
    onQuestionApproved: (question) => {
      updateOrRefetch(question, (q) => ({ ...q, isApproved: true }))
    },
    onQuestionDeleted: (questionId) => {
      setQuestions((prev) => prev.filter((q) => q.id !== questionId))
    },
    onCurrentQuestionChanged: (question) => {
      if (question) {
        setQuestions((prev) => {
          const exists = prev.some((q) => q.id === question.id)
          if (!exists) {
            refetchQuestions()
          }
          return prev
        })
      }
      setRoom((prev) => (prev ? { ...prev, currentQuestionId: question?.id || undefined } : null))
    },
  })

  useEffect(() => {
    const loadRoom = async () => {
      if (!friendlyName) return

      try {
        const roomData = await apiClient.get<RoomDto>(`/api/rooms/name/${friendlyName}`)
        setRoom(roomData)

        const questionsData = await apiClient.get<QuestionDto[]>(
          `/api/rooms/${roomData.id}/questions/approved`
        )
        setQuestions(questionsData)
      } catch {
        enqueueSnackbar('Room not found', { variant: 'error' })
      } finally {
        setLoading(false)
      }
    }

    loadRoom()
  }, [friendlyName, enqueueSnackbar])

  const handleSubmitQuestion = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!questionText.trim() || !authorName.trim()) {
      enqueueSnackbar('Please fill in all fields', { variant: 'error' })
      return
    }

    if (!room) return

    try {
      await apiClient.post(`/api/rooms/${room.id}/questions`, {
        questionText,
        authorName,
      })

      localStorage.setItem('authorName', authorName)
      setQuestionText('')
      enqueueSnackbar('Question submitted! Waiting for approval...', { variant: 'success' })
    } catch (error) {
      enqueueSnackbar(error instanceof Error ? error.message : 'Failed to submit question', {
        variant: 'error',
      })
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
    return (
      <Typography variant="h5" color="error">
        Room not found
      </Typography>
    )
  }

  const currentQuestion = questions.find((q) => q.id === room.currentQuestionId)

  return (
    <Box sx={{ maxWidth: 800, mx: 'auto' }}>
      <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          {room.friendlyName}
        </Typography>

        {currentQuestion && (
          <Box sx={{ mb: 3, p: 2, bgcolor: 'primary.main', color: 'primary.contrastText', borderRadius: 1 }}>
            <Typography variant="h6" component="h2">Current Question:</Typography>
            <Typography variant="body1">{currentQuestion.questionText}</Typography>
            <Typography variant="caption">— {currentQuestion.authorName}</Typography>
          </Box>
        )}

        <Typography variant="h6" component="h2" gutterBottom>
          Submit a Question
        </Typography>
        <Box component="form" onSubmit={handleSubmitQuestion}>
          <TextField
            fullWidth
            label="Your Name"
            value={authorName}
            onChange={(e) => setAuthorName(e.target.value)}
            margin="normal"
            required
            data-testid="author-name-input"
          />
          <TextField
            fullWidth
            label="Your Question"
            value={questionText}
            onChange={(e) => setQuestionText(e.target.value)}
            margin="normal"
            multiline
            rows={3}
            required
            data-testid="question-text-input"
          />
          <Button
            type="submit"
            variant="contained"
            startIcon={<SendIcon />}
            sx={{ mt: 2 }}
            data-testid="submit-question-button"
          >
            Submit Question
          </Button>
        </Box>
      </Paper>

      <Paper elevation={3} sx={{ p: 3 }}>
        <Typography variant="h6" gutterBottom>
          Upcoming Questions
        </Typography>
        {questions.length === 0 ? (
          <Typography color="text.secondary">No questions yet. Be the first to ask!</Typography>
        ) : (
          <List>
            {questions.map((question, index) => (
              <Box key={question.id}>
                {index > 0 && <Divider />}
                <ListItem>
                  <ListItemText
                    primary={question.questionText}
                    secondary={`— ${question.authorName}${question.isAnswered ? ' (Answered)' : ''}`}
                    sx={{
                      textDecoration: question.isAnswered ? 'line-through' : 'none',
                      opacity: question.isAnswered ? 0.6 : 1,
                    }}
                  />
                </ListItem>
              </Box>
            ))}
          </List>
        )}
      </Paper>
    </Box>
  )
}
