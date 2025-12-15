export type ApiQuotaAlert = {
  severity: string;
  message: string;
};

export type ApiQuotaStatus = {
  apiName: string;
  callsToday: number;
  callsThisHour: number;
  quotaLimit: number;
  quotaUsed: number;
  quotaRemaining: number;
  quotaPercentage: number;
  resetsAt: string;
  successRate: number;
  lastCall?: string | null;
  alerts: ApiQuotaAlert[];
};
