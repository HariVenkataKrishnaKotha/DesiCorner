import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/response.models';
import { Product, Category, CreateProductRequest } from '../models/product.models';
import { environment } from '@env/environment';
import { 
  AdminOrderListItem, 
  AdminOrderFilter, 
  OrderStats,
  PaginatedAdminOrderResponse,
  AdminUserListItem,
  AdminUserFilter,
  UserStats,
  Coupon,
  CreateCouponRequest,
  UpdateCouponRequest,
  CouponFilter,
  CouponStats,
  ProductStats,
  RecentOrder,
  RecentUser
} from '../models/admin.models';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private http = inject(HttpClient);
  private baseUrl = environment.gatewayUrl;

  // ==================== PRODUCTS ====================

  createProduct(product: CreateProductRequest, image?: File): Observable<ApiResponse<Product>> {
    const formData = new FormData();
    
    // Append product fields
    formData.append('name', product.name);
    formData.append('description', product.description);
    formData.append('price', product.price.toString());
    formData.append('categoryId', product.categoryId);
    formData.append('isAvailable', product.isAvailable.toString());
    formData.append('isVegetarian', product.isVegetarian.toString());
    formData.append('isVegan', product.isVegan ? product.isVegan.toString() : 'false');
    formData.append('isSpicy', product.isSpicy.toString());
    formData.append('spiceLevel', product.spiceLevel.toString());
    formData.append('preparationTime', product.preparationTime.toString());
    
    if (product.allergens) {
      formData.append('allergens', product.allergens);
    }
    
    // Append image if provided
    if (image) {
      formData.append('image', image, image.name);
    }

    return this.http.post<ApiResponse<Product>>(`${this.baseUrl}/api/products`, formData);
  }

  updateProduct(id: string, product: Partial<Product>, image?: File): Observable<ApiResponse<Product>> {
    const formData = new FormData();
    
    formData.append('id', id);
    formData.append('name', product.name!);
    formData.append('description', product.description!);
    formData.append('price', product.price!.toString());
    formData.append('categoryId', product.categoryId!);
    formData.append('isAvailable', product.isAvailable!.toString());
    formData.append('isVegetarian', product.isVegetarian!.toString());
    formData.append('isVegan', product.isVegan ? product.isVegan.toString() : 'false');
    formData.append('isSpicy', product.isSpicy!.toString());
    formData.append('spiceLevel', product.spiceLevel!.toString());
    formData.append('preparationTime', product.preparationTime!.toString());
    
    if (product.allergens) {
      formData.append('allergens', product.allergens);
    }
    
    if (image) {
      formData.append('image', image, image.name);
    }

    return this.http.put<ApiResponse<Product>>(`${this.baseUrl}/api/products/${id}`, formData);
  }

  deleteProduct(id: string): Observable<ApiResponse> {
    return this.http.delete<ApiResponse>(`${this.baseUrl}/api/products/${id}`);
  }

  updateProductImage(id: string, image: File): Observable<ApiResponse<Product>> {
    const formData = new FormData();
    formData.append('image', image, image.name);

    return this.http.post<ApiResponse<Product>>(`${this.baseUrl}/api/products/${id}/image`, formData);
  }

  deleteProductImage(id: string): Observable<ApiResponse<Product>> {
    return this.http.delete<ApiResponse<Product>>(`${this.baseUrl}/api/products/${id}/image`);
  }

  // ==================== CATEGORIES ====================

  createCategory(name: string, description: string, displayOrder: number, image?: File): Observable<ApiResponse<Category>> {
    const formData = new FormData();
    
    formData.append('name', name);
    formData.append('description', description);
    formData.append('displayOrder', displayOrder.toString());
    
    if (image) {
      formData.append('image', image, image.name);
    }

    return this.http.post<ApiResponse<Category>>(`${this.baseUrl}/api/categories`, formData);
  }

  updateCategory(id: string, name: string, description: string, displayOrder: number, image?: File): Observable<ApiResponse<Category>> {
    const formData = new FormData();
    
    formData.append('id', id);
    formData.append('name', name);
    formData.append('description', description);
    formData.append('displayOrder', displayOrder.toString());
    
    if (image) {
      formData.append('image', image, image.name);
    }

    return this.http.put<ApiResponse<Category>>(`${this.baseUrl}/api/categories/${id}`, formData);
  }

  deleteCategory(id: string): Observable<ApiResponse> {
    return this.http.delete<ApiResponse>(`${this.baseUrl}/api/categories/${id}`);
  }

  updateCategoryImage(id: string, image: File): Observable<ApiResponse<Category>> {
    const formData = new FormData();
    formData.append('image', image, image.name);

    return this.http.post<ApiResponse<Category>>(`${this.baseUrl}/api/categories/${id}/image`, formData);
  }

  deleteCategoryImage(id: string): Observable<ApiResponse<Category>> {
    return this.http.delete<ApiResponse<Category>>(`${this.baseUrl}/api/categories/${id}/image`);
  }

  // ==================== ORDERS ====================

  getAllOrders(filter: AdminOrderFilter): Observable<ApiResponse<PaginatedAdminOrderResponse>> {
    const params = new HttpParams()
      .set('page', filter.page.toString())
      .set('pageSize', filter.pageSize.toString())
      .set('sortBy', filter.sortBy)
      .set('sortDescending', filter.sortDescending.toString())
      .set('status', filter.status || '')
      .set('paymentStatus', filter.paymentStatus || '')
      .set('searchTerm', filter.searchTerm || '')
      .set('fromDate', filter.fromDate || '')
      .set('toDate', filter.toDate || '');

    return this.http.get<ApiResponse<PaginatedAdminOrderResponse>>(
      `${this.baseUrl}/api/orders/admin/all`, 
      { params }
    );
  }

  getOrderStats(): Observable<ApiResponse<OrderStats>> {
    return this.http.get<ApiResponse<OrderStats>>(`${this.baseUrl}/api/orders/admin/stats`);
  }

  getRecentOrders(count: number = 5): Observable<ApiResponse<AdminOrderListItem[]>> {
    return this.http.get<ApiResponse<AdminOrderListItem[]>>(
      `${this.baseUrl}/api/orders/admin/recent?count=${count}`
    );
  }

  updateOrderStatus(orderId: string, status: string, notes?: string): Observable<ApiResponse> {
  return this.http.put<ApiResponse>(
    `${this.baseUrl}/api/orders/status`,
    { orderId, status, notes }
  );
}

  getOrderById(orderId: string): Observable<ApiResponse> {
    return this.http.get<ApiResponse>(`${this.baseUrl}/api/orders/${orderId}`);
  }

  // ==================== USERS ====================

  getAllUsers(filter: AdminUserFilter): Observable<ApiResponse> {
    const params = new HttpParams()
      .set('page', filter.page.toString())
      .set('pageSize', filter.pageSize.toString())
      .set('sortBy', filter.sortBy)
      .set('sortDescending', filter.sortDescending.toString())
      .set('searchTerm', filter.searchTerm || '')
      .set('role', filter.role || '')
      .set('emailConfirmed', filter.emailConfirmed?.toString() || '')
      .set('isLocked', filter.isLocked?.toString() || '');

    return this.http.get<ApiResponse>(`${this.baseUrl}/api/admin/users`, { params });
  }

  getUserStats(): Observable<ApiResponse<UserStats>> {
    return this.http.get<ApiResponse<UserStats>>(`${this.baseUrl}/api/admin/users/stats`);
  }

  getRecentUsers(count: number = 5): Observable<ApiResponse<RecentUser[]>> {
    return this.http.get<ApiResponse<RecentUser[]>>(
      `${this.baseUrl}/api/admin/users/recent?count=${count}`
    );
  }

  updateUserRole(userId: string, role: string, add: boolean): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.baseUrl}/api/admin/users/role`, {
      userId,
      role,
      add
    });
  }

  toggleUserLock(userId: string, lock: boolean, reason?: string): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.baseUrl}/api/admin/users/lock`, {
      userId,
      lock,
      reason
    });
  }

  getAllRoles(): Observable<ApiResponse> {
    return this.http.get<ApiResponse>(`${this.baseUrl}/api/admin/roles`);
  }

  // ==================== COUPONS ====================

  getAllCoupons(filter: CouponFilter): Observable<ApiResponse<Coupon[]>> {
    const params = new HttpParams()
      .set('page', filter.page.toString())
      .set('pageSize', filter.pageSize.toString())
      .set('searchTerm', filter.searchTerm || '')
      .set('isActive', filter.isActive?.toString() || '')
      .set('isExpired', filter.isExpired?.toString() || '');

    return this.http.get<ApiResponse<Coupon[]>>(`${this.baseUrl}/api/coupons`, { params });
  }

  getCouponStats(): Observable<ApiResponse<CouponStats>> {
    return this.http.get<ApiResponse<CouponStats>>(`${this.baseUrl}/api/coupons/stats`);
  }

  createCoupon(coupon: CreateCouponRequest): Observable<ApiResponse<Coupon>> {
    return this.http.post<ApiResponse<Coupon>>(`${this.baseUrl}/api/coupons`, coupon);
  }

  updateCoupon(coupon: UpdateCouponRequest): Observable<ApiResponse<Coupon>> {
    return this.http.put<ApiResponse<Coupon>>(`${this.baseUrl}/api/coupons/${coupon.id}`, coupon);
  }

  deleteCoupon(id: string): Observable<ApiResponse> {
    return this.http.delete<ApiResponse>(`${this.baseUrl}/api/coupons/${id}`);
  }

  toggleCouponStatus(id: string): Observable<ApiResponse> {
    return this.http.patch<ApiResponse>(`${this.baseUrl}/api/coupons/${id}/toggle`, {});
  }

  // ==================== PRODUCT STATS ====================

  getProductStats(): Observable<ApiResponse<ProductStats>> {
    return this.http.get<ApiResponse<ProductStats>>(`${this.baseUrl}/api/products/admin/stats`);
  }
}