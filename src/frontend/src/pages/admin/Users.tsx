import React, { useCallback, useEffect, useMemo, useState } from 'react';
import toast from 'react-hot-toast';
import type { AdminUserListItem } from '../../types/admin/User';
import type { PagedResult } from '../../types/admin/AdminResponse';
import { deactivateUser, listUsers, reactivateUser } from '../../services/admin/adminUserService';
import { EmptyState } from '../../components/EmptyState';
import { UserTable } from '../../components/admin/UserTable';
import { UserDetailModal } from '../../components/admin/UserDetailModal';

const DEFAULT_TAKE = 20;

export const AdminUsersPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [role, setRole] = useState('');
  const [isActive, setIsActive] = useState<'all' | 'true' | 'false'>('all');

  const [skip, setSkip] = useState(0);
  const [take, setTake] = useState(DEFAULT_TAKE);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [data, setData] = useState<PagedResult<AdminUserListItem> | null>(null);

  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [busyUserId, setBusyUserId] = useState<string | null>(null);

  const isActiveParam = useMemo(() => {
    if (isActive === 'true') return true;
    if (isActive === 'false') return false;
    return undefined;
  }, [isActive]);

  const fetchUsers = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const result = await listUsers({
        email: email.trim() || undefined,
        role: role.trim() || undefined,
        isActive: isActiveParam,
        skip,
        take,
      });
      setData(result);
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'Unable to load users.';
      setError(message);
    } finally {
      setLoading(false);
    }
  }, [email, role, isActiveParam, skip, take]);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  const onSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setSkip(0);
    fetchUsers();
  };

  const pageStart = data?.skip ?? skip;
  const pageSize = data?.take ?? take;
  const total = data?.total ?? 0;
  const canPrev = pageStart > 0;
  const canNext = pageStart + pageSize < total;

  const handleDeactivate = async (userId: string) => {
    setBusyUserId(userId);
    try {
      await deactivateUser(userId);
      toast.success('User deactivated');
      await fetchUsers();
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'Unable to deactivate user.';
      toast.error(message);
    } finally {
      setBusyUserId(null);
    }
  };

  const handleReactivate = async (userId: string) => {
    setBusyUserId(userId);
    try {
      await reactivateUser(userId);
      toast.success('User reactivated');
      await fetchUsers();
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'Unable to reactivate user.';
      toast.error(message);
    } finally {
      setBusyUserId(null);
    }
  };

  return (
    <main className="mx-auto w-full max-w-6xl px-4 py-6">
      <header className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Admin · Users</h1>
        <p className="mt-1 text-sm text-gray-700">Search, review, and deactivate/reactivate accounts.</p>
      </header>

      <form className="mb-4 grid grid-cols-1 gap-3 sm:grid-cols-4" onSubmit={onSearchSubmit} aria-label="User filters">
        <div className="flex flex-col gap-1">
          <label htmlFor="email" className="text-sm font-medium text-gray-900">
            Email
          </label>
          <input
            id="email"
            className="rounded border border-gray-300 px-3 py-2 text-sm"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="Search by email"
          />
        </div>

        <div className="flex flex-col gap-1">
          <label htmlFor="role" className="text-sm font-medium text-gray-900">
            Role
          </label>
          <select
            id="role"
            className="rounded border border-gray-300 px-3 py-2 text-sm"
            value={role}
            onChange={(e) => setRole(e.target.value)}
          >
            <option value="">All roles</option>
            <option value="admin">admin</option>
            <option value="user">user</option>
          </select>
        </div>

        <div className="flex flex-col gap-1">
          <label htmlFor="status" className="text-sm font-medium text-gray-900">
            Status
          </label>
          <select
            id="status"
            className="rounded border border-gray-300 px-3 py-2 text-sm"
            value={isActive}
            onChange={(e) => setIsActive(e.target.value as 'all' | 'true' | 'false')}
          >
            <option value="all">All</option>
            <option value="true">Active</option>
            <option value="false">Inactive</option>
          </select>
        </div>

        <div className="flex items-end">
          <button
            type="submit"
            className="w-full rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
            disabled={loading}
          >
            {loading ? 'Searching…' : 'Search'}
          </button>
        </div>
      </form>

      {loading ? <div className="text-sm text-gray-700">Loading users…</div> : null}
      {error ? (
        <div role="alert" className="mb-4 rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">
          {error}
        </div>
      ) : null}

      {!loading && data && data.items.length === 0 ? (
        <EmptyState title="No users found" description="Try adjusting your search and filters." />
      ) : null}

      {data && data.items.length > 0 ? (
        <div className="space-y-4">
          <UserTable
            users={data.items}
            onViewDetail={setSelectedUserId}
            onDeactivate={handleDeactivate}
            onReactivate={handleReactivate}
            busyUserId={busyUserId}
          />

          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="text-sm text-gray-700">
              Showing {Math.min(total, pageStart + 1)}–{Math.min(total, pageStart + pageSize)} of {total}
            </div>
            <div className="flex items-center gap-2">
              <button
                type="button"
                className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
                disabled={!canPrev || loading}
                onClick={() => setSkip((prev) => Math.max(0, prev - pageSize))}
              >
                Prev
              </button>
              <button
                type="button"
                className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
                disabled={!canNext || loading}
                onClick={() => setSkip((prev) => prev + pageSize)}
              >
                Next
              </button>
              <select
                aria-label="Page size"
                className="rounded border border-gray-300 px-3 py-2 text-sm"
                value={take}
                onChange={(e) => {
                  const nextTake = Number(e.target.value) || DEFAULT_TAKE;
                  setTake(nextTake);
                  setSkip(0);
                }}
              >
                <option value={10}>10</option>
                <option value={20}>20</option>
                <option value={50}>50</option>
                <option value={100}>100</option>
              </select>
            </div>
          </div>
        </div>
      ) : null}

      {selectedUserId ? (
        <UserDetailModal userId={selectedUserId} open={true} onClose={() => setSelectedUserId(null)} />
      ) : null}
    </main>
  );
};
