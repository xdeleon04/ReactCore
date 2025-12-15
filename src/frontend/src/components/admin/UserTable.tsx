import React from 'react';
import type { AdminUserListItem } from '../../types/admin/User';

function badge(active: boolean) {
  return active ? (
    <span className="rounded bg-green-50 px-2 py-1 text-xs text-green-800">Active</span>
  ) : (
    <span className="rounded bg-gray-100 px-2 py-1 text-xs text-gray-800">Inactive</span>
  );
}

export const UserTable: React.FC<{
  users: AdminUserListItem[];
  onViewDetail: (userId: string) => void;
  onDeactivate: (userId: string) => void;
  onReactivate: (userId: string) => void;
  busyUserId?: string | null;
}> = ({ users, onViewDetail, onDeactivate, onReactivate, busyUserId }) => {
  return (
    <div className="overflow-auto rounded border border-gray-200 bg-white">
      <table className="w-full border-collapse text-left text-sm">
        <thead>
          <tr className="border-b border-gray-200 bg-gray-50">
            <th className="px-3 py-2 font-medium text-gray-900">Email</th>
            <th className="px-3 py-2 font-medium text-gray-900">Role</th>
            <th className="px-3 py-2 font-medium text-gray-900">Status</th>
            <th className="px-3 py-2 font-medium text-gray-900">Created</th>
            <th className="px-3 py-2 font-medium text-gray-900">Last login</th>
            <th className="px-3 py-2 font-medium text-gray-900">Actions</th>
          </tr>
        </thead>
        <tbody>
          {users.map((u) => {
            const busy = busyUserId === u.id;
            return (
              <tr key={u.id} className="border-b border-gray-100">
                <td className="px-3 py-2">
                  <button
                    type="button"
                    className="text-left text-sm font-medium text-gray-900 hover:underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
                    onClick={() => onViewDetail(u.id)}
                    aria-label={`View details for ${u.email}`}
                  >
                    {u.email}
                  </button>
                </td>
                <td className="px-3 py-2 text-gray-800">{u.role}</td>
                <td className="px-3 py-2">{badge(u.isActive)}</td>
                <td className="px-3 py-2 text-gray-800">{new Date(u.createdAt).toLocaleDateString()}</td>
                <td className="px-3 py-2 text-gray-800">{u.lastLogin ? new Date(u.lastLogin).toLocaleString() : '—'}</td>
                <td className="px-3 py-2">
                  {u.isActive ? (
                    <button
                      type="button"
                      className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
                      disabled={busy}
                      onClick={() => onDeactivate(u.id)}
                    >
                      {busy ? 'Working…' : 'Deactivate'}
                    </button>
                  ) : (
                    <button
                      type="button"
                      className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
                      disabled={busy}
                      onClick={() => onReactivate(u.id)}
                    >
                      {busy ? 'Working…' : 'Reactivate'}
                    </button>
                  )}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
};
