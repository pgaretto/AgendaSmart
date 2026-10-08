import { describe, expect, it } from 'vitest'
import { decodePayload, isExpired } from './jwt'

const token = (payload) => `h.${btoa(JSON.stringify(payload))}.s`

describe('jwt', () => {
  it('detecta un token vencido y uno vigente', () => {
    const now = Date.UTC(2026, 9, 8, 12, 0, 0)
    const secs = (ms) => Math.floor(ms / 1000)

    expect(isExpired(token({ exp: secs(now) - 1 }), now)).toBe(true)
    expect(isExpired(token({ exp: secs(now) + 60 }), now)).toBe(false)
  })

  it('trata como vencido un token ilegible o sin exp', () => {
    expect(isExpired('basura')).toBe(true)
    expect(isExpired(null)).toBe(true)
    expect(isExpired(token({ sub: '1' }))).toBe(true)
  })

  it('decodifica el payload', () => {
    expect(decodePayload(token({ sub: '42' })).sub).toBe('42')
  })
})
