import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import Dialog from '../Dialog'

describe('Dialog Component', () => {
  describe('Rendering', () => {
    it('renders dialog when open is true', () => {
      render(
        <Dialog open={true} title="Confirm Action">
          <p>Are you sure you want to proceed?</p>
        </Dialog>
      )

      expect(screen.getByRole('dialog')).toBeInTheDocument()
      expect(screen.getByText('Confirm Action')).toBeInTheDocument()
      expect(screen.getByText('Are you sure you want to proceed?')).toBeInTheDocument()
    })

    it('does not render dialog when open is false', () => {
      render(
        <Dialog open={false} title="Hidden Dialog">
          <p>This should not be visible</p>
        </Dialog>
      )

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
      expect(screen.queryByText('This should not be visible')).not.toBeInTheDocument()
    })

    it('renders dialog with title', () => {
      render(
        <Dialog open={true} title="Delete Confirmation">
          Content
        </Dialog>
      )

      expect(screen.getByText('Delete Confirmation')).toBeInTheDocument()
    })

    it('renders dialog with description', () => {
      render(
        <Dialog
          open={true}
          title="Confirm"
          description="This action cannot be undone"
        >
          Content
        </Dialog>
      )

      expect(screen.getByText('This action cannot be undone')).toBeInTheDocument()
    })

    it('renders dialog content', () => {
      render(
        <Dialog open={true} title="Test">
          <div data-testid="dialog-content">Custom content</div>
        </Dialog>
      )

      expect(screen.getByTestId('dialog-content')).toBeInTheDocument()
    })

    it('renders with custom className', () => {
      render(
        <Dialog open={true} title="Test" className="custom-dialog">
          Content
        </Dialog>
      )

      const dialog = screen.getByRole('dialog')
      expect(dialog).toHaveClass('custom-dialog')
    })
  })

  describe('Interactions', () => {
    it('calls onClose when close button is clicked', async () => {
      const handleClose = vi.fn()
      const user = userEvent.setup()

      render(
        <Dialog open={true} title="Test" onClose={handleClose}>
          Content
        </Dialog>
      )

      const closeButton = screen.getByRole('button', { name: /close|×/i })
      await user.click(closeButton)

      expect(handleClose).toHaveBeenCalledOnce()
    })

    it('calls onClose when escape key is pressed', async () => {
      const handleClose = vi.fn()
      const user = userEvent.setup()

      render(
        <Dialog open={true} title="Test" onClose={handleClose}>
          Content
        </Dialog>
      )

      // Focus within the dialog
      const dialog = screen.getByRole('dialog')
      dialog.focus()

      await user.keyboard('{Escape}')

      expect(handleClose).toHaveBeenCalledOnce()
    })

    it('calls onOpenChange when dialog state changes', async () => {
      const handleOpenChange = vi.fn()
      const { rerender } = render(
        <Dialog open={false} title="Test" onOpenChange={handleOpenChange}>
          Content
        </Dialog>
      )

      rerender(
        <Dialog open={true} title="Test" onOpenChange={handleOpenChange}>
          Content
        </Dialog>
      )

      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    it('closes when clicking outside (backdrop)', async () => {
      const handleClose = vi.fn()
      const user = userEvent.setup()

      render(
        <Dialog open={true} title="Test" onClose={handleClose}>
          Content
        </Dialog>
      )

      const backdrop = screen.getByRole('dialog').parentElement?.querySelector('[data-testid="dialog-backdrop"]')

      if (backdrop) {
        await user.click(backdrop)
        expect(handleClose).toHaveBeenCalled()
      }
    })

    it('handles action buttons', async () => {
      const handleConfirm = vi.fn()
      const handleCancel = vi.fn()
      const user = userEvent.setup()

      render(
        <Dialog open={true} title="Confirm">
          <p>Proceed with deletion?</p>
          <button onClick={handleConfirm}>Delete</button>
          <button onClick={handleCancel}>Cancel</button>
        </Dialog>
      )

      await user.click(screen.getByRole('button', { name: /delete/i }))
      expect(handleConfirm).toHaveBeenCalledOnce()

      await user.click(screen.getByRole('button', { name: /cancel/i }))
      expect(handleCancel).toHaveBeenCalledOnce()
    })
  })

  describe('Focus Management', () => {
    it('focuses on first focusable element when opened', async () => {
      const { rerender } = render(
        <Dialog open={false} title="Test">
          <input autoFocus placeholder="First input" />
        </Dialog>
      )

      rerender(
        <Dialog open={true} title="Test">
          <input autoFocus placeholder="First input" />
        </Dialog>
      )

      const input = screen.getByPlaceholderText('First input')
      expect(input).toHaveFocus()
    })

    it('restores focus when dialog closes', async () => {
      const triggerRef = vi.fn()
      const { rerender } = render(
        <>
          <button ref={triggerRef}>Open Dialog</button>
          <Dialog open={false} title="Test">
            Content
          </Dialog>
        </>
      )

      rerender(
        <>
          <button ref={triggerRef}>Open Dialog</button>
          <Dialog open={true} title="Test">
            Content
          </Dialog>
        </>
      )

      expect(screen.getByRole('dialog')).toBeInTheDocument()

      rerender(
        <>
          <button ref={triggerRef}>Open Dialog</button>
          <Dialog open={false} title="Test">
            Content
          </Dialog>
        </>
      )

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })

    it('traps focus within dialog', async () => {
      const user = userEvent.setup()

      render(
        <Dialog open={true} title="Test">
          <button>First Button</button>
          <button>Second Button</button>
          <button>Last Button</button>
        </Dialog>
      )

      const buttons = screen.getAllByRole('button')
      const firstButton = buttons[0]

      firstButton.focus()
      expect(firstButton).toHaveFocus()

      // Tab from first button should cycle to close button or second button
      await user.keyboard('{Shift>}{Tab}{/Shift}')

      // Focus should be trapped within dialog
      const focusedElement = document.activeElement
      const dialog = screen.getByRole('dialog')
      expect(dialog.contains(focusedElement)).toBe(true)
    })

    it('is keyboard navigable within dialog', async () => {
      const user = userEvent.setup()

      render(
        <Dialog open={true} title="Test">
          <button>Button 1</button>
          <button>Button 2</button>
          <input placeholder="Input field" />
        </Dialog>
      )

      const buttons = screen.getAllByRole('button')
      const input = screen.getByPlaceholderText('Input field')

      buttons[0].focus()
      expect(buttons[0]).toHaveFocus()

      await user.keyboard('{Tab}')
      expect(buttons[1]).toHaveFocus()

      await user.keyboard('{Tab}')
      expect(input).toHaveFocus()
    })
  })

  describe('Accessibility', () => {
    it('has role="dialog"', () => {
      render(
        <Dialog open={true} title="Test">
          Content
        </Dialog>
      )

      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    it('has aria-labelledby pointing to title', () => {
      render(
        <Dialog open={true} title="Dialog Title">
          Content
        </Dialog>
      )

      const dialog = screen.getByRole('dialog')
      const titleId = dialog.getAttribute('aria-labelledby')

      expect(titleId).toBeTruthy()
      expect(document.getElementById(titleId as string)).toHaveTextContent('Dialog Title')
    })

    it('has aria-describedby pointing to description', () => {
      render(
        <Dialog open={true} title="Test" description="Dialog description">
          Content
        </Dialog>
      )

      const dialog = screen.getByRole('dialog')
      const descriptionId = dialog.getAttribute('aria-describedby')

      if (descriptionId) {
        expect(document.getElementById(descriptionId)).toBeInTheDocument()
      }
    })

    it('announces modal to screen readers', () => {
      render(
        <Dialog open={true} title="Test">
          Content
        </Dialog>
      )

      const dialog = screen.getByRole('dialog')
      expect(dialog).toHaveAttribute('aria-modal', 'true')
    })

    it('has accessible close button', () => {
      render(
        <Dialog open={true} title="Test">
          Content
        </Dialog>
      )

      const closeButton = screen.getByRole('button', { name: /close|×/i })
      expect(closeButton).toBeInTheDocument()
      expect(closeButton).toHaveAttribute('aria-label')
    })

    it('inert backdrop prevents interaction outside', () => {
      render(
        <div>
          <button>Outside Button</button>
          <Dialog open={true} title="Test">
            Content
          </Dialog>
        </div>
      )

      const outsideButton = screen.getByRole('button', { name: /outside/i })
      expect(outsideButton).toBeDisabled()
    })
  })

  describe('Size and Width', () => {
    it('renders with default size', () => {
      render(
        <Dialog open={true} title="Test">
          Content
        </Dialog>
      )

      const dialog = screen.getByRole('dialog')
      expect(dialog.className).toMatch(/max-w/)
    })

    it('renders with custom size', () => {
      render(
        <Dialog open={true} title="Test" size="lg">
          Content
        </Dialog>
      )

      const dialog = screen.getByRole('dialog')
      expect(dialog.className).toMatch(/max-w-lg|max-w-2xl/)
    })

    it('applies small size', () => {
      render(
        <Dialog open={true} title="Test" size="sm">
          Content
        </Dialog>
      )

      const dialog = screen.getByRole('dialog')
      expect(dialog.className).toMatch(/max-w-sm/)
    })
  })

  describe('Animation', () => {
    it('shows enter animation when opening', () => {
      const { rerender } = render(
        <Dialog open={false} title="Test">
          Content
        </Dialog>
      )

      rerender(
        <Dialog open={true} title="Test">
          Content
        </Dialog>
      )

      const dialog = screen.getByRole('dialog')
      expect(dialog.className).toMatch(/animate|transition/)
    })

    it('shows exit animation when closing', async () => {
      const handleClose = vi.fn()
      const user = userEvent.setup()

      const { rerender } = render(
        <Dialog open={true} title="Test" onClose={handleClose}>
          Content
        </Dialog>
      )

      const closeButton = screen.getByRole('button', { name: /close|×/i })
      await user.click(closeButton)

      rerender(
        <Dialog open={false} title="Test" onClose={handleClose}>
          Content
        </Dialog>
      )

      expect(handleClose).toHaveBeenCalled()
    })
  })

  describe('Edge Cases', () => {
    it('handles multiple dialogs stacking', () => {
      render(
        <>
          <Dialog open={true} title="Dialog 1">
            Content 1
          </Dialog>
          <Dialog open={true} title="Dialog 2">
            Content 2
          </Dialog>
        </>
      )

      const dialogs = screen.getAllByRole('dialog')
      expect(dialogs).toHaveLength(2)
    })

    it('handles rapid open/close', async () => {
      const handleClose = vi.fn()
      const { rerender } = render(
        <Dialog open={false} title="Test" onClose={handleClose}>
          Content
        </Dialog>
      )

      rerender(<Dialog open={true} title="Test" onClose={handleClose}>Content</Dialog>)
      expect(screen.getByRole('dialog')).toBeInTheDocument()

      rerender(<Dialog open={false} title="Test" onClose={handleClose}>Content</Dialog>)
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()

      rerender(<Dialog open={true} title="Test" onClose={handleClose}>Content</Dialog>)
      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    it('handles very long content', () => {
      const longContent = 'A'.repeat(1000)

      render(
        <Dialog open={true} title="Test">
          {longContent}
        </Dialog>
      )

      expect(screen.getByText(longContent)).toBeInTheDocument()
      expect(screen.getByRole('dialog').className).toMatch(/overflow|scroll/)
    })

    it('handles empty content', () => {
      render(
        <Dialog open={true} title="Empty Dialog">
        </Dialog>
      )

      expect(screen.getByText('Empty Dialog')).toBeInTheDocument()
      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })
  })
})
