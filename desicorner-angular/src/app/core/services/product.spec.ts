import { TestBed } from '@angular/core/testing';
import { Action } from '@ngrx/store';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Subject } from 'rxjs';
import { ProductActions } from '../../store/product/product.actions';
import { initialProductState } from '../../store/product/product.reducer';
import { Category, CreateProductRequest, Product } from '../models/product.models';
import { ApiResponse } from '../models/response.models';
import { ProductService } from './product.service';

describe('ProductService', () => {
  let actions$: Subject<Action>;
  let store: MockStore;
  let service: ProductService;

  const samosa = { id: 'p1', name: 'Samosa', price: 4.5 } as Product;
  const snacks = { id: 'c1', name: 'Snacks' } as Category;

  beforeEach(() => {
    actions$ = new Subject<Action>();
    TestBed.configureTestingModule({
      providers: [
        provideMockStore({ initialState: { product: initialProductState } }),
        provideMockActions(() => actions$),
      ],
    });
    store = TestBed.inject(MockStore);
    spyOn(store, 'dispatch');
    service = TestBed.inject(ProductService);
  });

  /** Calls a facade method, emits one action, and returns what the caller received. */
  function respond<T>(call: () => import('rxjs').Observable<ApiResponse<T>>, action: Action): ApiResponse<T> | undefined {
    let result: ApiResponse<T> | undefined;
    call().subscribe(r => (result = r));
    actions$.next(action);
    return result;
  }

  it('loadProducts dispatches loadProducts and maps success and failure', () => {
    expect(respond(() => service.loadProducts(), ProductActions.loadProductsSuccess({ products: [samosa] })))
      .toEqual({ isSuccess: true, message: 'Products loaded', result: [samosa] });
    expect(store.dispatch).toHaveBeenCalledWith(ProductActions.loadProducts());

    expect(respond(() => service.loadProducts(), ProductActions.loadProductsFailure({ error: 'Gateway down' })))
      .toEqual({ isSuccess: false, message: 'Gateway down' });
  });

  it('loadCategories dispatches loadCategories and maps success and failure', () => {
    expect(respond(() => service.loadCategories(), ProductActions.loadCategoriesSuccess({ categories: [snacks] })))
      .toEqual({ isSuccess: true, message: 'Categories loaded', result: [snacks] });
    expect(store.dispatch).toHaveBeenCalledWith(ProductActions.loadCategories());

    expect(respond(() => service.loadCategories(), ProductActions.loadCategoriesFailure({ error: 'x' }))?.isSuccess)
      .toBeFalse();
  });

  it('getProductById dispatches loadProductById and maps success and failure', () => {
    expect(respond(() => service.getProductById('p1'), ProductActions.loadProductByIdSuccess({ product: samosa })))
      .toEqual({ isSuccess: true, message: 'Product loaded', result: samosa });
    expect(store.dispatch).toHaveBeenCalledWith(ProductActions.loadProductById({ productId: 'p1' }));

    expect(respond(() => service.getProductById('nope'), ProductActions.loadProductByIdFailure({ error: 'Not found' })))
      .toEqual({ isSuccess: false, message: 'Not found' });
  });

  it('getProductsByCategory dispatches loadProductsByCategory and maps the result', () => {
    expect(respond(() => service.getProductsByCategory('c1'), ProductActions.loadProductsByCategorySuccess({ products: [samosa] })))
      .toEqual({ isSuccess: true, message: 'Products loaded', result: [samosa] });
    expect(store.dispatch).toHaveBeenCalledWith(ProductActions.loadProductsByCategory({ categoryId: 'c1' }));

    expect(respond(() => service.getProductsByCategory('c1'), ProductActions.loadProductsByCategoryFailure({ error: 'x' }))?.isSuccess)
      .toBeFalse();
  });

  it('createProduct, updateProduct and deleteProduct dispatch and map their results', () => {
    const request = { name: 'Samosa', price: 4.5 } as CreateProductRequest;

    expect(respond(() => service.createProduct(request), ProductActions.createProductSuccess({ product: samosa })))
      .toEqual({ isSuccess: true, message: 'Product created', result: samosa });
    expect(store.dispatch).toHaveBeenCalledWith(ProductActions.createProduct({ product: request }));
    expect(respond(() => service.createProduct(request), ProductActions.createProductFailure({ error: 'Forbidden' })))
      .toEqual({ isSuccess: false, message: 'Forbidden' });

    expect(respond(() => service.updateProduct('p1', { price: 5 }), ProductActions.updateProductSuccess({ product: samosa })))
      .toEqual({ isSuccess: true, message: 'Product updated', result: samosa });
    expect(store.dispatch).toHaveBeenCalledWith(ProductActions.updateProduct({ id: 'p1', product: { price: 5 } }));
    expect(respond(() => service.updateProduct('p1', {}), ProductActions.updateProductFailure({ error: 'x' }))?.isSuccess)
      .toBeFalse();

    expect(respond(() => service.deleteProduct('p1'), ProductActions.deleteProductSuccess({ productId: 'p1' })))
      .toEqual({ isSuccess: true, message: 'Product deleted' });
    expect(store.dispatch).toHaveBeenCalledWith(ProductActions.deleteProduct({ productId: 'p1' }));
    expect(respond(() => service.deleteProduct('p1'), ProductActions.deleteProductFailure({ error: 'In use' })))
      .toEqual({ isSuccess: false, message: 'In use' });
  });

  it('exposes products and categories from the store', () => {
    expect(service.currentProducts).toEqual([]);
    expect(service.currentCategories).toEqual([]);

    store.setState({ product: { ...initialProductState, products: [samosa], categories: [snacks] } });

    let products: Product[] = [];
    service.products$.subscribe(p => (products = p));
    expect(products).toEqual([samosa]);
    expect(service.currentProducts).toEqual([samosa]);
    expect(service.currentCategories).toEqual([snacks]);
  });
});
