import { describe, expect, it, vi, beforeEach } from 'vitest';
import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { ProductsPage } from './Products';

vi.mock('../../services/admin/adminProductService', () => {
  return {
    listProducts: vi.fn(async () => ({
      items: [
        {
          id: 1,
          name: 'Widget',
          category: 'Tools',
          price: 9.99,
          stockQuantity: 3,
          reorderLevel: 5,
          isDeleted: false,
          updatedAt: '2025-01-01T00:00:00Z',
        },
      ],
      total: 1,
      skip: 0,
      take: 100,
    })),
    createProduct: vi.fn(async () => ({ id: 2, name: 'New', price: 1, category: 'Tools', stockQuantity: 0, createdAt: '2025-01-01T00:00:00Z' })),
    updateProduct: vi.fn(async () => ({ success: true, productId: 1, updatedAt: '2025-01-01T00:00:00Z' })),
    deleteProduct: vi.fn(async () => undefined),
    getProductDetail: vi.fn(async () => ({
      id: 1,
      name: 'Widget',
      category: 'Tools',
      description: 'A widget',
      price: 9.99,
      stockQuantity: 3,
      reorderLevel: 5,
      isDeleted: false,
      imageUrl: null,
      imageUrls: [],
      specifications: {},
      createdAt: '2025-01-01T00:00:00Z',
      updatedAt: '2025-01-01T00:00:00Z',
    })),
  };
});

describe('ProductsPage', () => {
  beforeEach(() => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
  });

  it('renders products list and can open create form', async () => {
    render(<ProductsPage />);

    expect(await screen.findByText('Admin · Products')).toBeInTheDocument();

    const table = await screen.findByRole('table');
    expect(within(table).getByText('Widget')).toBeInTheDocument();

    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'New product' }));
    });
    expect(screen.getByText('Create product')).toBeInTheDocument();
    expect(screen.getByRole('form', { name: 'Create product form' })).toBeInTheDocument();
  });

  it('can open edit and archive actions', async () => {
    render(<ProductsPage />);

    const row = await screen.findByRole('row', { name: /Widget/i });
    await act(async () => {
      fireEvent.click(within(row).getByRole('button', { name: 'Edit' }));
    });

    expect(await screen.findByText('Edit product')).toBeInTheDocument();

    await act(async () => {
      fireEvent.click(within(row).getByRole('button', { name: /archive/i }));
    });
    expect(window.confirm).toHaveBeenCalled();
  });
});
