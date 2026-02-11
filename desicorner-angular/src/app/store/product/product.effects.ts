import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { HttpClient } from '@angular/common/http';
import { switchMap, map, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { ProductActions } from './product.actions';
import { environment } from '@env/environment';
import { ApiResponse } from '../../core/models/response.models';
import { Product, Category } from '../../core/models/product.models';

@Injectable()
export class ProductEffects {
  private actions$ = inject(Actions);
  private http = inject(HttpClient);

  /** Load all products */
  loadProducts$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ProductActions.loadProducts),
      switchMap(() =>
        this.http.get<ApiResponse<Product[]>>(`${environment.gatewayUrl}/api/products`).pipe(
          map(response => {
            if (response?.isSuccess && response.result) {
              return ProductActions.loadProductsSuccess({ products: response.result });
            }
            return ProductActions.loadProductsFailure({ error: response?.message || 'Failed to load products' });
          }),
          catchError(error => of(ProductActions.loadProductsFailure({
            error: error.error?.message || 'Failed to load products'
          })))
        )
      )
    )
  );

  /** Load all categories */
  loadCategories$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ProductActions.loadCategories),
      switchMap(() =>
        this.http.get<ApiResponse<Category[]>>(`${environment.gatewayUrl}/api/categories`).pipe(
          map(response => {
            if (response?.isSuccess && response.result) {
              return ProductActions.loadCategoriesSuccess({ categories: response.result });
            }
            return ProductActions.loadCategoriesFailure({ error: response?.message || 'Failed to load categories' });
          }),
          catchError(error => of(ProductActions.loadCategoriesFailure({
            error: error.error?.message || 'Failed to load categories'
          })))
        )
      )
    )
  );

  /** Load products by category */
  loadProductsByCategory$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ProductActions.loadProductsByCategory),
      switchMap(({ categoryId }) =>
        this.http.get<ApiResponse<Product[]>>(`${environment.gatewayUrl}/api/products/category/${categoryId}`).pipe(
          map(response => {
            if (response?.isSuccess && response.result) {
              return ProductActions.loadProductsByCategorySuccess({ products: response.result });
            }
            return ProductActions.loadProductsByCategoryFailure({ error: response?.message || 'Failed to load products' });
          }),
          catchError(error => of(ProductActions.loadProductsByCategoryFailure({
            error: error.error?.message || 'Failed to load products'
          })))
        )
      )
    )
  );

  /** Load single product by ID */
  loadProductById$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ProductActions.loadProductById),
      switchMap(({ productId }) =>
        this.http.get<ApiResponse<Product>>(`${environment.gatewayUrl}/api/products/${productId}`).pipe(
          map(response => {
            if (response?.isSuccess && response.result) {
              return ProductActions.loadProductByIdSuccess({ product: response.result });
            }
            return ProductActions.loadProductByIdFailure({ error: response?.message || 'Product not found' });
          }),
          catchError(error => of(ProductActions.loadProductByIdFailure({
            error: error.error?.message || 'Failed to load product'
          })))
        )
      )
    )
  );

  /** Create product (admin) */
  createProduct$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ProductActions.createProduct),
      switchMap(({ product }) =>
        this.http.post<ApiResponse<Product>>(`${environment.gatewayUrl}/api/products`, product).pipe(
          map(response => {
            if (response?.isSuccess && response.result) {
              return ProductActions.createProductSuccess({ product: response.result });
            }
            return ProductActions.createProductFailure({ error: response?.message || 'Failed to create product' });
          }),
          catchError(error => of(ProductActions.createProductFailure({
            error: error.error?.message || 'Failed to create product'
          })))
        )
      )
    )
  );

  /** Update product (admin) */
  updateProduct$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ProductActions.updateProduct),
      switchMap(({ id, product }) =>
        this.http.put<ApiResponse<Product>>(`${environment.gatewayUrl}/api/products/${id}`, product).pipe(
          map(response => {
            if (response?.isSuccess && response.result) {
              return ProductActions.updateProductSuccess({ product: response.result });
            }
            return ProductActions.updateProductFailure({ error: response?.message || 'Failed to update product' });
          }),
          catchError(error => of(ProductActions.updateProductFailure({
            error: error.error?.message || 'Failed to update product'
          })))
        )
      )
    )
  );

  /** Delete product (admin) */
  deleteProduct$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ProductActions.deleteProduct),
      switchMap(({ productId }) =>
        this.http.delete<ApiResponse>(`${environment.gatewayUrl}/api/products/${productId}`).pipe(
          map(response => {
            if (response?.isSuccess) {
              return ProductActions.deleteProductSuccess({ productId });
            }
            return ProductActions.deleteProductFailure({ error: response?.message || 'Failed to delete product' });
          }),
          catchError(error => of(ProductActions.deleteProductFailure({
            error: error.error?.message || 'Failed to delete product'
          })))
        )
      )
    )
  );
}
