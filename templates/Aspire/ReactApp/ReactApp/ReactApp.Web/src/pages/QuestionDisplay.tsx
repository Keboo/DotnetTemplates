import { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { Box, Typography, Paper, CircularProgress } from '@mui/material'
import { QuestionDto, RoomDto } from '@/types'
import { apiClient } from '@/services/apiClient'
import { useRoomHub } from '@/hooks/useRoomHub'

export default function QuestionDisplay() {
  const { friendlyName } = useParams<{ friendlyName: string }>()
  const [room, setRoom] = useState<RoomDto | null>(null)
  const [currentQuestion, setCurrentQuestion] = useState<QuestionDto | null>(null)
  const [loading, setLoading] = useState(true)

  useRoomHub(room?.id, false, {
    onCurrentQuestionChanged: async (question) => {
      if (!room) return

      if (!question) {
        setCurrentQuestion(null)
        return
      }

      try {
        const questions = await apiClient.get<QuestionDto[]>(
          `/api/rooms/${room.id}/questions/approved`
        )
        setCurrentQuestion(questions.find((q) => q.id === question.id) || null)
      } catch (error) {
        console.error('Failed to load current question:', error)
      }
    },
    onQuestionAnswered: (question) => {
      if (currentQuestion?.id === question.id) {
        setCurrentQuestion({ ...currentQuestion, isAnswered: true })
      }
    },
  })

  useEffect(() => {
    const loadRoom = async () => {
      if (!friendlyName) return

      try {
        const roomData = await apiClient.get<RoomDto>(`/api/rooms/name/${friendlyName}`)
        setRoom(roomData)

        if (roomData.currentQuestionId) {
          const questions = await apiClient.get<QuestionDto[]>(
            `/api/rooms/${roomData.id}/questions/approved`
          )
          const question = questions.find((q) => q.id === roomData.currentQuestionId)
          setCurrentQuestion(question || null)
        }
      } catch (error) {
        console.error('Failed to load room:', error)
      } finally {
        setLoading(false)
      }
    }

    loadRoom()
  }, [friendlyName])

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="100vh">
        <CircularProgress size={60} />
      </Box>
    )
  }

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'background.default',
        p: 4,
      }}
    >
      {currentQuestion ? (
        <Paper
          elevation={6}
          sx={{
            p: 6,
            maxWidth: 1200,
            width: '100%',
            textAlign: 'center',
          }}
        >
          <Typography variant="h3" component="h1" gutterBottom>
            {currentQuestion.questionText}
          </Typography>
          <Typography variant="h6" color="text.secondary" sx={{ mt: 4 }}>
            — {currentQuestion.authorName}
          </Typography>
          {currentQuestion.isAnswered && (
            <Typography variant="h5" color="success.main" sx={{ mt: 4 }}>
              ✓ Answered
            </Typography>
          )}
        </Paper>
      ) : (
        <Paper elevation={6} sx={{ p: 6, textAlign: 'center' }}>
          <Typography variant="h4" color="text.secondary">
            {room ? 'No question currently selected' : 'Room not found'}
          </Typography>
        </Paper>
      )}
    </Box>
  )
}
