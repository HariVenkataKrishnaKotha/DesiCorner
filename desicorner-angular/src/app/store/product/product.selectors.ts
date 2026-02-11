import { createFeatureSelector, createSelector } from '@ngrx/store';
import { ProductState } from './product.reducer';

export const selectProductState = createFeatureSelector<ProductState>('product');

export const selectAllProducts = createSelector(
  selectProductState,
  (state) => state.products
);

export const selectAllCategories = createSelector(
  selectProductState,
  (state) => state.categories
);

export const selectSelectedProduct = createSelector(
  selectProductState,
  (state) => state.selectedProduct
);

export const selectProductLoading = createSelector(
  selectProductState,
  (state) => state.loading
);

export const selectProductError = createSelector(
  selectProductState,
  (state) => state.error
);

export const selectProductById = (productId: string) => createSelector(
  selectAllProducts,
  (products) => products.find(p => p.id === productId) ?? null
);

export const selectProductsByCategory = (categoryId: string) => createSelector(
  selectAllProducts,
  (products) => products.filter(p => p.categoryId === categoryId)
);
