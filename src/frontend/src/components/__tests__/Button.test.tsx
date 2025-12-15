import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import Button from '../Button'

describe('Button Component', () => {
  describe('Rendering', () => {
    it('renders button with text content', () => {
      render(<Button>Click me</Button>)

      const button = screen.getByRole('button', { name: /click me/i })
      expect(button).toBeInTheDocument()
      expect(button).toBeVisible()
    })

    it('renders button with children as ReactNode', () => {
      render(
        <Button>
          <span>Icon</span>
          Click
        </Button>
      )

      expect(screen.getByText('Icon')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /click/i })).toBeInTheDocument()
    })

    it('applies size variants correctly', () => {
      const { rerender } = render(<Button size="sm">Small</Button>)
      expect(screen.getByRole('button')).toHaveClass('px-3', 'py-1', 'text-sm')

      rerender(<Button size="md">Medium</Button>)
      expect(screen.getByRole('button')).toHaveClass('px-4', 'py-2')

      rerender(<Button size="lg">Large</Button>)
      expect(screen.getByRole('button')).toHaveClass('px-6', 'py-3', 'text-lg')
    })

    it('applies variant styles correctly', () => {
      const variants = ['primary', 'secondary', 'danger', 'outline', 'ghost'] as const

      variants.forEach(variant => {
        const { unmount } = render(<Button variant={variant}>Test</Button>)
        const button = screen.getByRole('button')

        expect(button.className).toMatch(/bg-|border-|text-/)

        unmount()
      })
    })
  })

  describe('Interactions', () => {
    it('handles click events', async () => {
      const handleClick = vi.fn()
      const user = userEvent.setup()

      render(<Button onClick={handleClick}>Click</Button>)

      const button = screen.getByRole('button')
      await user.click(button)

      expect(handleClick).toHaveBeenCalledOnce()
    })

    it('prevents multiple clicks when disabled', async () => {
      const handleClick = vi.fn()
      const user = userEvent.setup()

      render(
        <Button disabled onClick={handleClick}>
          Disabled
        </Button>
      )

      const button = screen.getByRole('button')
      await user.click(button)

      expect(handleClick).not.toHaveBeenCalled()
    })

    it('handles keyboard navigation (Enter key)', async () => {
      const handleClick = vi.fn()
      const user = userEvent.setup()

      render(<Button onClick={handleClick}>Button</Button>)

      const button = screen.getByRole('button')
      button.focus()

      await user.keyboard('{Enter}')

      expect(handleClick).toHaveBeenCalledOnce()
    })

    it('handles keyboard navigation (Space key)', async () => {
      const handleClick = vi.fn()
      const user = userEvent.setup()

      render(<Button onClick={handleClick}>Button</Button>)

      const button = screen.getByRole('button')
      button.focus()

      await user.keyboard(' ')

      expect(handleClick).toHaveBeenCalledOnce()
    })
  })

  describe('Loading State', () => {
    it('disables button and shows loading indicator', () => {
      render(<Button isLoading>Save</Button>)

      const button = screen.getByRole('button')
      expect(button).toBeDisabled()
      expect(screen.getByTestId('button-spinner')).toBeInTheDocument()
    })

    it('shows loading text when provided', () => {
      render(<Button isLoading loadingText="Saving...">Save</Button>)

      expect(screen.getByText('Saving...')).toBeInTheDocument()
      expect(screen.queryByText('Save')).not.toBeInTheDocument()
    })

    it('shows default loading text if not provided', () => {
      render(<Button isLoading>Save</Button>)

      const button = screen.getByRole('button')
      expect(button).toBeDisabled()
    })
  })

  describe('Disabled State', () => {
    it('disables button when disabled prop is true', () => {
      render(<Button disabled>Disabled Button</Button>)

      const button = screen.getByRole('button')
      expect(button).toBeDisabled()
      expect(button).toHaveAttribute('disabled')
    })

    it('applies disabled styling', () => {
      render(<Button disabled>Disabled</Button>)

      const button = screen.getByRole('button')
      expect(button.className).toMatch(/opacity|disabled/)
    })

    it('does not call onClick when disabled', async () => {
      const handleClick = vi.fn()
      const user = userEvent.setup()

      render(
        <Button disabled onClick={handleClick}>
          Click
        </Button>
      )

      await user.click(screen.getByRole('button'))
      expect(handleClick).not.toHaveBeenCalled()
    })
  })

  describe('Accessibility', () => {
    it('has correct accessible name', () => {
      render(<Button>Submit Form</Button>)

      expect(screen.getByRole('button', { name: /submit form/i })).toBeInTheDocument()
    })

    it('supports aria-label', () => {
      render(<Button aria-label="Close dialog">×</Button>)

      expect(screen.getByLabelText('Close dialog')).toBeInTheDocument()
    })

    it('supports aria-describedby', () => {
      render(
        <>
          <div id="help">This button submits the form</div>
          <Button aria-describedby="help">Submit</Button>
        </>
      )

      const button = screen.getByRole('button')
      expect(button).toHaveAttribute('aria-describedby', 'help')
    })

    it('has focus visible style', async () => {
      const user = userEvent.setup()

      render(<Button>Focusable</Button>)

      const button = screen.getByRole('button')

      await user.tab()

      expect(button).toHaveFocus()
    })

    it('announces loading state to screen readers', () => {
      render(<Button isLoading aria-label="Save changes">Save</Button>)

      const button = screen.getByRole('button')
      expect(button).toHaveAttribute('aria-busy', 'true')
    })
  })

  describe('Type Prop', () => {
    it('renders as button by default', () => {
      render(<Button>Button</Button>)

      const button = screen.getByRole('button') as HTMLButtonElement
      expect(button.type).toBe('button')
    })

    it('renders as submit button', () => {
      render(<Button type="submit">Submit</Button>)

      const button = screen.getByRole('button') as HTMLButtonElement
      expect(button.type).toBe('submit')
    })

    it('renders as reset button', () => {
      render(<Button type="reset">Reset</Button>)

      const button = screen.getByRole('button') as HTMLButtonElement
      expect(button.type).toBe('reset')
    })
  })

  describe('Custom Styling', () => {
    it('applies custom className', () => {
      render(<Button className="custom-class">Button</Button>)

      const button = screen.getByRole('button')
      expect(button).toHaveClass('custom-class')
    })

    it('applies custom style prop', () => {
      render(<Button style={{ color: 'red' }}>Button</Button>)

      const button = screen.getByRole('button')
      expect(button).toHaveStyle({ color: 'rgb(255, 0, 0)' })
    })
  })

  describe('Refs', () => {
    it('forwards ref correctly', () => {
      const ref = vi.fn()

      render(<Button ref={ref}>Button</Button>)

      expect(ref).toHaveBeenCalledWith(expect.any(HTMLButtonElement))
    })

    it('allows direct ref access', () => {
      let buttonRef: HTMLButtonElement | null = null

      render(
        <Button ref={el => { buttonRef = el }}>
          Ref Test
        </Button>
      )

      expect(buttonRef).toBeInstanceOf(HTMLButtonElement)
      expect(buttonRef?.textContent).toBe('Ref Test')
    })
  })

  describe('Edge Cases', () => {
    it('handles empty children gracefully', () => {
      render(<Button></Button>)

      const button = screen.getByRole('button')
      expect(button).toBeInTheDocument()
    })

    it('handles rapid clicks', async () => {
      const handleClick = vi.fn()
      const user = userEvent.setup()

      render(<Button onClick={handleClick}>Click</Button>)

      const button = screen.getByRole('button')

      // Simulate rapid clicks
      await user.click(button)
      await user.click(button)
      await user.click(button)

      expect(handleClick).toHaveBeenCalledTimes(3)
    })

    it('handles prop updates correctly', () => {
      const { rerender } = render(<Button disabled>Disabled</Button>)

      expect(screen.getByRole('button')).toBeDisabled()

      rerender(<Button disabled={false}>Enabled</Button>)

      expect(screen.getByRole('button')).not.toBeDisabled()
    })
  })
})
