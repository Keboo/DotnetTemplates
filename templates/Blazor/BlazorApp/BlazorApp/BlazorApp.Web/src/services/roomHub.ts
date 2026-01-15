import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import { QuestionDto, RoomDto } from '@/types'

export type RoomHubEvents = {
  onQuestionCreated: (question: QuestionDto) => void
  onQuestionApproved: (questionId: string) => void
  onQuestionAnswered: (questionId: string) => void
  onQuestionDeleted: (questionId: string) => void
  onCurrentQuestionChanged: (questionId: string | null) => void
  onRoomUpdated: (room: RoomDto) => void
}

export class RoomHubConnection {
  private connection: HubConnection | null = null
  private events: Partial<RoomHubEvents> = {}

  constructor(
    private baseUrl: string = '',
    private accessToken?: string
  ) {}

  async start(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return
    }

    const url = `${this.baseUrl}/hubs/room${this.accessToken ? `?access_token=${this.accessToken}` : ''}`

    console.log('[RoomHub] Starting connection to:', url.replace(/access_token=[^&]+/, 'access_token=***'))

    this.connection = new HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build()

    // Register event handlers
    this.connection.on('QuestionCreated', (question: QuestionDto) => {
      this.events.onQuestionCreated?.(question)
    })

    this.connection.on('QuestionApproved', (questionId: string) => {
      this.events.onQuestionApproved?.(questionId)
    })

    this.connection.on('QuestionAnswered', (questionId: string) => {
      this.events.onQuestionAnswered?.(questionId)
    })

    this.connection.on('QuestionDeleted', (questionId: string) => {
      this.events.onQuestionDeleted?.(questionId)
    })

    this.connection.on('CurrentQuestionChanged', (questionId: string | null) => {
      this.events.onCurrentQuestionChanged?.(questionId)
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
