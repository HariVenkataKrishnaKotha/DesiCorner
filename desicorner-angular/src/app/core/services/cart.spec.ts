import { TestBed } from '@angular/core/testing';
import { Action } from '@ngrx/store';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Subject } from 'rxjs';
import { CartActions } from '../../store/cart/cart.actions';
import { initialCartState } from '../../store/cart/cart.reducer';
import { Cart } from '../models/cart.models';
import { Product } from '../models/product.models';
import { ApiResponse } from '../models/response.models';
import { CartService } from './cart.service';

describe('CartService', () => {
  let actions$: Subject<Action>;
  let store: MockStore;
  let service: CartService;

  const cart: Cart = {
    id: 'c1',
    items: [
      { id: 'i1', productId: 'p1', productName: 'Samosa', price: 4, quantity: 2, isVegetarian: true, isSpicy: false, spiceLevel: 0 },
      { id: 'i2', productId: 'p2', productName: 'Chai', price: 3, quantity: 3, isVegetarian: true, isSpicy: false, spiceLevel: 0 },
    ],
    subtotal: 17,
    tax: 1.02,
    deliveryFee: 5,
    discount: 0,
    total: 23.02,
  };

  beforeEach(() => {
    actions$ = new Subject<Action>();
    TestBed.configureTestingModule({
      providers: [
        provideMockStore({ initialState: { cart: initialCartState } }),
        provideMockActions(() => actions$),
      ],
    });
    store = TestBed.inject(MockStore);
    spyOn(store, 'dispatch');
    service = TestBed.inject(CartService);
  });

  it('dispatches the matching cart action for each command', () => {
    const product = { id: 'p1' } as Product;

    service.loadCart();
    service.addItem(product);
    service.addItem(product, 3);
    service.removeItem('i1');
    service.clearCart();
    service.setDeliveryFee('Pickup');

    expect((store.dispatch as jasmine.Spy).calls.allArgs()).toEqual([
      [CartActions.loadCart()],
      [CartActions.addItem({ product, quantity: 1 })],
      [CartActions.addItem({ product, quantity: 3 })],
      [CartActions.removeItem({ cartItemId: 'i1' })],
      [CartActions.clearCart()],
      [CartActions.setDeliveryFee({ orderType: 'Pickup' })],
    ]);
  });

  it('updateQuantity dispatches updateQuantity for a positive quantity', () => {
    service.updateQuantity('i1', 4);

    expect(store.dispatch).toHaveBeenCalledOnceWith(CartActions.updateQuantity({ cartItemId: 'i1', quantity: 4 }));
  });

  for (const quantity of [0, -1]) {
    it(`updateQuantity removes the item instead when the quantity is ${quantity}`, () => {
      service.updateQuantity('i1', quantity);

      expect(store.dispatch).toHaveBeenCalledOnceWith(CartActions.removeItem({ cartItemId: 'i1' }));
    });
  }

  it('applyCoupon dispatches applyCoupon and maps success to a response with the cart', () => {
    let result: ApiResponse<Cart> | undefined;

    service.applyCoupon('WELCOME10').subscribe(r => (result = r));
    expect(store.dispatch).toHaveBeenCalledOnceWith(CartActions.applyCoupon({ couponCode: 'WELCOME10' }));
    actions$.next(CartActions.applyCouponSuccess({ cart }));

    expect(result).toEqual({ isSuccess: true, message: 'Coupon applied', result: cart });
  });

  it('applyCoupon maps failure to an unsuccessful response with the error', () => {
    let result: ApiResponse<Cart> | undefined;

    service.applyCoupon('BOGUS').subscribe(r => (result = r));
    actions$.next(CartActions.applyCouponFailure({ error: 'Invalid coupon' }));

    expect(result).toEqual({ isSuccess: false, message: 'Invalid coupon' });
  });

  it('removeCoupon dispatches removeCoupon and maps success and failure', () => {
    const results: ApiResponse<Cart>[] = [];

    service.removeCoupon().subscribe(r => results.push(r));
    expect(store.dispatch).toHaveBeenCalledOnceWith(CartActions.removeCoupon());
    actions$.next(CartActions.removeCouponSuccess({ cart }));
    service.removeCoupon().subscribe(r => results.push(r));
    actions$.next(CartActions.removeCouponFailure({ error: 'No coupon' }));

    expect(results).toEqual([
      { isSuccess: true, message: 'Coupon removed', result: cart },
      { isSuccess: false, message: 'No coupon' },
    ]);
  });

  it('exposes the cart, its item count and a snapshot from the store', () => {
    expect(service.itemCount).toBe(0);
    expect(service.currentCart.items).toEqual([]);

    store.setState({ cart: { ...initialCartState, cart } });

    let emitted: Cart | undefined;
    service.cart$.subscribe(c => (emitted = c));
    expect(emitted).toEqual(cart);
    expect(service.itemCount).toBe(5);
    expect(service.currentCart).toEqual(cart);
  });
});
