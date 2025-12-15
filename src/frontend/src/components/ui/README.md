# Componentes de UI (estilo shadcn)

Estos componentes son primitivos locales estilizados con Tailwind, pensados para reutilizarse en distintas páginas.

## Componentes

- `Button` ([button.tsx](button.tsx))
- `Card`, `CardHeader`, `CardContent`, `CardFooter` ([card.tsx](card.tsx))
- `Input` ([input.tsx](input.tsx))
- `Dialog` primitives ([dialog.tsx](dialog.tsx))
- `ToastProvider` ([ToastContext.tsx](../../context/ToastContext.tsx)) + `useToast` ([useToast.ts](../../hooks/useToast.ts))

## Uso

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
		<CardTitle>Título</CardTitle>
	</CardHeader>
	<CardContent>Contenido</CardContent>
	<CardFooter>Footer actions</CardFooter>
</Card>
```

### Input

```tsx
import { Input } from '@/components/ui/input'

<label htmlFor="email">Correo</label>
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
			<DialogTitle>Título</DialogTitle>
			<DialogDescription>Descripción</DialogDescription>
		</DialogHeader>
		<DialogFooter>{/* actions */}</DialogFooter>
	</DialogContent>
</Dialog>
```

### Toasts

```tsx
import { useToast } from '@/hooks/useToast'

const toast = useToast()

toast.success({ title: 'Guardado' })
toast.error({ title: 'Error', description: 'Por favor, inténtalo de nuevo.' })
```
