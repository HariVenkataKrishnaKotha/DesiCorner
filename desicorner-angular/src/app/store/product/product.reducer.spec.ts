import { Category, Product } from '../../core/models/product.models';
import { ProductActions } from './product.actions';
import { ProductState, initialProductState, productReducer } from './product.reducer';
import { selectAllCategories, selectAllProducts } from './product.selectors';

describe('productReducer', () => {
  const samosa = { id: 'p1', name: 'Samosa', price: 4.5 } as Product;
  const chai = { id: 'p2', name: 'Chai', price: 3 } as Product;
  const snacks = { id: 'c1', name: 'Snacks' } as Category;
  const withProducts: ProductState = { ...initialProductState, products: [samosa, chai], selectedProduct: samosa };

  it('returns the initial state for an unknown action', () => {
    expect(productReducer(undefined, { type: 'unknown' })).toEqual(initialProductState);
  });

  it('loadProducts starts loading and clears the error; success stores products', () => {
    const loading = productReducer({ ...initialProductState, error: 'old' }, ProductActions.loadProducts());
    expect(loading.loading).toBeTrue();
    expect(loading.error).toBeNull();

    const done = productReducer(loading, ProductActions.loadProductsSuccess({ products: [samosa] }));
    expect(done.products).toEqual([samosa]);
    expect(done.loading).toBeFalse();
  });

  it('loadProductsFailure stops loading and records the error', () => {
    const state = productReducer({ ...initialProductState, loading: true }, ProductActions.loadProductsFailure({ error: 'down' }));

    expect(state.loading).toBeFalse();
    expect(state.error).toBe('down');
  });

  it('loads categories, and records a category failure', () => {
    expect(productReducer(initialProductState, ProductActions.loadCategoriesSuccess({ categories: [snacks] })).categories).toEqual([snacks]);
    expect(productReducer(initialProductState, ProductActions.loadCategoriesFailure({ error: 'x' })).error).toBe('x');
  });

  it('loads products by category, replacing the product list', () => {
    const loading = productReducer(withProducts, ProductActions.loadProductsByCategory({ categoryId: 'c1' }));
    expect(loading.loading).toBeTrue();

    const done = productReducer(loading, ProductActions.loadProductsByCategorySuccess({ products: [chai] }));
    expect(done.products).toEqual([chai]);
    expect(done.loading).toBeFalse();

    const failed = productReducer(loading, ProductActions.loadProductsByCategoryFailure({ error: 'x' }));
    expect(failed.loading).toBeFalse();
    expect(failed.error).toBe('x');
  });

  it('loadProductByIdSuccess selects the product; failure records the error', () => {
    expect(productReducer(initialProductState, ProductActions.loadProductByIdSuccess({ product: chai })).selectedProduct).toBe(chai);
    expect(productReducer(initialProductState, ProductActions.loadProductByIdFailure({ error: 'nf' })).error).toBe('nf');
  });

  it('createProductSuccess appends the new product', () => {
    const naan = { id: 'p3', name: 'Naan' } as Product;

    expect(productReducer(withProducts, ProductActions.createProductSuccess({ product: naan })).products).toEqual([samosa, chai, naan]);
  });

  it('updateProductSuccess replaces the product in the list and in the selection', () => {
    const updated = { ...samosa, price: 5 };
    const state = productReducer(withProducts, ProductActions.updateProductSuccess({ product: updated }));

    expect(state.products).toEqual([updated, chai]);
    expect(state.selectedProduct).toEqual(updated);
  });

  it('updateProductSuccess for another product leaves the selection alone', () => {
    const updated = { ...chai, price: 2 };

    expect(productReducer(withProducts, ProductActions.updateProductSuccess({ product: updated })).selectedProduct).toBe(samosa);
  });

  it('deleteProductSuccess removes the product', () => {
    expect(productReducer(withProducts, ProductActions.deleteProductSuccess({ productId: 'p1' })).products).toEqual([chai]);
  });

  it('selectors expose products and categories', () => {
    const root = { product: { ...withProducts, categories: [snacks] } };

    expect(selectAllProducts(root)).toEqual([samosa, chai]);
    expect(selectAllCategories(root)).toEqual([snacks]);
  });
});
