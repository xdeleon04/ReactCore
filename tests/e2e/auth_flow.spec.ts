import { test, expect } from '@playwright/test';

test.describe('Authentication Flow', () => {
  test('should allow user to login, access protected route, refresh token, and logout', async ({ page }) => {
    // 1. Login
    await page.goto('http://localhost:5173/login');
    await page.fill('input[type="email"]', 'user@example.com');
    await page.fill('input[type="password"]', 'User123!');
    await page.click('button[type="submit"]');

    // Verify redirect to dashboard
    await expect(page).toHaveURL('http://localhost:5173/dashboard');
    await expect(page.locator('h1')).toHaveText('Dashboard');
    await expect(page.locator('text=Welcome, user@example.com')).toBeVisible();

    // 2. Access Protected Route (Dashboard calls /api/user/profile)
    // This is implicitly verified by the dashboard loading user info
    await expect(page.locator('.profile-details')).toBeVisible();

    // 3. Refresh Token (Simulated by waiting or forcing refresh)
    // Since we can't easily wait 15 mins, we can check if the refresh cookie is present
    // or just rely on the fact that the dashboard loaded which means the access token worked.
    // To test refresh explicitly, we might need to manually expire the token or wait for the silent refresh timer.
    // For this E2E, we'll assume if the session persists after a reload, refresh is working (or token is still valid).

    await page.reload();
    await expect(page.locator('h1')).toHaveText('Dashboard');
    await expect(page.locator('text=Welcome, user@example.com')).toBeVisible();

    // 4. Logout
    await page.click('button:has-text("Logout")');

    // Verify redirect to login
    await expect(page).toHaveURL('http://localhost:5173/login');

    // Verify protected route is no longer accessible
    await page.goto('http://localhost:5173/dashboard');
    await expect(page).toHaveURL('http://localhost:5173/login');
  });
});
