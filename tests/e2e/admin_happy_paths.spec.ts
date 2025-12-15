import { test, expect } from '@playwright/test';

test.describe('Admin Dashboard (Smoke)', () => {
  test.afterEach(async ({ page }) => {
    try {
      await page.request.post('http://localhost:5149/api/auth/logout');
    } catch {
      // ignore
    }
  });

  test('admin can login and access admin pages', async ({ page }) => {
    // Ensure backend is up before attempting login (avoids hanging "Logging in..." state)
    let backendReady = false;
    for (let i = 0; i < 30; i++) {
      try {
        const resp = await page.request.get('http://localhost:5149/weatherforecast');
        if (resp.ok()) {
          backendReady = true;
          break;
        }
      } catch {
        // ignore
      }
      await page.waitForTimeout(1000);
    }
    expect(backendReady).toBeTruthy();

    await page.goto('/login');
    await page.fill('input[type="email"]', 'admin@example.com');
    await page.fill('input[type="password"]', 'Admin123!');
    await page.click('button[type="submit"]');

    await page.waitForResponse(
      (r) => r.url().includes('/api/auth/login') && r.status() === 200,
      { timeout: 30000 }
    );

    // Navigate to admin dashboard (ProtectedRoute requires admin role)
    await page.goto('/admin/dashboard');
    await expect(page.locator('h1')).toHaveText('Admin · Dashboard');

    await page.goto('/admin/users');
    await expect(page.locator('h1')).toHaveText('Admin · Users');

    await page.goto('/admin/products');
    await expect(page.locator('h1')).toHaveText('Admin · Products');

    await page.goto('/admin/orders');
    await expect(page.locator('h1')).toHaveText('Admin · Orders');

    await page.goto('/admin/reports');
    await expect(page.locator('h1')).toHaveText('Admin · Reports');

    await page.goto('/admin/audit-logs');
    await expect(page.locator('h1')).toHaveText('Admin · Audit Logs');
  });
});
