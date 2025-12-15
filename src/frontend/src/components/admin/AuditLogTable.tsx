import React from 'react';
import type { AdminAction } from '../../types/admin/AdminAction';

export const AuditLogTable: React.FC<{ items: AdminAction[] }> = ({ items }) => {
  return (
    <div className="overflow-x-auto">
      <table className="min-w-full border-separate border-spacing-0" aria-label="Audit logs">
        <thead>
          <tr className="text-left text-sm text-gray-700">
            <th className="border-b border-gray-200 px-3 py-2">Time</th>
            <th className="border-b border-gray-200 px-3 py-2">Admin</th>
            <th className="border-b border-gray-200 px-3 py-2">Action</th>
            <th className="border-b border-gray-200 px-3 py-2">Entity</th>
            <th className="border-b border-gray-200 px-3 py-2">Reason</th>
          </tr>
        </thead>
        <tbody>
          {items.map((i) => (
            <tr key={i.id} className="text-sm text-gray-900">
              <td className="border-b border-gray-100 px-3 py-2 whitespace-nowrap">
                {new Date(i.timestamp).toLocaleString()}
              </td>
              <td className="border-b border-gray-100 px-3 py-2">{i.adminEmail}</td>
              <td className="border-b border-gray-100 px-3 py-2">{i.action}</td>
              <td className="border-b border-gray-100 px-3 py-2">
                {i.entityType} · {i.entityId}
              </td>
              <td className="border-b border-gray-100 px-3 py-2">{i.reason || ''}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};
