import React, { useMemo } from 'react';

type Preset = 'custom' | 'today' | 'last7' | 'last30' | 'thisMonth';

export type ReportDateRangePickerProps = {
  startDate: string;
  endDate: string;
  onChange: (next: { startDate: string; endDate: string }) => void;
  disabled?: boolean;
};

function pad2(n: number): string {
  return String(n).padStart(2, '0');
}

function toDateInputValue(date: Date): string {
  return `${date.getUTCFullYear()}-${pad2(date.getUTCMonth() + 1)}-${pad2(date.getUTCDate())}`;
}

function addDaysUtc(date: Date, days: number): Date {
  const next = new Date(date.getTime());
  next.setUTCDate(next.getUTCDate() + days);
  return next;
}

export const ReportDateRangePicker: React.FC<ReportDateRangePickerProps> = ({ startDate, endDate, onChange, disabled }) => {
  const today = useMemo(() => {
    const now = new Date();
    return new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
  }, []);

  const applyPreset = (preset: Preset) => {
    if (preset === 'custom') return;

    if (preset === 'today') {
      const v = toDateInputValue(today);
      onChange({ startDate: v, endDate: v });
      return;
    }

    if (preset === 'last7') {
      const start = toDateInputValue(addDaysUtc(today, -6));
      const end = toDateInputValue(today);
      onChange({ startDate: start, endDate: end });
      return;
    }

    if (preset === 'last30') {
      const start = toDateInputValue(addDaysUtc(today, -29));
      const end = toDateInputValue(today);
      onChange({ startDate: start, endDate: end });
      return;
    }

    const startOfMonth = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), 1));
    onChange({ startDate: toDateInputValue(startOfMonth), endDate: toDateInputValue(today) });
  };

  return (
    <section aria-label="Report date range" className="grid grid-cols-1 gap-3 sm:grid-cols-4">
      <div className="flex flex-col gap-1">
        <label htmlFor="preset" className="text-sm font-medium text-gray-900">
          Preset
        </label>
        <select
          id="preset"
          className="rounded border border-gray-300 px-3 py-2 text-sm"
          onChange={(e) => applyPreset(e.target.value as Preset)}
          defaultValue="custom"
          disabled={disabled}
        >
          <option value="custom">Custom</option>
          <option value="today">Today</option>
          <option value="last7">Last 7 days</option>
          <option value="last30">Last 30 days</option>
          <option value="thisMonth">This month</option>
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <label htmlFor="startDate" className="text-sm font-medium text-gray-900">
          Start date
        </label>
        <input
          id="startDate"
          type="date"
          className="rounded border border-gray-300 px-3 py-2 text-sm"
          value={startDate}
          onChange={(e) => onChange({ startDate: e.target.value, endDate })}
          disabled={disabled}
        />
      </div>

      <div className="flex flex-col gap-1">
        <label htmlFor="endDate" className="text-sm font-medium text-gray-900">
          End date
        </label>
        <input
          id="endDate"
          type="date"
          className="rounded border border-gray-300 px-3 py-2 text-sm"
          value={endDate}
          onChange={(e) => onChange({ startDate, endDate: e.target.value })}
          disabled={disabled}
        />
      </div>

      <div className="flex items-end">
        <div className="w-full rounded border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-700">
          Uses UTC dates
        </div>
      </div>
    </section>
  );
};
