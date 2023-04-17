# Network Timers

## Implementation Overview
Since many games use time limits, a working timer that can be synced across the network properly is often a necessary part of multiplayer games. With this implementation of [NetworkTimer](./Networking/NetworkTimer.cs), we created a clock that is synced up from the server, but calculated on the client to keep network traffic low while maintaining synchronization for all connected users.

## How It Works
When a timer is spawned or created, it spawns in a 'dormant' state. Calling __StartTimer()__ on the newly spawned timer will start it, along with setting the duration of the timer. The timer will then keep track of the time from the starting time. The timer itself works by using the NetworkManager's LocalTime to set the start time variable. This start time variable is networked, so clients will also get this starting time value from the server.

The duration of the timer is also networked. With this information, the client can calculate the current state of the timer by reading __TimeRemaining__, which calculates the remaining time by adding together the duration and start time, then subtracting the current local time from that. The number of seconds remaining on the timer can be changed while it is running using the __SetTimer()__ method.

## Timer Events
The NetworkTimer has multiple events that a developer can use:

- __OnTimerStartedOnServer__
  - This event is fired when the timer is started on the server instance only. This event does not run on clients.
- __OnTimerExpiredOnServer__
  - This event is fired when the timer has ended on the server instance only. This event also does not run on clients.
- __OnClientTransitionTimeReached__
  - This event is fired when the timer reaches the specified time left (adjustable in the timer component). This can be used to add transitions or do things when the timer is about to end. This event is called on all clients.
