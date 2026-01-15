import { useEffect, useRef, useState } from 'react'
import { RoomHubConnection, RoomHubEvents } from '@/services/roomHub'

export function useRoomHub(
  roomId: string | undefined,
  accessToken?: string,
  events?: Partial<RoomHubEvents>
) {
  const [connected, setConnected] = useState(false)
  const hubRef = useRef<RoomHubConnection | null>(null)

  useEffect(() => {
    if (!roomId) return

    const hub = new RoomHubConnection('', accessToken)
    hubRef.current = hub

    // Register events
    if (events) {
      Object.entries(events).forEach(([event, handler]) => {
        if (handler) {
          hub.on(event as keyof RoomHubEvents, handler as any)
        }
      })
    }

    // Start connection and join room
    const initHub = async () => {
      try {
        await hub.start()
        setConnected(true)
        
        if (accessToken) {
          await hub.joinAsOwner(roomId)
        } else {
          await hub.joinAsParticipant(roomId)
        }
      } catch (error) {
        console.error('Failed to connect to hub:', error)
        setConnected(false)
      }
    }

    initHub()

    return () => {
      const cleanup = async () => {
        if (hub && roomId) {
          try {
            // Only try to leave if connection is still active
            const state = hub.getState()
            if (state === 1) { // HubConnectionState.Connected = 1
              if (accessToken) {
                await hub.leaveAsOwner(roomId)
              } else {
                await hub.leaveAsParticipant(roomId)
              }
            }
            await hub.stop()
          } catch (error) {
            console.error('Error cleaning up hub:', error)
          }
        }
      }
      cleanup()
    }
  }, [roomId, accessToken])

  return { connected, hub: hubRef.current }
}
