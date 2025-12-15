import { http, HttpResponse } from 'msw'

/**
 * Mock Service Worker (MSW) request handlers for API endpoints
 * Used in testing to mock all API calls
 */

export const handlers = [
  // ============ Auth Endpoints ============
  http.post('/api/auth/login', async ({ request }) => {
    const body = (await request.json()) as { email: string; password: string }

    // Mock successful login
    if (body.email === 'test@example.com' && body.password === 'Test123!') {
      return HttpResponse.json(
        {
          accessToken: 'mock-access-token-xyz',
          refreshToken: 'mock-refresh-token-xyz',
          user: {
            id: '1',
            email: 'test@example.com',
            name: 'Test User',
            role: 'user',
          },
        },
        { status: 200 }
      )
    }

    // Mock failed login
    return HttpResponse.json(
      {
        error: 'Invalid credentials',
        message: 'Email or password is incorrect',
      },
      { status: 401 }
    )
  }),

  http.post('/api/auth/logout', () => {
    return HttpResponse.json({ message: 'Logged out successfully' }, { status: 200 })
  }),

  http.post('/api/auth/refresh', () => {
    return HttpResponse.json(
      {
        accessToken: 'new-mock-token',
      },
      { status: 200 }
    )
  }),

  http.get('/api/auth/me', () => {
    // Check if user is authenticated
    return HttpResponse.json(
      {
        id: '1',
        email: 'test@example.com',
        name: 'Test User',
        role: 'user',
      },
      { status: 200 }
    )
  }),

  // ============ Products Endpoints ============
  http.get('/api/products', ({ request }) => {
    const url = new URL(request.url)
    const category = url.searchParams.get('category')
    const search = url.searchParams.get('search')

    let products = [
      {
        id: '1',
        name: 'Laptop',
        description: 'High-performance laptop',
        price: 999.99,
        category: 'electronics',
        imageUrl: 'https://example.com/laptop.jpg',
        stock: 10,
      },
      {
        id: '2',
        name: 'Mouse',
        description: 'Wireless mouse',
        price: 29.99,
        category: 'electronics',
        imageUrl: 'https://example.com/mouse.jpg',
        stock: 50,
      },
      {
        id: '3',
        name: 'Keyboard',
        description: 'Mechanical keyboard',
        price: 79.99,
        category: 'electronics',
        imageUrl: 'https://example.com/keyboard.jpg',
        stock: 30,
      },
    ]

    // Filter by category
    if (category) {
      products = products.filter(p => p.category === category)
    }

    // Filter by search
    if (search) {
      products = products.filter(p =>
        p.name.toLowerCase().includes(search.toLowerCase())
      )
    }

    return HttpResponse.json(
      {
        data: products,
        total: products.length,
      },
      { status: 200 }
    )
  }),

  http.get('/api/products/:id', ({ params }) => {
    const { id } = params

    type MockProduct = {
      id: string
      name: string
      description: string
      price: number
      category: string
      imageUrl: string
      stock: number
      reviews: unknown[]
    }

    const products: Record<string, MockProduct> = {
      '1': {
        id: '1',
        name: 'Laptop',
        description: 'High-performance laptop with 16GB RAM',
        price: 999.99,
        category: 'electronics',
        imageUrl: 'https://example.com/laptop.jpg',
        stock: 10,
        reviews: [],
      },
      '2': {
        id: '2',
        name: 'Mouse',
        description: 'Wireless mouse with ergonomic design',
        price: 29.99,
        category: 'electronics',
        imageUrl: 'https://example.com/mouse.jpg',
        stock: 50,
        reviews: [],
      },
    }

    if (!products[id as string]) {
      return HttpResponse.json(
        { error: 'Product not found' },
        { status: 404 }
      )
    }

    return HttpResponse.json(products[id as string], { status: 200 })
  }),

  http.post('/api/products', () => {
    return HttpResponse.json(
      {
        id: '4',
        name: 'New Product',
        price: 0,
        stock: 0,
      },
      { status: 201 }
    )
  }),

  // ============ Cart Endpoints ============
  http.get('/api/cart', () => {
    return HttpResponse.json(
      {
        id: 'cart-1',
        items: [],
        total: 0,
      },
      { status: 200 }
    )
  }),

  http.post('/api/cart/items', () => {
    return HttpResponse.json(
      {
        message: 'Item added to cart',
        quantity: 1,
      },
      { status: 200 }
    )
  }),

  // ============ Orders Endpoints ============
  http.post('/api/orders', () => {
    return HttpResponse.json(
      {
        id: 'order-123',
        orderNumber: 'ORD-001',
        total: 0,
        status: 'pending',
        createdAt: new Date().toISOString(),
      },
      { status: 201 }
    )
  }),

  http.get('/api/orders/:orderNumber', ({ params }) => {
    const { orderNumber } = params

    return HttpResponse.json(
      {
        id: 'order-1',
        orderNumber,
        total: 1050.00,
        status: 'delivered',
        items: [
          {
            productId: '1',
            productName: 'Laptop',
            quantity: 1,
            price: 999.99,
          },
          {
            productId: '2',
            productName: 'Mouse',
            quantity: 2,
            price: 29.99,
          },
        ],
        createdAt: new Date().toISOString(),
      },
      { status: 200 }
    )
  }),

  // ============ External API Endpoints ============
  http.get('/api/external/weather', ({ request }) => {
    const url = new URL(request.url)
    const location = url.searchParams.get('location') || 'London'

    return HttpResponse.json(
      {
        location,
        temperature: 15,
        feelsLike: 13,
        conditions: 'Cloudy',
        humidity: 65,
        windSpeed: 12,
        lastUpdated: new Date().toISOString(),
      },
      { status: 200 }
    )
  }),

  http.get('/api/admin/api-usage', () => {
    return HttpResponse.json(
      {
        provider: 'OpenWeatherMap',
        used: 150,
        limit: 1000,
        resetTime: new Date(Date.now() + 3600000).toISOString(),
      },
      { status: 200 }
    )
  }),

  // ============ User Profile Endpoints ============
  http.get('/api/users/:userId', ({ params }) => {
    const { userId } = params

    if (userId === '1') {
      return HttpResponse.json(
        {
          id: '1',
          name: 'Test User',
          email: 'test@example.com',
          avatar: 'https://example.com/avatar.jpg',
        },
        { status: 200 }
      )
    }

    return HttpResponse.json(
      { error: 'User not found' },
      { status: 404 }
    )
  }),
]
