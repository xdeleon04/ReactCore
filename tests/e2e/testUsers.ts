export type E2EUser = {
  email: string;
  password: string;
};

import type { TestInfo } from '@playwright/test';
import * as crypto from 'node:crypto';

const E2E_USER_COUNT = 16;

export function getE2EUser(testInfo: TestInfo): E2EUser {
  // Derive a stable user assignment per test so parallel tests don't share credentials.
  // (Using workerIndex can assign the same user to multiple tests when workers are reused.)
  const key = testInfo.titlePath.join(' › ');
  const digest = crypto.createHash('sha1').update(key).digest();
  const n = digest.readUInt32BE(0);
  const userNumber = (n % E2E_USER_COUNT) + 1;
  return {
    email: `e2e-user-${userNumber}@example.com`,
    password: 'User123!',
  };
}
