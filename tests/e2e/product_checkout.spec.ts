import { test, expect } from '@playwright/test';

test.describe('Product + Cart + Checkout', () => {
  test('user can add product to cart and place order', async ({ page }) => {
    // Login
    await page.goto('http://localhost:5173/login');
    await page.fill('input[type="email"]', 'admin@example.com');
    await page.fill('input[type="password"]', 'Admin123!');
    await page.click('button[type="submit"]');
    await expect(page).toHaveURL('http://localhost:5173/dashboard');
    // Shop
    await page.goto('http://localhost:5173/shop');
    await expect(page.locator('text=Shop')).toBeVisible();

    // Open first product
    const firstProduct = page.locator('a[href^="/products/"]').first();
    await expect(firstProduct).toBeVisible();
    await firstProduct.click();

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
