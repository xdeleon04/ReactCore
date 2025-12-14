import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import toast from 'react-hot-toast';
import { ImageGallery } from '../components/ImageGallery';
import { RelatedProducts } from '../components/RelatedProducts';
import { SpecificationsTable } from '../components/SpecificationsTable';
import { StockStatusBadge } from '../components/StockStatusBadge';
import { AddToCartButton } from '../components/AddToCartButton';
import { NotifyMeButton } from '../components/NotifyMeButton';
import { ProductDetailSkeleton } from '../components/skeletons/ProductDetailSkeleton';
import type { ProductDetail, RelatedProduct } from '../types/Product';
import { getProductById, getRelatedProducts } from '../services/productService';
import { usePolling } from '../hooks/usePolling';
import { getInventoryStatus } from '../services/inventoryService';

export const ProductDetailPage: React.FC = () => {
  const { id } = useParams();
  const productId = Number(id);

  const [product, setProduct] = useState<ProductDetail | null>(null);
  const [related, setRelated] = useState<RelatedProduct[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!Number.isFinite(productId) || productId <= 0) {
      setError('Invalid product id');
      setLoading(false);
      return;
    }

    const load = async () => {
      setLoading(true);
      setError('');
      try {
        const p = await getProductById(productId);
        setProduct(p);

        const r = await getRelatedProducts(productId, 4);
        setRelated(r);
      } catch (e) {
        console.error(e);
        setError('Unable to load product.');
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [productId]);

  usePolling(
    async () => {
      if (!Number.isFinite(productId) || productId <= 0) return;

      try {
        const inv = await getInventoryStatus(productId);

        setProduct((prev) => {
          if (!prev) return prev;

          const prevStatus = prev.status;
          const next = {
            ...prev,
            stockQuantity: inv.stockQuantity,
            reorderLevel: inv.reorderLevel,
            status: inv.status,
          };

          if (prevStatus !== inv.status) {
            if (inv.status === 'out-of-stock') {
              toast.error('This product is now out of stock.');
            } else {
              toast.success('This product is back in stock.');
            }
          }

          return next;
        });
      } catch {
        // Ignore polling errors
      }
    },
    5000,
    Boolean(productId)
  );

  if (loading) {
    return <ProductDetailSkeleton />;
  }

  if (error) {
    return (
      <main className="mx-auto max-w-6xl px-4 py-6">
        <div role="alert" className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">
          {error}
        </div>
      </main>
    );
  }

  if (!product) {
    return (
      <main className="mx-auto max-w-6xl px-4 py-6">
        <div className="text-sm text-gray-700">Product not found.</div>
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-6xl px-4 py-6">
      <header className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">{product.name}</h1>
        <div className="mt-2 flex items-center gap-3">
          <div className="text-lg font-semibold text-gray-900">${product.price.toFixed(2)}</div>
          <StockStatusBadge status={product.status} stockQuantity={product.stockQuantity} />
        </div>
      </header>

      <section className="grid grid-cols-1 gap-6 lg:grid-cols-2" aria-label="Product details">
        <ImageGallery imageUrls={product.imageUrls} alt={product.name} />

        <div>
          {product.description ? <p className="text-sm text-gray-700">{product.description}</p> : null}

          <div className="mt-4">
            <AddToCartButton product={product} />
            {product.status === 'out-of-stock' ? (
              <div className="mt-3">
                <NotifyMeButton productId={product.id} status={product.status} />
              </div>
            ) : null}
          </div>

          <div className="mt-6">
            <h2 className="text-lg font-semibold text-gray-900">Specifications</h2>
            <div className="mt-2">
              <SpecificationsTable specifications={product.specifications} />
            </div>
          </div>
        </div>
      </section>

      <section className="mt-10" aria-label="Related products">
        <h2 className="text-lg font-semibold text-gray-900">Related products</h2>
        <div className="mt-3">
          <RelatedProducts products={related} />
        </div>
      </section>
    </main>
  );
};
