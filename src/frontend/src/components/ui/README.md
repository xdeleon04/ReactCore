# UI Components (shadcn-style)

These components are local, Tailwind-styled primitives intended to be reused across pages.

## Components

- `Button` ([button.tsx](button.tsx))
- `Card`, `CardHeader`, `CardContent`, `CardFooter` ([card.tsx](card.tsx))
- `Input` ([input.tsx](input.tsx))
- `Dialog` primitives ([dialog.tsx](dialog.tsx))
- `ToastProvider` ([ToastContext.tsx](../../context/ToastContext.tsx)) + `useToast` ([useToast.ts](../../hooks/useToast.ts))

## Usage

### Button

```tsx
import { Button } from '@/components/ui/button'

<Button>Save</Button>
<Button variant="outline">Cancel</Button>
<Button isLoading loadingText="Saving…">Save</Button>
```

### Card

```tsx
import { Card, CardHeader, CardTitle, CardContent, CardFooter } from '@/components/ui/card'

<Card>
	<CardHeader>
		<CardTitle>Title</CardTitle>
	</CardHeader>
	<CardContent>Content</CardContent>
	<CardFooter>Footer actions</CardFooter>
</Card>
```

### Input

```tsx
import { Input } from '@/components/ui/input'

<label htmlFor="email">Email</label>
<Input id="email" type="email" autoComplete="email" />
```

### Dialog

```tsx
import {
	Dialog,
	DialogTrigger,
	DialogContent,
	DialogHeader,
	DialogTitle,
	DialogDescription,
	DialogFooter,
} from '@/components/ui/dialog'

<Dialog>
	<DialogTrigger asChild>
		<button type="button">Open</button>
	</DialogTrigger>
	<DialogContent>
		<DialogHeader>
			<DialogTitle>Title</DialogTitle>
			<DialogDescription>Description</DialogDescription>
		</DialogHeader>
		<DialogFooter>{/* actions */}</DialogFooter>
	</DialogContent>
</Dialog>
```

### Toasts

```tsx
import { useToast } from '@/hooks/useToast'

const toast = useToast()

toast.success({ title: 'Saved' })
toast.error({ title: 'Failed', description: 'Please try again.' })
```
