import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Action } from '@ngrx/store';
import { provideMockActions } from '@ngrx/effects/testing';
import { Observable, of } from 'rxjs';
import { environment } from '@env/environment';
import { Category, CreateProductRequest, Product } from '../../core/models/product.models';
import { ProductActions } from './product.actions';
import { ProductEffects } from './product.effects';

describe('ProductEffects', () => {
  const api = environment.gatewayUrl;
  const samosa = { id: 'p1', name: 'Samosa', price: 4.5 } as Product;
  const snacks = { id: 'c1', name: 'Snacks' } as Category;

  let actions$: Observable<Action>;
  let effects: ProductEffects;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ProductEffects, provideMockActions(() => actions$), provideHttpClient(), provideHttpClientTesting()],
    });
    effects = TestBed.inject(ProductEffects);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  interface Case {
    name: string;
    effect: () => Observable<Action>;
    trigger: Action;
    method: string;
    url: string;
    body?: object;
    result: object | null;
    success: Action;
    failure: (error: string) => Action;
    /** Default message on an HTTP error with no body. */
    fallback: string;
  }

  const request = { name: 'Samosa', price: 4.5 } as CreateProductRequest;
  const cases: Case[] = [
    {
      name: 'loadProducts$', effect: () => effects.loadProducts$, trigger: ProductActions.loadProducts(),
      method: 'GET', url: `${api}/api/products`, result: [samosa],
      success: ProductActions.loadProductsSuccess({ products: [samosa] }),
      failure: error => ProductActions.loadProductsFailure({ error }), fallback: 'Failed to load products',
    },
    {
      name: 'loadCategories$', effect: () => effects.loadCategories$, trigger: ProductActions.loadCategories(),
      method: 'GET', url: `${api}/api/categories`, result: [snacks],
      success: ProductActions.loadCategoriesSuccess({ categories: [snacks] }),
      failure: error => ProductActions.loadCategoriesFailure({ error }), fallback: 'Failed to load categories',
    },
    {
      name: 'loadProductsByCategory$', effect: () => effects.loadProductsByCategory$,
      trigger: ProductActions.loadProductsByCategory({ categoryId: 'c1' }),
      method: 'GET', url: `${api}/api/products/category/c1`, result: [samosa],
      success: ProductActions.loadProductsByCategorySuccess({ products: [samosa] }),
      failure: error => ProductActions.loadProductsByCategoryFailure({ error }), fallback: 'Failed to load products',
    },
    {
      name: 'loadProductById$', effect: () => effects.loadProductById$,
      trigger: ProductActions.loadProductById({ productId: 'p1' }),
      method: 'GET', url: `${api}/api/products/p1`, result: samosa,
      success: ProductActions.loadProductByIdSuccess({ product: samosa }),
      failure: error => ProductActions.loadProductByIdFailure({ error }), fallback: 'Failed to load product',
    },
    {
      name: 'createProduct$', effect: () => effects.createProduct$, trigger: ProductActions.createProduct({ product: request }),
      method: 'POST', url: `${api}/api/products`, body: request, result: samosa,
      success: ProductActions.createProductSuccess({ product: samosa }),
      failure: error => ProductActions.createProductFailure({ error }), fallback: 'Failed to create product',
    },
    {
      name: 'updateProduct$', effect: () => effects.updateProduct$,
      trigger: ProductActions.updateProduct({ id: 'p1', product: { price: 5 } }),
      method: 'PUT', url: `${api}/api/products/p1`, body: { price: 5 }, result: samosa,
      success: ProductActions.updateProductSuccess({ product: samosa }),
      failure: error => ProductActions.updateProductFailure({ error }), fallback: 'Failed to update product',
    },
    {
      name: 'deleteProduct$', effect: () => effects.deleteProduct$, trigger: ProductActions.deleteProduct({ productId: 'p1' }),
      method: 'DELETE', url: `${api}/api/products/p1`, result: null,
      success: ProductActions.deleteProductSuccess({ productId: 'p1' }),
      failure: error => ProductActions.deleteProductFailure({ error }), fallback: 'Failed to delete product',
    },
  ];

  function run(effect: Observable<Action>): Action[] {
    const out: Action[] = [];
    effect.subscribe(a => out.push(a));
    return out;
  }

  for (const c of cases) {
    describe(c.name, () => {
      beforeEach(() => (actions$ = of(c.trigger)));

      it(`${c.method}s ${c.url.replace(api, '')} and emits success`, () => {
        const out = run(c.effect());

        const req = httpMock.expectOne(c.url);
        expect(req.request.method).toBe(c.method);
        if (c.body) expect(req.request.body).toEqual(c.body);
        req.flush({ isSuccess: true, message: '', result: c.result });

        expect(out).toEqual([c.success]);
      });

      it('emits failure with the API message when the response is unsuccessful', () => {
        const out = run(c.effect());
        httpMock.expectOne(c.url).flush({ isSuccess: false, message: 'Rejected by API' });

        expect(out).toEqual([c.failure('Rejected by API')]);
      });

      it('emits failure with the server message, or a default, on an HTTP error', () => {
        let out = run(c.effect());
        httpMock.expectOne(c.url).flush({ message: 'Forbidden' }, { status: 403, statusText: 'Forbidden' });
        expect(out).toEqual([c.failure('Forbidden')]);

        out = run(c.effect());
        httpMock.expectOne(c.url).flush(null, { status: 500, statusText: 'Error' });
        expect(out).toEqual([c.failure(c.fallback)]);
      });
    });
  }
});
