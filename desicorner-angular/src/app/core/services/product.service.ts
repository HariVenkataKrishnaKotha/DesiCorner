import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { Store } from '@ngrx/store';
import { Actions, ofType } from '@ngrx/effects';
import { AppState } from '../../store';
import { ProductActions } from '../../store/product/product.actions';
import {
  selectAllProducts,
  selectAllCategories,
} from '../../store/product/product.selectors';
import { Product, Category, CreateProductRequest } from '../models/product.models';
import { ApiResponse } from '../models/response.models';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private store = inject(Store<AppState>);
  private actions$ = inject(Actions);

  /** Observable for components — same shape as the old BehaviorSubject */
  public products$ = this.store.select(selectAllProducts);
  public categories$ = this.store.select(selectAllCategories);

  /** Load all products */
  loadProducts(): Observable<ApiResponse<Product[]>> {
    this.store.dispatch(ProductActions.loadProducts());
    return this.actions$.pipe(
      ofType(ProductActions.loadProductsSuccess, ProductActions.loadProductsFailure),
      take(1),
      map(action => {
        if (action.type === ProductActions.loadProductsSuccess.type) {
          return { isSuccess: true, message: 'Products loaded', result: (action as any).products } as ApiResponse<Product[]>;
        }
        return { isSuccess: false, message: (action as any).error } as ApiResponse<Product[]>;
      })
    );
  }

  /** Load all categories */
  loadCategories(): Observable<ApiResponse<Category[]>> {
    this.store.dispatch(ProductActions.loadCategories());
    return this.actions$.pipe(
      ofType(ProductActions.loadCategoriesSuccess, ProductActions.loadCategoriesFailure),
      take(1),
      map(action => {
        if (action.type === ProductActions.loadCategoriesSuccess.type) {
          return { isSuccess: true, message: 'Categories loaded', result: (action as any).categories } as ApiResponse<Category[]>;
        }
        return { isSuccess: false, message: (action as any).error } as ApiResponse<Category[]>;
      })
    );
  }

  /** Get single product by ID */
  getProductById(id: string): Observable<ApiResponse<Product>> {
    this.store.dispatch(ProductActions.loadProductById({ productId: id }));
    return this.actions$.pipe(
      ofType(ProductActions.loadProductByIdSuccess, ProductActions.loadProductByIdFailure),
      take(1),
      map(action => {
        if (action.type === ProductActions.loadProductByIdSuccess.type) {
          return { isSuccess: true, message: 'Product loaded', result: (action as any).product } as ApiResponse<Product>;
        }
        return { isSuccess: false, message: (action as any).error } as ApiResponse<Product>;
      })
    );
  }

  /** Get products by category */
  getProductsByCategory(categoryId: string): Observable<ApiResponse<Product[]>> {
    this.store.dispatch(ProductActions.loadProductsByCategory({ categoryId }));
    return this.actions$.pipe(
      ofType(ProductActions.loadProductsByCategorySuccess, ProductActions.loadProductsByCategoryFailure),
      take(1),
      map(action => {
        if (action.type === ProductActions.loadProductsByCategorySuccess.type) {
          return { isSuccess: true, message: 'Products loaded', result: (action as any).products } as ApiResponse<Product[]>;
        }
        return { isSuccess: false, message: (action as any).error } as ApiResponse<Product[]>;
      })
    );
  }

  /** Create product (admin) */
  createProduct(product: CreateProductRequest): Observable<ApiResponse<Product>> {
    this.store.dispatch(ProductActions.createProduct({ product }));
    return this.actions$.pipe(
      ofType(ProductActions.createProductSuccess, ProductActions.createProductFailure),
      take(1),
      map(action => {
        if (action.type === ProductActions.createProductSuccess.type) {
          return { isSuccess: true, message: 'Product created', result: (action as any).product } as ApiResponse<Product>;
        }
        return { isSuccess: false, message: (action as any).error } as ApiResponse<Product>;
      })
    );
  }

  /** Update product (admin) */
  updateProduct(id: string, product: Partial<Product>): Observable<ApiResponse<Product>> {
    this.store.dispatch(ProductActions.updateProduct({ id, product }));
    return this.actions$.pipe(
      ofType(ProductActions.updateProductSuccess, ProductActions.updateProductFailure),
      take(1),
      map(action => {
        if (action.type === ProductActions.updateProductSuccess.type) {
          return { isSuccess: true, message: 'Product updated', result: (action as any).product } as ApiResponse<Product>;
        }
        return { isSuccess: false, message: (action as any).error } as ApiResponse<Product>;
      })
    );
  }

  /** Delete product (admin) */
  deleteProduct(id: string): Observable<ApiResponse> {
    this.store.dispatch(ProductActions.deleteProduct({ productId: id }));
    return this.actions$.pipe(
      ofType(ProductActions.deleteProductSuccess, ProductActions.deleteProductFailure),
      take(1),
      map(action => {
        if (action.type === ProductActions.deleteProductSuccess.type) {
          return { isSuccess: true, message: 'Product deleted' } as ApiResponse;
        }
        return { isSuccess: false, message: (action as any).error } as ApiResponse;
      })
    );
  }

  /** Get current products snapshot (synchronous) */
  get currentProducts(): Product[] {
    let products: Product[] = [];
    this.store.select(selectAllProducts).pipe(take(1)).subscribe(v => products = v);
    return products;
  }

  /** Get current categories snapshot (synchronous) */
  get currentCategories(): Category[] {
    let categories: Category[] = [];
    this.store.select(selectAllCategories).pipe(take(1)).subscribe(v => categories = v);
    return categories;
  }
}
