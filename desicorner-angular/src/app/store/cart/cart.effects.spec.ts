import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Action } from '@ngrx/store';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { getEffectsMetadata } from '@ngrx/effects';
import { Observable, of } from 'rxjs';
import { environment } from '@env/environment';
import { Cart } from '../../core/models/cart.models';
import { Product } from '../../core/models/product.models';
import { AuthActions } from '../auth/auth.actions';
import { CartActions } from './cart.actions';
import { CartEffects } from './cart.effects';
import { initialCartState } from './cart.reducer';

const STORAGE_KEY = 'desicorner_cart';
const GUEST_SESSION_KEY = 'desicorner_guest_session';

describe('CartEffects', () => {
  const cartApi = `${environment.gatewayUrl}/api/cart`;

  /** A cart as CartAPI returns it (backend field names). */
  const backendCart = {
    id: 'c1',
    items: [{ id: 'i1', productId: 'p1', productName: 'Samosa', productImage: '/s.png', price: 4.5, quantity: 2 }],
    subTotal: 9,
    taxAmount: 0.54,
    deliveryFee: 5,
    discountAmount: 1,
    total: 13.54,
    couponCode: 'SAVE1',
  };
  /** The same cart after mapping to the front-end model. */
  const mappedCart: Cart = {
    id: 'c1',
    items: [{
      id: 'i1', productId: 'p1', productName: 'Samosa', productImage: '/s.png', price: 4.5, quantity: 2,
      isVegetarian: false, isSpicy: false, spiceLevel: 0,
    }],
    subtotal: 9,
    tax: 0.54,
    deliveryFee: 5,
    discount: 1,
    total: 13.54,
    couponCode: 'SAVE1',
  };
  const emptyCart: Cart = { items: [], subtotal: 0, tax: 0, deliveryFee: 0, discount: 0, total: 0 };

  const chai = {
    id: 'p2', name: 'Chai', price: 10, imageUrl: '/chai.png', isVegetarian: true, isSpicy: false, spiceLevel: 0,
  } as Product;

  let actions$: Observable<Action>;
  let effects: CartEffects;
  let httpMock: HttpTestingController;
  let store: MockStore;

  function setStoreCart(cart: Cart): void {
    store.setState({ cart: { ...initialCartState, cart } });
  }

  function run(effect: Observable<Action>): Action[] {
    const out: Action[] = [];
    effect.subscribe(a => out.push(a));
    return out;
  }

  function stored(): Cart | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : null;
  }

  beforeEach(() => {
    localStorage.removeItem(STORAGE_KEY);
    TestBed.configureTestingModule({
      providers: [
        CartEffects,
        provideMockActions(() => actions$),
        provideMockStore({ initialState: { cart: initialCartState } }),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    effects = TestBed.inject(CartEffects);
    httpMock = TestBed.inject(HttpTestingController);
    store = TestBed.inject(MockStore);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.removeItem(STORAGE_KEY);
  });

  it('loads the cart once auth state is known', () => {
    for (const action of [AuthActions.checkAuthSuccess(), AuthActions.checkAuthNoToken()]) {
      actions$ = of(action);
      expect(run(effects.loadCartOnAuthReady$)).withContext(action.type).toEqual([CartActions.loadCart()]);
    }
  });

  it('does not load the cart when the profile arrives (a sign-in loads it after the merge)', () => {
    actions$ = of(AuthActions.loadUserProfileSuccess({ profile: {} as never }));

    expect(run(effects.loadCartOnAuthReady$)).toEqual([]);
  });

  describe('guest cart merge on sign-in', () => {
    const mergeUrl = `${cartApi}/merge`;

    /** Runs every effect CartEffects registers and collects what the dispatching ones emit. */
    function runAll(): Action[] {
      const out: Action[] = [];
      const metadata = getEffectsMetadata(effects);
      for (const key of Object.keys(metadata) as (keyof CartEffects)[]) {
        const effect = effects[key] as unknown as Observable<Action>;
        if (metadata[key]?.dispatch === false) {
          effect.subscribe();
        } else {
          effect.subscribe(a => out.push(a));
        }
      }
      return out;
    }

    const merged = {
      isSuccess: true,
      message: 'Guest cart merged',
      result: { cart: { ...backendCart }, droppedItems: [], droppedCouponCode: null },
    };

    beforeEach(() => localStorage.setItem(GUEST_SESSION_KEY, 'guest-1'));
    afterEach(() => localStorage.removeItem(GUEST_SESSION_KEY));

    it('on a fresh sign-in, POSTs the guest session to /api/cart/merge and reports success', () => {
      actions$ = of(AuthActions.signInCompleted());
      const out = runAll();

      const req = httpMock.expectOne(mergeUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.headers.get('X-Session-Id')).toBe('guest-1');
      req.flush(merged);

      expect(out).toEqual([CartActions.mergeGuestCartSuccess()]);
    });

    it('clears the guest session after a successful merge', () => {
      // provideMockActions doesn't feed emitted actions back in, so send the success directly
      actions$ = of(CartActions.mergeGuestCartSuccess());

      runAll();

      expect(localStorage.getItem(GUEST_SESSION_KEY)).toBeNull();
    });

    it('keeps the guest session when the merge fails', () => {
      actions$ = of(CartActions.mergeGuestCartFailure({ error: 'x' }));

      runAll();

      expect(localStorage.getItem(GUEST_SESSION_KEY)).toBe('guest-1');
    });

    it('keeps the guest session and reports failure when the merge fails', () => {
      actions$ = of(AuthActions.signInCompleted());
      const out = runAll();

      httpMock.expectOne(mergeUrl).flush(
        { isSuccess: false, message: 'Products could not be checked right now' },
        { status: 503, statusText: 'Service Unavailable' });

      expect(out).toEqual([CartActions.mergeGuestCartFailure({ error: 'Products could not be checked right now' })]);
      expect(localStorage.getItem(GUEST_SESSION_KEY)).toBe('guest-1');
    });

    it('without a guest session, skips the merge and loads the cart', () => {
      localStorage.removeItem(GUEST_SESSION_KEY);
      actions$ = of(AuthActions.signInCompleted());

      const out = runAll();

      httpMock.expectNone(mergeUrl);
      expect(out).toEqual([CartActions.loadCart()]);
    });

    for (const finished of [CartActions.mergeGuestCartSuccess(), CartActions.mergeGuestCartFailure({ error: 'x' })]) {
      it(`loads the cart after ${finished.type}`, () => {
        actions$ = of(finished);

        expect(runAll()).toEqual([CartActions.loadCart()]);
      });
    }

    for (const notASignIn of [
      AuthActions.checkAuthSuccess(),
      AuthActions.pkceCallbackSuccess(),
      AuthActions.loadUserProfileSuccess({ profile: {} as never }),
    ]) {
      it(`does not merge on ${notASignIn.type} (a page load with a stored token)`, () => {
        actions$ = of(notASignIn);

        const out = runAll();

        httpMock.expectNone(mergeUrl);
        expect(out.map(a => a.type)).not.toContain(CartActions.mergeGuestCartSuccess.type);
        expect(localStorage.getItem(GUEST_SESSION_KEY)).toBe('guest-1');
      });
    }
  });

  describe('loadCart$', () => {
    beforeEach(() => (actions$ = of(CartActions.loadCart())));

    it('GETs the cart, maps backend field names and persists it', () => {
      const out = run(effects.loadCart$);

      const req = httpMock.expectOne(cartApi);
      expect(req.request.method).toBe('GET');
      req.flush({ isSuccess: true, message: '', result: backendCart });

      expect(out).toEqual([CartActions.loadCartSuccess({ cart: mappedCart })]);
      expect(stored()).toEqual(mappedCart);
    });

    it('falls back to the persisted cart when the request fails', () => {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(mappedCart));
      const out = run(effects.loadCart$);

      httpMock.expectOne(cartApi).flush(null, { status: 503, statusText: 'Unavailable' });

      expect(out).toEqual([CartActions.loadCartFailure({ cart: mappedCart })]);
    });

    it('falls back to an empty cart when nothing is persisted and the API returns no cart', () => {
      const out = run(effects.loadCart$);

      httpMock.expectOne(cartApi).flush({ isSuccess: false, message: 'Either user must be authenticated or X-Session-Id header must be provided' });

      expect(out).toEqual([CartActions.loadCartFailure({ cart: emptyCart })]);
    });
  });

  describe('addItem$', () => {
    beforeEach(() => (actions$ = of(CartActions.addItem({ product: chai, quantity: 2 }))));

    it('POSTs the product id and quantity and maps the returned cart', () => {
      const out = run(effects.addItem$);

      const req = httpMock.expectOne(`${cartApi}/add`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ productId: 'p2', quantity: 2 });
      req.flush({ isSuccess: true, message: '', result: backendCart });

      expect(out).toEqual([CartActions.addItemSuccess({ cart: mappedCart })]);
    });

    it('on failure adds the item locally, with 6% tax and $5 delivery under $50', () => {
      const out = run(effects.addItem$);
      httpMock.expectOne(`${cartApi}/add`).flush(null, { status: 500, statusText: 'Error' });

      const action = out[0] as ReturnType<typeof CartActions.addItemLocalFallback>;
      expect(action.type).toBe(CartActions.addItemLocalFallback.type);
      expect(action.cart.items.length).toBe(1);
      expect(action.cart.items[0]).toEqual(jasmine.objectContaining({
        productId: 'p2', productName: 'Chai', productImage: '/chai.png', price: 10, quantity: 2, isVegetarian: true,
      }));
      expect(action.cart.items[0].id).toEqual(jasmine.any(String));
      expect(action.cart.subtotal).toBe(20);
      expect(action.cart.tax).toBeCloseTo(1.2, 10);
      expect(action.cart.deliveryFee).toBe(5);
      expect(action.cart.total).toBeCloseTo(26.2, 10);
      expect(stored()).toEqual(action.cart);
    });

    it('on failure increments an existing line and drops the delivery fee at $50', () => {
      setStoreCart({
        ...emptyCart,
        discount: 2,
        items: [{ id: 'i9', productId: 'p2', productName: 'Chai', price: 10, quantity: 3, isVegetarian: true, isSpicy: false, spiceLevel: 0 }],
      });
      const out = run(effects.addItem$);
      httpMock.expectOne(`${cartApi}/add`).error(new ProgressEvent('error'));

      const cart = (out[0] as ReturnType<typeof CartActions.addItemLocalFallback>).cart;
      expect(cart.items.length).toBe(1);
      expect(cart.items[0].quantity).toBe(5);
      expect(cart.subtotal).toBe(50);
      expect(cart.deliveryFee).toBe(0);
      expect(cart.total).toBeCloseTo(50 + 3 - 2, 10);
    });
  });

  describe('removeItem$', () => {
    beforeEach(() => (actions$ = of(CartActions.removeItem({ cartItemId: 'i1' }))));

    it('DELETEs the item and maps the returned cart', () => {
      const out = run(effects.removeItem$);

      const req = httpMock.expectOne(`${cartApi}/item/i1`);
      expect(req.request.method).toBe('DELETE');
      req.flush({ isSuccess: true, message: '', result: { ...backendCart, items: [] } });

      expect((out[0] as ReturnType<typeof CartActions.removeItemSuccess>).cart.items).toEqual([]);
      expect(out[0].type).toBe(CartActions.removeItemSuccess.type);
    });

    it('on failure removes the item locally and recalculates', () => {
      setStoreCart({
        ...emptyCart,
        items: [
          { id: 'i1', productId: 'p1', productName: 'Samosa', price: 4.5, quantity: 2, isVegetarian: true, isSpicy: false, spiceLevel: 0 },
          { id: 'i2', productId: 'p2', productName: 'Chai', price: 10, quantity: 1, isVegetarian: true, isSpicy: false, spiceLevel: 0 },
        ],
      });
      const out = run(effects.removeItem$);
      httpMock.expectOne(`${cartApi}/item/i1`).flush(null, { status: 500, statusText: 'Error' });

      const cart = (out[0] as ReturnType<typeof CartActions.removeItemLocalFallback>).cart;
      expect(out[0].type).toBe(CartActions.removeItemLocalFallback.type);
      expect(cart.items.map(i => i.id)).toEqual(['i2']);
      expect(cart.subtotal).toBe(10);
      expect(cart.total).toBeCloseTo(10 + 0.6 + 5, 10);
    });
  });

  describe('updateQuantity$', () => {
    beforeEach(() => (actions$ = of(CartActions.updateQuantity({ cartItemId: 'i1', quantity: 3 }))));

    it('PUTs the item id and quantity and maps the returned cart', () => {
      const out = run(effects.updateQuantity$);

      const req = httpMock.expectOne(`${cartApi}/update`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({ cartItemId: 'i1', quantity: 3 });
      req.flush({ isSuccess: true, message: '', result: backendCart });

      expect(out).toEqual([CartActions.updateQuantitySuccess({ cart: mappedCart })]);
    });

    it('emits failure when the API does not return a cart', () => {
      const out = run(effects.updateQuantity$);
      httpMock.expectOne(`${cartApi}/update`).flush({ isSuccess: false, message: 'x' });

      expect(out).toEqual([CartActions.updateQuantityFailure({ error: 'Failed to update quantity' })]);
    });
  });

  describe('coupons', () => {
    beforeEach(() => setStoreCart(mappedCart));

    it('applyCoupon$ POSTs the current cart id with the code', () => {
      actions$ = of(CartActions.applyCoupon({ couponCode: 'WELCOME10' }));
      const out = run(effects.applyCoupon$);

      const req = httpMock.expectOne(`${cartApi}/apply-coupon`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ cartId: 'c1', couponCode: 'WELCOME10' });
      req.flush({ isSuccess: true, message: '', result: backendCart });

      expect(out).toEqual([CartActions.applyCouponSuccess({ cart: mappedCart })]);
    });

    it('applyCoupon$ reports the API message, or the server error message', () => {
      actions$ = of(CartActions.applyCoupon({ couponCode: 'BOGUS' }));

      let out = run(effects.applyCoupon$);
      httpMock.expectOne(`${cartApi}/apply-coupon`).flush({ isSuccess: false, message: 'Coupon expired' });
      expect(out).toEqual([CartActions.applyCouponFailure({ error: 'Coupon expired' })]);

      out = run(effects.applyCoupon$);
      httpMock.expectOne(`${cartApi}/apply-coupon`).flush({ message: 'Invalid coupon code' }, { status: 400, statusText: 'Bad Request' });
      expect(out).toEqual([CartActions.applyCouponFailure({ error: 'Invalid coupon code' })]);
    });

    it('removeCoupon$ POSTs to remove-coupon/{cartId}', () => {
      actions$ = of(CartActions.removeCoupon());
      const out = run(effects.removeCoupon$);

      const req = httpMock.expectOne(`${cartApi}/remove-coupon/c1`);
      expect(req.request.method).toBe('POST');
      req.flush({ isSuccess: true, message: '', result: { ...backendCart, couponCode: null, discountAmount: 0 } });

      const cart = (out[0] as ReturnType<typeof CartActions.removeCouponSuccess>).cart;
      expect(out[0].type).toBe(CartActions.removeCouponSuccess.type);
      expect(cart.discount).toBe(0);
    });

    it('removeCoupon$ reports failure', () => {
      actions$ = of(CartActions.removeCoupon());
      const out = run(effects.removeCoupon$);

      httpMock.expectOne(`${cartApi}/remove-coupon/c1`).flush(null, { status: 500, statusText: 'Error' });

      expect(out).toEqual([CartActions.removeCouponFailure({ error: 'Failed to remove coupon' })]);
    });
  });

  describe('clearCart$', () => {
    beforeEach(() => {
      actions$ = of(CartActions.clearCart());
      localStorage.setItem(STORAGE_KEY, JSON.stringify(mappedCart));
    });

    it('DELETEs the cart and persists an empty one', () => {
      const out = run(effects.clearCart$);

      const req = httpMock.expectOne(`${cartApi}/clear`);
      expect(req.request.method).toBe('DELETE');
      req.flush({ isSuccess: true });

      expect(out).toEqual([CartActions.clearCartSuccess()]);
      expect(stored()).toEqual(emptyCart);
    });

    it('still clears locally when the request fails', () => {
      const out = run(effects.clearCart$);
      httpMock.expectOne(`${cartApi}/clear`).flush(null, { status: 500, statusText: 'Error' });

      expect(out).toEqual([CartActions.clearCartSuccess()]);
      expect(stored()).toEqual(emptyCart);
    });
  });

  it('persists the cart after a delivery-fee change, once the reducer has run', fakeAsync(() => {
    setStoreCart(mappedCart);
    actions$ = of(CartActions.setDeliveryFee({ orderType: 'Pickup' }));

    run(effects.persistOnDeliveryFee$);
    expect(stored()).toBeNull();
    tick();

    expect(stored()).toEqual(mappedCart);
  }));
});
