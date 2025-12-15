import { test, expect } from '@playwright/test';
import { getE2EUser } from './testUsers';

test.describe('Product + Cart + Checkout', () => {
  test.afterEach(async ({ page }) => {
    try {
      await page.request.post('http://localhost:5149/api/auth/logout');
    } catch {
      // ignore
    }
  });

  test('user can add product to cart and place order', async ({ page }, testInfo) => {
    const user = getE2EUser(testInfo);

    // Login
    await page.goto('/login');
    await page.fill('input[type="email"]', user.email);
    await page.fill('input[type="password"]', user.password);
    await page.click('button[type="submit"]');

    // Wait for login response
    await page.waitForResponse(
      (r) => r.url().includes('/api/auth/login') && r.status() === 200,
      { timeout: 30000 }
    );

    await expect(page).toHaveURL('http://localhost:5173/dashboard');
    // Shop
    await page.goto('/shop');
    await expect(page.locator('text=Shop')).toBeVisible();

    // Pick an in-stock product via the API so repeated runs don't flake when inventory changes.
    const productsResp = await page.request.get('http://localhost:5149/api/products?take=50');
    if (!productsResp.ok()) {
      throw new Error(`Failed to load products: HTTP ${productsResp.status()} ${productsResp.statusText()}`);
    }

    const productsJson = (await productsResp.json()) as {
      items: Array<{ id: number; status: string; stockQuantity: number }>;
    };

    const candidate = productsJson.items.find((p) => p.status !== 'out-of-stock' && p.stockQuantity > 0);
    if (!candidate) {
      throw new Error('No in-stock products available to test checkout. Reset inventory/DB seed and retry.');
    }

    await page.goto(`/products/${candidate.id}`);

    // Add to cart
    await page.click('button:has-text("Add to cart")');

    // Wait for cart badge count to update
    await expect(page.locator('button[aria-label="Open cart"] span')).toHaveText('1');

    // Open cart
    await page.click('button[aria-label="Open cart"]');
    await expect(page.getByRole('heading', { name: 'Cart' })).toBeVisible();
    await expect(page.getByText('Your cart is empty.')).toHaveCount(0);

    // Go to checkout
    const checkoutButton = page.locator('aside').getByRole('button', { name: 'Checkout' });
    await expect(checkoutButton).toBeEnabled();
    await checkoutButton.scrollIntoViewIfNeeded();
    await checkoutButton.dispatchEvent('click');
    await expect(page).toHaveURL('http://localhost:5173/checkout');

    // Place order
    await page.fill('#checkout-email', 'customer@example.com');
    await page.click('button:has-text("Place order")');

    await expect(page.locator('h1')).toHaveText('Order confirmed');
    await expect(page.locator('text=Order number:')).toBeVisible();
  });
});
