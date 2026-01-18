import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import { QuestionDto, RoomDto } from '@/types'

export type RoomHubEvents = {
  onQuestionSubmitted: (question: QuestionDto) => void
  onQuestionApproved: (question: QuestionDto) => void
  onQuestionAnswered: (question: QuestionDto) => void
  onQuestionDeleted: (questionId: string) => void
  onCurrentQuestionChanged: (question: QuestionDto | null) => void
  onRoomUpdated: (room: RoomDto) => void
}

export class RoomHubConnection {
  private connection: HubConnection | null = null
  private events: Partial<RoomHubEvents> = {}

  constructor(
    private baseUrl: string = ''
  ) {}

  async start(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return
    }

    const url = `${this.baseUrl}/hubs/room`

    console.log('[RoomHub] Starting connection to:', url)

    this.connection = new HubConnectionBuilder()
      .withUrl(url, {
        withCredentials: true  // Enable cookies for authentication
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build()

    // Register event handlers
    this.connection.on('QuestionSubmitted', (question: QuestionDto) => {
      this.events.onQuestionSubmitted?.(question)
    })

    this.connection.on('QuestionApproved', (question: QuestionDto) => {
      this.events.onQuestionApproved?.(question)
    })

    this.connection.on('QuestionAnswered', (question: QuestionDto) => {
      this.events.onQuestionAnswered?.(question)
    })

    this.connection.on('QuestionDeleted', (questionId: string) => {
      this.events.onQuestionDeleted?.(questionId)
    })

    this.connection.on('CurrentQuestionChanged', (question: QuestionDto | null) => {
      this.events.onCurrentQuestionChanged?.(question)
    })

    this.connection.on('RoomUpdated', (room: RoomDto) => {
      this.events.onRoomUpdated?.(room)
    })

    try {
      await this.connection.start()
      console.log('[RoomHub] Connection established successfully')
    } catch (error) {
      console.error('[RoomHub] Failed to start connection:', error)
      throw error
    }
  }

  async stop(): Promise<void> {
    if (this.connection) {
      await this.connection.stop()
    }
  }

  async joinAsParticipant(roomId: string): Promise<void> {
    if (!this.connection) throw new Error('Connection not started')
    await this.connection.invoke('JoinRoom', roomId)
  }

  async leaveAsParticipant(roomId: string): Promise<void> {
    if (!this.connection) throw new Error('Connection not started')
    await this.connection.invoke('LeaveRoom', roomId)
  }

  async joinAsOwner(roomId: string): Promise<void> {
    if (!this.connection) throw new Error('Connection not started')
    await this.connection.invoke('JoinRoomAsOwner', roomId)
  }

  async leaveAsOwner(roomId: string): Promise<void> {
    if (!this.connection) throw new Error('Connection not started')
    await this.connection.invoke('LeaveRoomAsOwner', roomId)
  }

  on<K extends keyof RoomHubEvents>(event: K, handler: RoomHubEvents[K]): void {
    this.events[event] = handler
  }

  off<K extends keyof RoomHubEvents>(event: K): void {
    delete this.events[event]
  }

  getState(): HubConnectionState {
    return this.connection?.state ?? HubConnectionState.Disconnected
  }
}
