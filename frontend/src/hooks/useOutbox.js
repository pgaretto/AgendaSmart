import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { api } from '../services/api'
import { createItem, loadItems, outboxKey, retryItem, saveItems, userIdFromToken } from '../services/outbox'
import { tokenStorage } from '../services/tokenStorage'

const SEND = {
  text: (item) => api.post('/api/parse', item.payload),
  entry: (item) => api.post('/api/entries', item.payload),
}

const wait = (ms) => new Promise((resolve) => setTimeout(resolve, ms))

// Cola de envíos fallidos por falta de señal (RNF-04). Reintenta sola al recuperar la conexión.
export function useOutbox({ onEntrySaved }) {
  const key = useMemo(() => outboxKey(userIdFromToken(tokenStorage.get()) ?? 'anonimo'), [])
  const [items, setItems] = useState(() => loadItems(localStorage, key))
  const [online, setOnline] = useState(() => navigator.onLine)

  const itemsRef = useRef(items)
  const running = useRef(false)
  const onEntrySavedRef = useRef(onEntrySaved)
  useEffect(() => {
    onEntrySavedRef.current = onEntrySaved
  })

  const commit = useCallback(
    (next) => {
      itemsRef.current = next
      setItems(next)
      saveItems(localStorage, key, next)
    },
    [key],
  )

  const replace = useCallback(
    (item) => commit(itemsRef.current.map((i) => (i.id === item.id ? item : i))),
    [commit],
  )

  const runQueue = useCallback(async () => {
    if (running.current || !navigator.onLine || !tokenStorage.get()) return
    running.current = true
    try {
      for (const pending of itemsRef.current.filter((i) => i.status === 'queued')) {
        const outcome = await retryItem(pending, { send: (i) => SEND[i.kind](i), wait, onProgress: replace })

        if (outcome.status === 'done') {
          commit(itemsRef.current.filter((i) => i.id !== outcome.id))
          onEntrySavedRef.current?.()
        } else {
          replace(outcome)
          // Sesión vencida: el resto de la cola fallaría igual; se retoma al volver a iniciar sesión.
          if (outcome.error === 'sesion') break
        }
      }
    } finally {
      running.current = false
    }
  }, [commit, replace])

  useEffect(() => {
    const goOnline = () => {
      setOnline(true)
      runQueue()
    }
    const goOffline = () => setOnline(false)

    window.addEventListener('online', goOnline)
    window.addEventListener('offline', goOffline)
    runQueue()

    return () => {
      window.removeEventListener('online', goOnline)
      window.removeEventListener('offline', goOffline)
    }
  }, [runQueue])

  const enqueue = useCallback(
    (kind, payload) => {
      commit([...itemsRef.current, createItem(kind, payload)])
      runQueue()
    },
    [commit, runQueue],
  )

  const discard = useCallback((id) => commit(itemsRef.current.filter((i) => i.id !== id)), [commit])

  const retryManually = useCallback(
    (id) => {
      commit(itemsRef.current.map((i) => (i.id === id ? { ...i, status: 'queued', attempts: 0, error: undefined } : i)))
      runQueue()
    },
    [commit, runQueue],
  )

  return { items, online, enqueue, discard, retryManually }
}
