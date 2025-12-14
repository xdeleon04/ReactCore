import React from 'react';

export const EmptyState: React.FC<{
  title: string;
  description?: string;
  actionLabel?: string;
  onAction?: () => void;
}> = ({ title, description, actionLabel, onAction }) => {
  return (
    <div className="rounded border border-gray-200 bg-white p-4">
      <div className="text-sm font-medium text-gray-900">{title}</div>
      {description ? <div className="mt-1 text-sm text-gray-700">{description}</div> : null}
      {actionLabel && onAction ? (
        <button
          type="button"
          className="mt-3 rounded border border-gray-300 px-3 py-2 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
          onClick={onAction}
        >
          {actionLabel}
        </button>
      ) : null}
    </div>
  );
};
