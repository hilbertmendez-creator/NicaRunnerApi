import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { PositionBadge } from '../components/PositionBadge'

describe('PositionBadge', () => {
  it('renders a medal svg alongside the number for podium positions 1-3', () => {
    // Escenario: Podium position renders a medal
    for (const position of [1, 2, 3] as const) {
      const { container, unmount } = render(<PositionBadge position={position} />)
      expect(screen.getByText(String(position))).toBeInTheDocument()
      expect(container.querySelector('svg')).not.toBeNull()
      expect(container.querySelector('.pos-cell')).not.toBeNull()
      unmount()
    }
  })

  it('renders only the number, no medal, for positions greater than 3', () => {
    // Escenario: Non-podium position renders number only
    const { container } = render(<PositionBadge position={7} />)
    expect(screen.getByText('7')).toBeInTheDocument()
    expect(container.querySelector('svg')).toBeNull()
    expect(container.querySelector('.pos-cell')).toBeNull()
  })

  it.each([
    ['null', null],
    ['undefined', undefined],
    ['zero', 0],
    ['negative', -1],
  ])('renders a plain dash with no number and no medal when position is %s', (_label, position) => {
    // Escenarios: DNF runner shows a dash / Null position shows a dash
    // (resolved decision 4: this short-circuits before the >3 fallback path)
    const { container } = render(<PositionBadge position={position as number | null | undefined} />)
    expect(screen.getByText('—')).toBeInTheDocument()
    expect(container.querySelector('svg')).toBeNull()
    expect(container.querySelector('.pos-num')).toBeNull()
  })
})
