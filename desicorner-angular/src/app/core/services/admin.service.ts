import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/response.models';
import { Product, Category, CreateProductRequest } from '../models/product.models';
import { environment } from '@env/environment';

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
}