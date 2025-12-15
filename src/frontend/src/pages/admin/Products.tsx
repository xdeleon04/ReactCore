import React, { useEffect, useMemo, useState } from 'react';
import toast from 'react-hot-toast';
import { ProductEditForm } from '../../components/admin/ProductEditForm';
import { ProductTable } from '../../components/admin/ProductTable';
import {
  createProduct,
  deleteProduct,
  getProductDetail,
  listProducts,
  updateProduct,
} from '../../services/admin/adminProductService';
import type {
  AdminProductListItem,
  AdminProductUpsertRequest,
} from '../../types/admin/Product';

type EditingState =
  | { kind: 'none' }
  | { kind: 'create' }
  | { kind: 'edit'; productId: number; initial: Partial<AdminProductUpsertRequest> };

export const ProductsPage: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [products, setProducts] = useState<AdminProductListItem[]>([]);
  const [search, setSearch] = useState('');
  const [includeArchived, setIncludeArchived] = useState(false);
  const [lowStockOnly, setLowStockOnly] = useState(false);

  const [editing, setEditing] = useState<EditingState>({ kind: 'none' });

  const reload = async () => {
    setLoading(true);
    try {
      const result = await listProducts({
        search: search.trim() ? search.trim() : undefined,
        isDeleted: includeArchived ? true : undefined,
        lowStockOnly: lowStockOnly ? true : undefined,
        skip: 0,
        take: 100,
      });
      setProducts(result.items);
    } catch {
      toast.error('Unable to load products. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void reload();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search, includeArchived, lowStockOnly]);

  const selectedEditInitial = useMemo(() => {
    if (editing.kind !== 'edit') return undefined;
    return editing.initial;
  }, [editing]);

  const onCreateClick = () => {
    setEditing({ kind: 'create' });
  };

  const onEdit = async (productId: number) => {
    setBusy(true);
    try {
      const detail = await getProductDetail(productId);
      setEditing({
        kind: 'edit',
        productId,
        initial: {
          name: detail.name,
          category: detail.category,
          description: detail.description ?? null,
          price: detail.price,
          stockQuantity: detail.stockQuantity,
          reorderLevel: detail.reorderLevel,
          imageUrl: detail.imageUrl ?? null,
        },
      });
    } catch {
      toast.error('Unable to load product details. Please try again.');
    } finally {
      setBusy(false);
    }
  };

  const onArchive = async (productId: number) => {
    const p = products.find((x) => x.id === productId);
    if (!p) return;

    if (!window.confirm(`Archive "${p.name}"?`)) return;

    setBusy(true);
    try {
      await deleteProduct(productId);
      toast.success('Product archived');
      await reload();
    } catch {
      toast.error('Unable to archive product. Please try again.');
    } finally {
      setBusy(false);
    }
  };

  const onSubmit = async (value: AdminProductUpsertRequest) => {
    setBusy(true);
    try {
      if (editing.kind === 'create') {
        await createProduct(value);
        toast.success('Product created');
      }

      if (editing.kind === 'edit') {
        await updateProduct(editing.productId, value);
        toast.success('Product updated');
      }

      setEditing({ kind: 'none' });
      await reload();
    } catch {
      toast.error('Unable to save product. Please try again.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <main className="space-y-4" aria-label="Admin products page">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-gray-900">Admin · Products</h1>
        <button
          type="button"
          className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
          onClick={onCreateClick}
          disabled={busy}
        >
          New product
        </button>
      </div>

      <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
        <div className="flex-1">
          <label htmlFor="admin-products-search" className="sr-only">
            Search products
          </label>
          <input
            id="admin-products-search"
            className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            placeholder="Search by name"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <label className="flex items-center gap-2 text-sm text-gray-900">
          <input
            type="checkbox"
            checked={includeArchived}
            onChange={(e) => setIncludeArchived(e.target.checked)}
          />
          Include archived
        </label>

        <label className="flex items-center gap-2 text-sm text-gray-900">
          <input
            type="checkbox"
            checked={lowStockOnly}
            onChange={(e) => setLowStockOnly(e.target.checked)}
          />
          Low stock only
        </label>
      </div>

      {loading ? (
        <div className="text-sm text-gray-700">Loading…</div>
      ) : (
        <ProductTable products={products} onEdit={onEdit} onArchive={onArchive} />
      )}

      {editing.kind !== 'none' ? (
        <section className="rounded border border-gray-200 bg-white p-4" aria-label="Product editor">
          <h2 className="mb-3 text-base font-medium text-gray-900">
            {editing.kind === 'create' ? 'Create product' : 'Edit product'}
          </h2>

          <ProductEditForm
            mode={editing.kind === 'create' ? 'create' : 'edit'}
            initialValue={editing.kind === 'edit' ? selectedEditInitial : undefined}
            busy={busy}
            onCancel={() => setEditing({ kind: 'none' })}
            onSubmit={onSubmit}
          />
        </section>
      ) : null}
    </main>
  );
};
