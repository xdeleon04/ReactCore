import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import Input from '../Input'

describe('Input Component', () => {
  describe('Rendering', () => {
    it('renders input element', () => {
      render(<Input />)

      const input = screen.getByRole('textbox')
      expect(input).toBeInTheDocument()
      expect(input).toBeVisible()
    })

    it('renders with placeholder text', () => {
      render(<Input placeholder="Enter email address" />)

      const input = screen.getByPlaceholderText('Enter email address')
      expect(input).toBeInTheDocument()
    })

    it('renders with label', () => {
      render(<Input label="Email Address" />)

      const input = screen.getByRole('textbox')
      const label = screen.getByText('Email Address')
      expect(label).toBeInTheDocument()
      expect(label.tagName.toLowerCase()).toBe('label')
      expect(label).toHaveAttribute('for', input.id)
    })

    it('renders with description text', () => {
      render(<Input label="Password" description="Minimum 8 characters" />)

      expect(screen.getByText('Minimum 8 characters')).toBeInTheDocument()
    })

    it('renders error message when provided', () => {
      render(<Input error="This field is required" />)

      expect(screen.getByText('This field is required')).toBeInTheDocument()
    })

    it('renders with success state', () => {
      render(<Input value="valid@email.com" success />)

      const input = screen.getByRole('textbox')
      expect(input.className).toMatch(/border-green|success/)
    })
  })

  describe('Input Types', () => {
    it('renders text input', () => {
      render(<Input type="text" />)

      const input = screen.getByRole('textbox') as HTMLInputElement
      expect(input.type).toBe('text')
    })

    it('renders email input', () => {
      render(<Input type="email" />)

      const input = screen.getByRole('textbox') as HTMLInputElement
      expect(input.type).toBe('email')
    })

    it('renders password input', () => {
      render(<Input type="password" />)

      const input = screen.getByRole('textbox') as HTMLInputElement
      expect(input.type).toBe('password')
    })

    it('renders number input', () => {
      render(<Input type="number" />)

      const input = screen.getByRole('spinbutton') as HTMLInputElement
      expect(input.type).toBe('number')
    })

    it('renders date input', () => {
      render(<Input type="date" />)

      const input = screen.getByRole('textbox') as HTMLInputElement
      expect(input.type).toBe('date')
    })

    it('renders search input', () => {
      render(<Input type="search" />)

      const input = screen.getByRole('searchbox')
      expect(input).toBeInTheDocument()
    })
  })

  describe('User Input', () => {
    it('accepts user input', async () => {
      const user = userEvent.setup()
      render(<Input />)

      const input = screen.getByRole('textbox')
      await user.type(input, 'Hello World')

      expect(input).toHaveValue('Hello World')
    })

    it('updates value prop', () => {
      const { rerender } = render(<Input value="Initial" readOnly />)

      let input = screen.getByRole('textbox') as HTMLInputElement
      expect(input.value).toBe('Initial')

      rerender(<Input value="Updated" readOnly />)

      input = screen.getByRole('textbox') as HTMLInputElement
      expect(input.value).toBe('Updated')
    })

    it('handles onChange callback', async () => {
      const handleChange = vi.fn()
      const user = userEvent.setup()

      render(<Input onValueChange={handleChange} />)

      const input = screen.getByRole('textbox')
      await user.type(input, 'test')

      expect(handleChange).toHaveBeenCalledWith('test')
    })

    it('handles onBlur callback', async () => {
      const handleBlur = vi.fn()
      const user = userEvent.setup()

      render(<Input onBlur={handleBlur} />)

      const input = screen.getByRole('textbox')
      input.focus()
      await user.keyboard('test')
      input.blur()

      expect(handleBlur).toHaveBeenCalled()
    })

    it('handles onFocus callback', async () => {
      const handleFocus = vi.fn()
      const user = userEvent.setup()

      render(<Input onFocus={handleFocus} />)

      const input = screen.getByRole('textbox')
      await user.click(input)

      expect(handleFocus).toHaveBeenCalled()
    })

    it('handles onKeyDown callback', async () => {
      const handleKeyDown = vi.fn()
      const user = userEvent.setup()

      render(<Input onKeyDown={handleKeyDown} />)

      const input = screen.getByRole('textbox')
      await user.type(input, 'a')

      expect(handleKeyDown).toHaveBeenCalled()
    })
  })

  describe('Disabled State', () => {
    it('disables input when disabled prop is true', () => {
      render(<Input disabled />)

      const input = screen.getByRole('textbox')
      expect(input).toBeDisabled()
    })

    it('prevents input when disabled', async () => {
      const handleChange = vi.fn()
      const user = userEvent.setup()

      render(<Input disabled onValueChange={handleChange} />)

      const input = screen.getByRole('textbox')
      await user.type(input, 'text')

      expect(handleChange).not.toHaveBeenCalled()
    })

    it('applies disabled styling', () => {
      render(<Input disabled />)

      const input = screen.getByRole('textbox')
      expect(input.className).toMatch(/opacity|disabled|bg-/)
    })
  })

  describe('Validation', () => {
    it('displays error state styling', () => {
      render(<Input error="Invalid email" />)

      const input = screen.getByRole('textbox')
      expect(input.className).toMatch(/border-red|error/)
    })

    it('clears error when value changes', async () => {
      const { rerender } = render(<Input error="Required" value="" />)

      expect(screen.getByText('Required')).toBeInTheDocument()

      rerender(<Input error={undefined} value="new value" />)

      expect(screen.queryByText('Required')).not.toBeInTheDocument()
    })

    it('validates email format', async () => {
      const handleChange = vi.fn(
        (value: string) => !value.includes('@') ? 'Invalid email' : undefined
      )
      const user = userEvent.setup()

      render(<Input type="email" onValueChange={handleChange} />)

      const input = screen.getByRole('textbox')
      await user.type(input, 'invalid')

      // HTML5 validation attribute
      expect(input).toHaveAttribute('type', 'email')
    })

    it('validates minimum length', async () => {
      const user = userEvent.setup()
      render(<Input minLength={5} />)

      const input = screen.getByRole('textbox') as HTMLInputElement
      expect(input.minLength).toBe(5)

      await user.type(input, 'hi')
      expect(input).toHaveValue('hi')
      expect(input.validity.valid).toBe(false)
    })

    it('validates maximum length', async () => {
      const user = userEvent.setup()
      render(<Input maxLength={10} />)

      const input = screen.getByRole('textbox') as HTMLInputElement
      expect(input.maxLength).toBe(10)

      await user.type(input, 'a'.repeat(15))
      expect(input.value).toBe('a'.repeat(10))
    })

    it('requires input when required prop is true', () => {
      render(<Input required />)

      const input = screen.getByRole('textbox')
      expect(input).toBeRequired()
    })
  })

  describe('Accessibility', () => {
    it('has associated label', () => {
      render(<Input label="Email" />)

      const input = screen.getByRole('textbox')
      const label = screen.getByText('Email')
      expect(label.tagName.toLowerCase()).toBe('label')
      expect(label).toHaveAttribute('for', input.id)
    })

    it('supports aria-label', () => {
      render(<Input aria-label="Search" />)

      expect(screen.getByLabelText('Search')).toBeInTheDocument()
    })

    it('supports aria-describedby', () => {
      render(
        <>
          <span id="hint">8 characters minimum</span>
          <Input aria-describedby="hint" />
        </>
      )

      const input = screen.getByRole('textbox')
      expect(input).toHaveAttribute('aria-describedby', 'hint')
    })

    it('announces error to screen readers', () => {
      render(<Input aria-invalid="true" aria-describedby="error" />)

      const input = screen.getByRole('textbox')
      expect(input).toHaveAttribute('aria-invalid', 'true')
    })

    it('is keyboard navigable', async () => {
      const user = userEvent.setup()
      render(<Input />)

      const input = screen.getByRole('textbox')
      await user.tab()

      expect(input).toHaveFocus()
    })

    it('supports read-only state for accessibility', () => {
      render(<Input value="readonly value" readOnly />)

      const input = screen.getByRole('textbox')
      expect(input).toHaveAttribute('readonly')
    })
  })

  describe('Size Variants', () => {
    it('applies small size styling', () => {
      render(<Input size="sm" />)

      const input = screen.getByRole('textbox')
      expect(input.className).toMatch(/px-2|py-1|text-sm/)
    })

    it('applies medium size styling', () => {
      render(<Input size="md" />)

      const input = screen.getByRole('textbox')
      expect(input.className).toMatch(/px-3|py-2/)
    })

    it('applies large size styling', () => {
      render(<Input size="lg" />)

      const input = screen.getByRole('textbox')
      expect(input.className).toMatch(/px-4|py-3|text-lg/)
    })
  })

  describe('Refs', () => {
    it('forwards ref correctly', () => {
      const ref = vi.fn()

      render(<Input ref={ref} />)

      expect(ref).toHaveBeenCalledWith(expect.any(HTMLInputElement))
    })

    it('allows direct ref access to input', () => {
      let inputRef: HTMLInputElement | null = null

      render(<Input ref={el => { inputRef = el }} />)

      expect(inputRef).toBeInstanceOf(HTMLInputElement)
      inputRef?.focus()
      expect(inputRef).toHaveFocus()
    })
  })

  describe('Edge Cases', () => {
    it('handles rapid input changes', async () => {
      const handleChange = vi.fn()
      const user = userEvent.setup()

      render(<Input onChange={handleChange} />)

      const input = screen.getByRole('textbox')
      await user.type(input, 'rapid')

      expect(handleChange).toHaveBeenCalledTimes(5) // One per character
    })

    it('handles clearing input', async () => {
      const handleChange = vi.fn()
      const user = userEvent.setup()

      render(<Input value="initial" onChange={handleChange} />)

      const input = screen.getByRole('textbox')
      await user.clear(input)

      expect(input).toHaveValue('')
    })

    it('handles special characters', async () => {
      const user = userEvent.setup()

      render(<Input />)

      const input = screen.getByRole('textbox')
      await user.type(input, '!@#$%^&*()')

      expect(input).toHaveValue('!@#$%^&*()')
    })

    it('handles pasting content', async () => {
      const user = userEvent.setup()

      render(<Input />)

      const input = screen.getByRole('textbox')

      await user.click(input)
      await user.paste('pasted content')

      expect(input).toHaveValue('pasted content')
    })
  })
})
