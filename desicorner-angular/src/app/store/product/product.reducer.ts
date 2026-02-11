import { createReducer, on } from '@ngrx/store';
import { Product, Category } from '../../core/models/product.models';
import { ProductActions } from './product.actions';

export interface ProductState {
  products: Product[];
  categories: Category[];
  selectedProduct: Product | null;
  loading: boolean;
  error: string | null;
}

export const initialProductState: ProductState = {
  products: [],
  categories: [],
  selectedProduct: null,
  loading: false,
  error: null,
};

export const productReducer = createReducer(
  initialProductState,

  // Load Products
  on(ProductActions.loadProducts, (state) => ({
    ...state,
    loading: true,
    error: null,
  })),
  on(ProductActions.loadProductsSuccess, (state, { products }) => ({
    ...state,
    products,
    loading: false,
  })),
  on(ProductActions.loadProductsFailure, (state, { error }) => ({
    ...state,
    loading: false,
    error,
  })),

  // Load Categories
  on(ProductActions.loadCategoriesSuccess, (state, { categories }) => ({
    ...state,
    categories,
  })),
  on(ProductActions.loadCategoriesFailure, (state, { error }) => ({
    ...state,
    error,
  })),

  // Load Products by Category
  on(ProductActions.loadProductsByCategory, (state) => ({
    ...state,
    loading: true,
  })),
  on(ProductActions.loadProductsByCategorySuccess, (state, { products }) => ({
    ...state,
    products,
    loading: false,
  })),
  on(ProductActions.loadProductsByCategoryFailure, (state, { error }) => ({
    ...state,
    loading: false,
    error,
  })),

  // Load Product by ID
  on(ProductActions.loadProductByIdSuccess, (state, { product }) => ({
    ...state,
    selectedProduct: product,
  })),
  on(ProductActions.loadProductByIdFailure, (state, { error }) => ({
    ...state,
    error,
  })),

  // Create Product
  on(ProductActions.createProductSuccess, (state, { product }) => ({
    ...state,
    products: [...state.products, product],
  })),

  // Update Product
  on(ProductActions.updateProductSuccess, (state, { product }) => ({
    ...state,
    products: state.products.map(p => p.id === product.id ? product : p),
    selectedProduct: state.selectedProduct?.id === product.id ? product : state.selectedProduct,
  })),

  // Delete Product
  on(ProductActions.deleteProductSuccess, (state, { productId }) => ({
    ...state,
    products: state.products.filter(p => p.id !== productId),
  })),
);
