import { test, expect } from '@playwright/test';
import { getE2EUser } from './testUsers';

test.describe('External API Weather Widget', () => {
  test.afterEach(async ({ page }) => {
    try {
      await page.request.post('http://localhost:5149/api/auth/logout');
    } catch {
      // ignore
    }
  });

  test('shows weather widget on dashboard', async ({ page }, testInfo) => {
    const user = getE2EUser(testInfo);

    // Mock backend response so E2E doesn't depend on an external API key.
    await page.route('**/api/external/weather**', async (route) => {
      const now = new Date();
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          temperature: 18,
          condition: 'Clear',
          location: 'Santo Domingo',
          humidity: 50,
          windSpeed: 3,
          fetchedAt: now.toISOString(),
          isCached: true,
          cacheExpiresAt: new Date(now.getTime() + 60_000).toISOString(),
        }),
      });
    });

    await page.goto('/login');
    await page.fill('input[type="email"]', user.email);
    await page.fill('input[type="password"]', user.password);
    await page.click('button[type="submit"]');

    await page.waitForResponse(
      (r) => r.url().includes('/api/auth/login') && r.status() === 200,
      { timeout: 30000 }
    );

    await expect(page).toHaveURL('http://localhost:5173/dashboard');
    await expect(page.locator('h1')).toHaveText('Dashboard');

    await expect(page.locator('text=Weather')).toBeVisible();
    await expect(page.locator('text=Santo Domingo')).toBeVisible();
    await expect(page.locator('text=Cached')).toBeVisible();
  });
});
