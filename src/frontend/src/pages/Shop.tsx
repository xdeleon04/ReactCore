import React, { useEffect, useMemo, useState } from 'react';
import { CategoryFilter } from '../components/CategoryFilter';
import { PriceRangeFilter } from '../components/PriceRangeFilter';
import { ProductList } from '../components/ProductList';
import type { ProductListResponse } from '../types/Product';
import { getProducts } from '../services/productService';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card';

const DEFAULT_TAKE = 20;

function parseOptionalNumber(value: string): number | undefined {
  const trimmed = value.trim();
  if (!trimmed) return undefined;
  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : undefined;
}

export const Shop: React.FC = () => {
  const [category, setCategory] = useState('');
  const [minPrice, setMinPrice] = useState('');
  const [maxPrice, setMaxPrice] = useState('');

  const [skip, setSkip] = useState(0);
  const [take] = useState(DEFAULT_TAKE);

  const [data, setData] = useState<ProductListResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const categories = useMemo(() => {
    const set = new Set<string>();
    for (const p of data?.items ?? []) set.add(p.category);
    return Array.from(set).sort();
  }, [data]);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setError('');

      try {
        const result = await getProducts({
          category: category || undefined,
          minPrice: parseOptionalNumber(minPrice),
          maxPrice: parseOptionalNumber(maxPrice),
          skip,
          take,
        });
        setData(result);
      } catch (e) {
        console.error(e);
        setError('Unable to load products. Please try again.');
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [category, minPrice, maxPrice, skip, take]);

  const total = data?.total ?? 0;
  const items = data?.items ?? [];
  const canPrev = skip > 0;
  const canNext = skip + take < total;

  const onCategoryChange = (next: string) => {
    setCategory(next);
    setSkip(0);
  };

  const onPriceChange = (next: { minPrice: string; maxPrice: string }) => {
    setMinPrice(next.minPrice);
    setMaxPrice(next.maxPrice);
    setSkip(0);
  };

  return (
    <main className="space-y-6">
      <header>
        <h1 className="text-2xl font-bold text-slate-900 sm:text-3xl">Products</h1>
        <p className="mt-1 text-sm text-slate-700">Browse products and filter by category and price.</p>
      </header>

      <section className="grid grid-cols-1 gap-4 md:grid-cols-2" aria-label="Product filters">
        <Card>
          <CardHeader>
            <CardTitle>Category</CardTitle>
          </CardHeader>
          <CardContent>
            <CategoryFilter categories={categories} value={category} onChange={onCategoryChange} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Price</CardTitle>
          </CardHeader>
          <CardContent>
            <PriceRangeFilter minPrice={minPrice} maxPrice={maxPrice} onChange={onPriceChange} />
          </CardContent>
        </Card>
      </section>

      {error ? (
        <div role="alert" className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">
          {error}
        </div>
      ) : null}

      <section aria-label="Product results">
        <ProductList items={items} loading={loading} />
      </section>

      <nav className="mt-6 flex items-center justify-between" aria-label="Pagination">
        <Button
          type="button"
          variant="outline"
          onClick={() => setSkip(Math.max(0, skip - take))}
          disabled={!canPrev || loading}
        >
          Previous
        </Button>

        <div className="text-sm text-slate-700">
          {total > 0 ? (
            <span>
              Showing {Math.min(skip + 1, total)}–{Math.min(skip + take, total)} of {total}
            </span>
          ) : (
            <span>Showing 0 results</span>
          )}
        </div>

        <Button type="button" variant="outline" onClick={() => setSkip(skip + take)} disabled={!canNext || loading}>
          Next
        </Button>
      </nav>
    </main>
  );
};
