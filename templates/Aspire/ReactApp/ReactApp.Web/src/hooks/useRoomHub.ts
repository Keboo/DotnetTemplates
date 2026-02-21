import { useEffect, useRef, useState } from 'react'
import { RoomHubConnection, RoomHubEvents } from '@/services/roomHub'
import { HubConnectionState } from '@microsoft/signalr'

export function useRoomHub(
  roomId: string | undefined,
  asOwner: boolean,
  events?: Partial<RoomHubEvents>
) {
  const [connected, setConnected] = useState(false)
  const hubRef = useRef<RoomHubConnection | null>(null)
  const eventsRef = useRef(events)
  
  // Keep events ref up to date
  useEffect(() => {
    eventsRef.current = events
  }, [events])

  useEffect(() => {
    if (!roomId) return

    let cancelled = false
    
    // Use backend URL from vite config (via define)
    const baseUrl = __API_BASE_URL__
    
    console.info('[useRoomHub] Initializing RoomHubConnection with baseUrl:', baseUrl)
    const hubInstance = new RoomHubConnection(baseUrl)
    hubRef.current = hubInstance

    // Register events
    if (eventsRef.current) {
      Object.entries(eventsRef.current).forEach(([event, handler]) => {
        if (handler) {
          hubInstance.on(event as keyof RoomHubEvents, handler)
        }
      })
    }

    // Start connection and join room
    const initHub = async () => {
      try {
        await hubInstance.start()
        if (cancelled) {
          await hubInstance.stop()
          return
        }
        setConnected(true)
        
        if (asOwner) {
          // Join as owner - authentication is handled via cookies
          await hubInstance.joinAsOwner(roomId)
        } else {
          await hubInstance.joinAsParticipant(roomId)
        }
      } catch (error) {
        if (!cancelled) {
          console.error('Failed to connect to hub:', error)
          setConnected(false)
        }
      }
    }

    initHub()

    return () => {
      cancelled = true
      setConnected(false)
      hubRef.current = null
      
      const cleanup = async () => {
        if (hubInstance) {
          try {
            // Only try to leave if connection is still active
            const state = hubInstance.getState()
            if (state === HubConnectionState.Connected && roomId) {
              await hubInstance.leaveAsOwner(roomId)
            }
            await hubInstance.stop()
          } catch (error) {
            console.error('Error cleaning up hub:', error)
          }
        }
      }
      cleanup()
    }
  }, [roomId, asOwner])

  return { connected, hubRef }
}
