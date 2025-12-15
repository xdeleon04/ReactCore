export interface AdminUserListItem {
  id: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  lastLogin?: string | null;
}

export interface AdminUserOrderHistoryItem {
  orderNumber: string;
  date: string;
  total: number;
  status: string;
}

export interface AdminUserCartActivity {
  itemCount: number;
  lastUpdated?: string | null;
}

export interface AdminUserDetail {
  id: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  lastLogin?: string | null;
  orderHistory: AdminUserOrderHistoryItem[];
  cartActivity: AdminUserCartActivity;
}
