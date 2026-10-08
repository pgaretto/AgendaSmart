import { describe, expect, it, vi } from 'vitest'
import { backoffDelay, createItem, isRecoverable, loadItems, MAX_ATTEMPTS, outboxKey, retryItem, saveItems, userIdFromToken } from './outbox'

const networkError = () => Object.assign(new Error('red'), { isNetwork: true })
const wait = vi.fn(async () => {})

describe('retryItem', () => {
  it('reintenta hasta 5 veces por falla de red y luego pasa a manual', async () => {
    const send = vi.fn().mockRejectedValue(networkError())

    const result = await retryItem(createItem('entry', { a: 1 }), { send, wait })

    expect(send).toHaveBeenCalledTimes(MAX_ATTEMPTS)
    expect(result.status).toBe('manual')
    expect(result.attempts).toBe(5)
  })

  it('termina en cuanto un reintento funciona', async () => {
    const send = vi.fn().mockRejectedValueOnce(networkError()).mockRejectedValueOnce(networkError()).mockResolvedValue({})

    const result = await retryItem(createItem('entry', {}), { send, wait })

    expect(send).toHaveBeenCalledTimes(3)
    expect(result.status).toBe('done')
    expect(result.attempts).toBe(3)
  })

  it('una frase interpretada queda lista para confirmar y no se guarda sola', async () => {
    const proposal = { event: null, expense: { amount: 100, category: 'Comida' } }

    const result = await retryItem(createItem('text', { text: 'x' }), { send: async () => proposal, wait })

    expect(result.status).toBe('ready')
    expect(result.result).toEqual(proposal)
  })

  it('si el servidor rechaza (no es falla de red) no reintenta y queda manual', async () => {
    const send = vi.fn().mockRejectedValue(Object.assign(new Error('mal'), { status: 400 }))

    const result = await retryItem(createItem('entry', {}), { send, wait })

    expect(send).toHaveBeenCalledTimes(1)
    expect(result.status).toBe('manual')
  })

  it('con la sesión vencida (401) pausa el envío sin gastar intentos', async () => {
    const send = vi.fn().mockRejectedValue(Object.assign(new Error('401'), { status: 401 }))

    const result = await retryItem({ ...createItem('entry', {}), attempts: 2 }, { send, wait })

    expect(send).toHaveBeenCalledTimes(1)
    expect(result).toMatchObject({ status: 'queued', error: 'sesion', attempts: 2 })
  })

  it('ante un 429 espera lo que pide Retry-After y reintenta', async () => {
    const w = vi.fn(async () => {})
    const limited = Object.assign(new Error('429'), { status: 429, retryAfter: 10 })
    const send = vi.fn().mockRejectedValueOnce(limited).mockResolvedValue({})

    const result = await retryItem(createItem('entry', {}), { send, wait: w })

    expect(w).toHaveBeenCalledWith(10000)
    expect(result.status).toBe('done')
  })

  it('si el límite persiste tras 5 intentos queda manual con motivo "limite"', async () => {
    const send = vi.fn().mockRejectedValue(Object.assign(new Error('429'), { status: 429 }))

    const result = await retryItem(createItem('entry', {}), { send, wait })

    expect(send).toHaveBeenCalledTimes(MAX_ATTEMPTS)
    expect(result).toMatchObject({ status: 'manual', error: 'limite' })
  })

  it('espera con backoff exponencial entre intentos y no espera tras el último', async () => {
    const w = vi.fn(async () => {})

    await retryItem(createItem('entry', {}), { send: async () => Promise.reject(networkError()), wait: w })

    expect(w.mock.calls.map(([ms]) => ms)).toEqual([1000, 2000, 4000, 8000])
    expect(backoffDelay(1)).toBe(1000)
  })

  it('retoma desde los intentos ya consumidos y reporta cada intento fallido', async () => {
    const onProgress = vi.fn()
    const send = vi.fn().mockRejectedValue(networkError())

    const result = await retryItem({ ...createItem('entry', {}), attempts: 3 }, { send, wait, onProgress })

    expect(send).toHaveBeenCalledTimes(2)
    expect(onProgress.mock.calls.map(([i]) => i.attempts)).toEqual([4, 5])
    expect(result.status).toBe('manual')
  })
})

describe('almacenamiento', () => {
  const memoryStorage = () => {
    const data = new Map()
    return { getItem: (k) => data.get(k) ?? null, setItem: (k, v) => data.set(k, v) }
  }

  it('guarda y recupera los items de cada usuario por separado', () => {
    const storage = memoryStorage()
    const fede = createItem('entry', { u: 'fede' })

    saveItems(storage, outboxKey('1'), [fede])

    expect(loadItems(storage, outboxKey('1'))).toEqual([fede])
    expect(loadItems(storage, outboxKey('2'))).toEqual([])
  })

  it('tolera contenido corrupto', () => {
    const storage = memoryStorage()
    storage.setItem(outboxKey('1'), '{no es json')

    expect(loadItems(storage, outboxKey('1'))).toEqual([])
  })

  it('obtiene el id de usuario del claim sub del JWT', () => {
    const payload = btoa(JSON.stringify({ sub: '42', email: 'a@b.com' }))

    expect(userIdFromToken(`h.${payload}.s`)).toBe('42')
    expect(userIdFromToken('basura')).toBeNull()
    expect(userIdFromToken(null)).toBeNull()
  })
})

describe('isRecoverable', () => {
  it('guarda para reintentar solo si es falla de red o sesión vencida', () => {
    expect(isRecoverable(Object.assign(new Error(), { isNetwork: true }))).toBe(true)
    expect(isRecoverable(Object.assign(new Error(), { status: 401 }))).toBe(true)
    expect(isRecoverable(Object.assign(new Error(), { status: 400 }))).toBe(false)
    expect(isRecoverable(Object.assign(new Error(), { status: 502 }))).toBe(false)
  })
})
