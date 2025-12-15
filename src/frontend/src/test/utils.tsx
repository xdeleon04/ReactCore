import React, { type PropsWithChildren } from 'react'
import { render as rtlRender, type RenderOptions } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'

type RenderWithRouterOptions = {
  route?: string
}

function Wrapper({ children, route }: PropsWithChildren<{ route: string }>) {
  return <MemoryRouter initialEntries={[route]}>{children}</MemoryRouter>
}

export function render(ui: React.ReactElement, options: RenderOptions & RenderWithRouterOptions = {}) {
  const { route = '/', ...renderOptions } = options
  return rtlRender(ui, {
    wrapper: props => <Wrapper route={route} {...props} />,
    ...renderOptions,
  })
}

export * from '@testing-library/react'
