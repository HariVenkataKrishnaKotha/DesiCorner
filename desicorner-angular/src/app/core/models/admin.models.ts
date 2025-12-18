// ==================== ORDER ADMIN MODELS ====================

export interface AdminOrderListItem {
  id: string;
  orderNumber: string;
  customerEmail: string;
  customerName?: string;
  isGuestOrder: boolean;
  total: number;
  status: string;
  paymentStatus: string;
  orderDate: string;
  itemCount: number;
}

export interface AdminOrderFilter {
  status?: string;
  paymentStatus?: string;
  searchTerm?: string;
  fromDate?: string;
  toDate?: string;
  page: number;
  pageSize: number;
  sortBy: string;
  sortDescending: boolean;
}

export interface OrderStats {
  totalOrders: number;
  pendingOrders: number;
  processingOrders: number;
  deliveredOrders: number;
  cancelledOrders: number;
  totalRevenue: number;
  todayRevenue: number;
  weekRevenue: number;
  monthRevenue: number;
}

export interface PaginatedAdminOrderResponse {
  orders: AdminOrderListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ==================== USER ADMIN MODELS ====================

export interface AdminUserListItem {
  id: string;
  email: string;
  phoneNumber?: string;
  emailConfirmed: boolean;
  phoneNumberConfirmed: boolean;
  dietaryPreference?: string;
  rewardPoints: number;
  roles: string[];
  createdAt: string;
  lastLoginAt?: string;
  isLocked: boolean;
}

export interface AdminUserFilter {
  searchTerm?: string;
  role?: string;
  emailConfirmed?: boolean;
  isLocked?: boolean;
  page: number;
  pageSize: number;
  sortBy: string;
  sortDescending: boolean;
}

export interface UserStats {
  totalUsers: number;
  activeUsers: number;
  newUsersToday: number;
  newUsersThisWeek: number;
  newUsersThisMonth: number;
  lockedUsers: number;
  unverifiedUsers: number;
  adminCount: number;
  customerCount: number;
}

// ==================== COUPON ADMIN MODELS ====================

export interface Coupon {
  id: string;
  code: string;
  description?: string;
  discountAmount: number;
  discountType: 'Fixed' | 'Percentage';
  minAmount: number;
  maxDiscount?: number;
  expiryDate?: string;
  isActive: boolean;
  maxUsageCount: number;
  usedCount: number;
  createdAt: string;
}

export interface CreateCouponRequest {
  code: string;
  description?: string;
  discountAmount: number;
  discountType: 'Fixed' | 'Percentage';
  minAmount: number;
  maxDiscount?: number;
  expiryDate?: string;
  isActive: boolean;
  maxUsageCount: number;
}

export interface UpdateCouponRequest extends CreateCouponRequest {
  id: string;
}

export interface CouponFilter {
  searchTerm?: string;
  isActive?: boolean;
  isExpired?: boolean;
  page: number;
  pageSize: number;
}

export interface CouponStats {
  totalCoupons: number;
  activeCoupons: number;
  expiredCoupons: number;
  fullyUsedCoupons: number;
  totalDiscountGiven: number;
}

// ==================== PRODUCT STATS MODELS ====================

export interface ProductStats {
  totalProducts: number;
  activeProducts: number;
  outOfStockProducts: number;
  lowStockProducts: number;
  featuredProducts: number;
  averagePrice: number;
  averageRating: number;
  totalReviews: number;
  categoryBreakdown: CategoryStats[];
  topRatedProducts: TopProduct[];
}

export interface CategoryStats {
  categoryId: string;
  categoryName: string;
  productCount: number;
  averagePrice: number;
}

export interface TopProduct {
  id: string;
  name: string;
  imageUrl?: string;
  price: number;
  averageRating: number;
  reviewCount: number;
}

// ==================== DASHBOARD MODELS ====================

export interface RecentOrder {
  orderId: string;
  orderNumber: string;
  customerEmail: string;
  total: number;
  status: string;
  createdAt: string;
}

export interface RecentUser {
  userId: string;
  email: string;
  registeredAt: string;
}