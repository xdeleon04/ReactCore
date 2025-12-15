import { expect } from 'vitest'
import { axe, type AxeResults } from 'jest-axe'

export async function checkA11y(container: Element | DocumentFragment): Promise<AxeResults> {
  const results = await axe(container)
  expect(results).toHaveNoViolations()
  return results
}
