import { Injectable, inject } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { ApiService } from './api.service';
import { Product, Category, CreateProductRequest } from '../models/product.models';
import { ApiResponse } from '../models/response.models';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private api = inject(ApiService);

  private productsSubject = new BehaviorSubject<Product[]>([]);
  private categoriesSubject = new BehaviorSubject<Category[]>([]);

  products$ = this.productsSubject.asObservable();
  categories$ = this.categoriesSubject.asObservable();

  loadProducts(): Observable<ApiResponse<Product[]>> {
    return this.api.get<ApiResponse<Product[]>>('/api/products').pipe(
      tap(response => {
        if (response.isSuccess && response.result) {
          this.productsSubject.next(response.result);
        }
      })
    );
  }

  loadCategories(): Observable<ApiResponse<Category[]>> {
    return this.api.get<ApiResponse<Category[]>>('/api/categories').pipe(
      tap(response => {
        if (response.isSuccess && response.result) {
          this.categoriesSubject.next(response.result);
        }
      })
    );
  }

  getProductById(id: string): Observable<ApiResponse<Product>> {
    return this.api.get<ApiResponse<Product>>(`/api/products/${id}`);
  }

  getProductsByCategory(categoryId: string): Observable<ApiResponse<Product[]>> {
    return this.api.get<ApiResponse<Product[]>>(`/api/products/category/${categoryId}`);
  }

  createProduct(product: CreateProductRequest): Observable<ApiResponse<Product>> {
    return this.api.post<ApiResponse<Product>>('/api/products', product);
  }

  updateProduct(id: string, product: Partial<Product>): Observable<ApiResponse<Product>> {
    return this.api.put<ApiResponse<Product>>(`/api/products/${id}`, product);
  }

  deleteProduct(id: string): Observable<ApiResponse> {
    return this.api.delete<ApiResponse>(`/api/products/${id}`);
  }

  get currentProducts(): Product[] {
    return this.productsSubject.value;
  }

  get currentCategories(): Category[] {
    return this.categoriesSubject.value;
  }
}