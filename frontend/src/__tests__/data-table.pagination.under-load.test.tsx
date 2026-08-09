import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { DataTable } from '@nicarunner/ui'
import type { Column } from '@nicarunner/ui'

type Row = { id: number; name: string }

const columns: Column<Row>[] = [
  { header: 'ID', render: (r) => r.id },
  { header: 'Nombre', render: (r) => r.name },
]

/** Simulates operator paging a large list (25 rows, pageSize 10 → 3 pages). */
function pageSlice(all: Row[], pageIndex: number, pageSize: number): Row[] {
  const offset = (pageIndex - 1) * pageSize
  return all.slice(offset, offset + pageSize)
}

function expectRowVisible(name: string) {
  expect(screen.getAllByText(name).length).toBeGreaterThan(0)
}

function expectRowAbsent(name: string) {
  expect(screen.queryAllByText(name)).toHaveLength(0)
}

describe('DataTable pagination under load', () => {
  const pageSize = 10
  const all = Array.from({ length: 25 }, (_, i) => ({
    id: i + 1,
    name: `User ${i + 1}`,
  }))
  const pageCount = Math.ceil(all.length / pageSize)

  it('starts at 1-based page 1 with first slice and offset 0', () => {
    const onPageChange = vi.fn()
    const pageIndex = 1
    render(
      <DataTable
        columns={columns}
        data={pageSlice(all, pageIndex, pageSize)}
        rowKey={(r) => r.id}
        pageIndex={pageIndex}
        pageCount={pageCount}
        onPageChange={onPageChange}
      />,
    )

    expect(screen.getByText(/Página/)).toBeInTheDocument()
    expectRowVisible('User 1')
    expectRowVisible('User 10')
    expectRowAbsent('User 11')
    expect((pageIndex - 1) * pageSize).toBe(0)

    screen.getAllByRole('button', { name: /Anterior/i }).forEach((btn) => {
      expect(btn).toBeDisabled()
    })
  })

  it('advances with Siguiente and disables Next on last page (no thrash)', async () => {
    const user = userEvent.setup()
    let pageIndex = 1
    const onPageChange = vi.fn((p: number) => {
      pageIndex = p
    })

    const { rerender } = render(
      <DataTable
        columns={columns}
        data={pageSlice(all, pageIndex, pageSize)}
        rowKey={(r) => r.id}
        pageIndex={pageIndex}
        pageCount={pageCount}
        onPageChange={onPageChange}
      />,
    )

    await user.click(screen.getAllByRole('button', { name: /Siguiente/i })[0])
    expect(onPageChange).toHaveBeenCalledWith(2)
    pageIndex = 2
    rerender(
      <DataTable
        columns={columns}
        data={pageSlice(all, pageIndex, pageSize)}
        rowKey={(r) => r.id}
        pageIndex={pageIndex}
        pageCount={pageCount}
        onPageChange={onPageChange}
      />,
    )
    expectRowVisible('User 11')
    expectRowAbsent('User 1')
    expect((pageIndex - 1) * pageSize).toBe(10)

    await user.click(screen.getAllByRole('button', { name: /Siguiente/i })[0])
    expect(onPageChange).toHaveBeenCalledWith(3)
    pageIndex = 3
    rerender(
      <DataTable
        columns={columns}
        data={pageSlice(all, pageIndex, pageSize)}
        rowKey={(r) => r.id}
        pageIndex={pageIndex}
        pageCount={pageCount}
        onPageChange={onPageChange}
      />,
    )
    expectRowVisible('User 21')
    expectRowVisible('User 25')
    expect((pageIndex - 1) * pageSize).toBe(20)

    screen.getAllByRole('button', { name: /Siguiente/i }).forEach((btn) => {
      expect(btn).toBeDisabled()
    })

    const nav = screen.getByLabelText('Pagination')
    await user.click(within(nav).getByRole('button', { name: '1' }))
    expect(onPageChange).toHaveBeenCalledWith(1)
  })
})

describe('DataTable wrapperClassName', () => {
  const data: Row[] = [{ id: 1, name: 'User 1' }]

  function desktopWrapper(container: HTMLElement) {
    // Único elemento con la clase table-scroll: el contenedor de escritorio (sm:block).
    return container.querySelector('.table-scroll') as HTMLElement
  }

  it('is additive: omitting wrapperClassName leaves the default wrapper classes unchanged', () => {
    // Escenario: Existing DataTable callers compile unchanged
    const { container } = render(<DataTable columns={columns} data={data} rowKey={(r) => r.id} />)

    const classes = desktopWrapper(container).className.trim().split(/\s+/)
    expect(classes).toEqual(['table-scroll', 'hidden', 'overflow-x-auto', 'sm:block'])
  })

  it('appends the supplied class rather than replacing the default wrapper classes', () => {
    const { container } = render(
      <DataTable columns={columns} data={data} rowKey={(r) => r.id} wrapperClassName="no-stagger" />,
    )

    const classes = desktopWrapper(container).className.trim().split(/\s+/)
    expect(classes).toEqual(['table-scroll', 'hidden', 'overflow-x-auto', 'sm:block', 'no-stagger'])
  })
})
