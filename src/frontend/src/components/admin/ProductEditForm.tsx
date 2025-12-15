import React, { useMemo, useState } from 'react';
import type { AdminProductUpsertRequest } from '../../types/admin/Product';

export type ProductEditFormMode = 'create' | 'edit';

type Props = {
  mode: ProductEditFormMode;
  initialValue?: Partial<AdminProductUpsertRequest>;
  busy?: boolean;
  onCancel: () => void;
  onSubmit: (value: AdminProductUpsertRequest) => void;
};

type FormErrors = Partial<Record<keyof AdminProductUpsertRequest, string>>;

function validate(value: AdminProductUpsertRequest): FormErrors {
  const errors: FormErrors = {};

  if (!value.name.trim()) errors.name = 'Name is required';
  if (value.name.trim().length > 255) errors.name = 'Name must be 255 characters or less';

  if (!value.category.trim()) errors.category = 'Category is required';
  if (value.category.trim().length > 100) errors.category = 'Category must be 100 characters or less';

  if (!(value.price > 0)) errors.price = 'Price must be greater than 0';
  if (!(Number.isFinite(value.price))) errors.price = 'Price must be a number';

  if (!Number.isInteger(value.stockQuantity) || value.stockQuantity < 0) errors.stockQuantity = 'Stock must be an integer ≥ 0';

  if (value.reorderLevel !== undefined) {
    if (!Number.isInteger(value.reorderLevel) || value.reorderLevel < 0) errors.reorderLevel = 'Reorder level must be an integer ≥ 0';
  }

  if (value.imageUrl && value.imageUrl.length > 500) errors.imageUrl = 'Image URL must be 500 characters or less';

  return errors;
}

export const ProductEditForm: React.FC<Props> = ({ mode, initialValue, busy, onCancel, onSubmit }) => {
  const [prevInitialValue, setPrevInitialValue] = useState(initialValue);
  const [name, setName] = useState(initialValue?.name ?? '');
  const [category, setCategory] = useState(initialValue?.category ?? '');
  const [description, setDescription] = useState(initialValue?.description ?? '');
  const [price, setPrice] = useState<number>(initialValue?.price ?? 0);
  const [stockQuantity, setStockQuantity] = useState<number>(initialValue?.stockQuantity ?? 0);
  const [reorderLevel, setReorderLevel] = useState<number>(initialValue?.reorderLevel ?? 5);
  const [imageUrl, setImageUrl] = useState<string>(initialValue?.imageUrl ?? '');

  if (initialValue !== prevInitialValue) {
    setPrevInitialValue(initialValue);
    setName(initialValue?.name ?? '');
    setCategory(initialValue?.category ?? '');
    setDescription(initialValue?.description ?? '');
    setPrice(initialValue?.price ?? 0);
    setStockQuantity(initialValue?.stockQuantity ?? 0);
    setReorderLevel(initialValue?.reorderLevel ?? 5);
    setImageUrl(initialValue?.imageUrl ?? '');
  }

  const value: AdminProductUpsertRequest = useMemo(
    () => ({
      name,
      category,
      description: description ? description : null,
      price,
      stockQuantity,
      reorderLevel,
      imageUrl: imageUrl ? imageUrl : null,
    }),
    [name, category, description, price, stockQuantity, reorderLevel, imageUrl],
  );

  const errors = useMemo(() => validate(value), [value]);
  const isValid = Object.keys(errors).length === 0;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!isValid || busy) return;
    onSubmit({
      ...value,
      name: value.name.trim(),
      category: value.category.trim(),
    });
  };

  return (
    <form onSubmit={handleSubmit} aria-label={mode === 'create' ? 'Create product form' : 'Edit product form'}>
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <div className="flex flex-col gap-1">
          <label htmlFor="product-name" className="text-sm font-medium text-gray-900">
            Name
          </label>
          <input
            id="product-name"
            className="rounded border border-gray-300 px-3 py-2 text-sm"
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
          {errors.name ? <div className="text-xs text-red-700">{errors.name}</div> : null}
        </div>

        <div className="flex flex-col gap-1">
          <label htmlFor="product-category" className="text-sm font-medium text-gray-900">
            Category
          </label>
          <input
            id="product-category"
            className="rounded border border-gray-300 px-3 py-2 text-sm"
            value={category}
            onChange={(e) => setCategory(e.target.value)}
          />
          {errors.category ? <div className="text-xs text-red-700">{errors.category}</div> : null}
        </div>

        <div className="flex flex-col gap-1">
          <label htmlFor="product-price" className="text-sm font-medium text-gray-900">
            Price
          </label>
          <input
            id="product-price"
            type="number"
            className="rounded border border-gray-300 px-3 py-2 text-sm"
            value={price}
            onChange={(e) => setPrice(Number(e.target.value))}
            step="0.01"
            min={0}
          />
          {errors.price ? <div className="text-xs text-red-700">{errors.price}</div> : null}
        </div>

        <div className="flex flex-col gap-1">
          <label htmlFor="product-stock" className="text-sm font-medium text-gray-900">
            Stock quantity
          </label>
          <input
            id="product-stock"
            type="number"
            className="rounded border border-gray-300 px-3 py-2 text-sm"
            value={stockQuantity}
            onChange={(e) => setStockQuantity(Number(e.target.value))}
            step="1"
            min={0}
          />
          {errors.stockQuantity ? <div className="text-xs text-red-700">{errors.stockQuantity}</div> : null}
        </div>

        <div className="flex flex-col gap-1">
          <label htmlFor="product-reorder" className="text-sm font-medium text-gray-900">
            Reorder level
          </label>
          <input
            id="product-reorder"
            type="number"
            className="rounded border border-gray-300 px-3 py-2 text-sm"
            value={reorderLevel}
            onChange={(e) => setReorderLevel(Number(e.target.value))}
            step="1"
            min={0}
          />
          {errors.reorderLevel ? <div className="text-xs text-red-700">{errors.reorderLevel}</div> : null}
        </div>

        <div className="flex flex-col gap-1">
          <label htmlFor="product-imageUrl" className="text-sm font-medium text-gray-900">
            Image URL
          </label>
          <input
            id="product-imageUrl"
            className="rounded border border-gray-300 px-3 py-2 text-sm"
            value={imageUrl}
            onChange={(e) => setImageUrl(e.target.value)}
          />
          {errors.imageUrl ? <div className="text-xs text-red-700">{errors.imageUrl}</div> : null}
        </div>

        <div className="sm:col-span-2 flex flex-col gap-1">
          <label htmlFor="product-description" className="text-sm font-medium text-gray-900">
            Description
          </label>
          <textarea
            id="product-description"
            className="min-h-24 rounded border border-gray-300 px-3 py-2 text-sm"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </div>
      </div>

      <div className="mt-4 flex items-center justify-end gap-2">
        <button
          type="button"
          className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
          onClick={onCancel}
          disabled={busy}
        >
          Cancel
        </button>
        <button
          type="submit"
          className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
          disabled={!isValid || busy}
        >
          {busy ? 'Saving…' : mode === 'create' ? 'Create' : 'Save'}
        </button>
      </div>
    </form>
  );
};
