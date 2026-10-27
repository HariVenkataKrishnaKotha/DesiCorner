import { Cart } from '../../core/models/cart.models';
import { AuthActions } from '../auth/auth.actions';
import { CartActions } from './cart.actions';
import { CartState, cartReducer, initialCartState } from './cart.reducer';
import { selectCartId, selectCartItemCount, selectCartTotal, selectCouponCode } from './cart.selectors';

describe('cartReducer', () => {
  function cartWith(subtotal: number, extra: Partial<Cart> = {}): Cart {
    return {
      id: 'c1',
      items: [{ id: 'i1', productId: 'p1', productName: 'Thali', price: subtotal, quantity: 1, isVegetarian: true, isSpicy: false, spiceLevel: 0 }],
      subtotal,
      tax: +(subtotal * 0.06).toFixed(2),
      deliveryFee: 0,
      discount: 0,
      total: subtotal,
      ...extra,
    };
  }
  const loaded: CartState = { cart: cartWith(20), loading: false, error: null };

  it('returns the initial (empty) state for an unknown action', () => {
    const state = cartReducer(undefined, { type: 'unknown' });

    expect(state).toEqual(initialCartState);
    expect(state.cart.items).toEqual([]);
    expect(state.cart.total).toBe(0);
  });

  it('loadCart starts loading and clears the error', () => {
    expect(cartReducer({ ...initialCartState, error: 'old' }, CartActions.loadCart()))
      .toEqual({ ...initialCartState, loading: true, error: null });
  });

  it('loadCartSuccess and loadCartFailure both store the given cart and stop loading', () => {
    const fromServer = cartWith(30);
    const fromStorage = cartWith(12);

    expect(cartReducer({ ...initialCartState, loading: true }, CartActions.loadCartSuccess({ cart: fromServer })).cart).toBe(fromServer);
    const failed = cartReducer({ ...initialCartState, loading: true }, CartActions.loadCartFailure({ cart: fromStorage }));
    expect(failed.cart).toBe(fromStorage);
    expect(failed.loading).toBeFalse();
  });

  const replacing = [
    (cart: Cart) => CartActions.addItemSuccess({ cart }),
    (cart: Cart) => CartActions.addItemLocalFallback({ cart }),
    (cart: Cart) => CartActions.removeItemSuccess({ cart }),
    (cart: Cart) => CartActions.removeItemLocalFallback({ cart }),
    (cart: Cart) => CartActions.updateQuantitySuccess({ cart }),
    (cart: Cart) => CartActions.applyCouponSuccess({ cart }),
    (cart: Cart) => CartActions.removeCouponSuccess({ cart }),
  ];
  for (const make of replacing) {
    const action = make(cartWith(1));
    it(`${action.type} replaces the cart`, () => {
      const next = cartWith(42, { couponCode: 'WELCOME10' });
      expect(cartReducer(loaded, make(next)).cart).toBe(next);
    });
  }

  for (const action of [CartActions.clearCartSuccess(), AuthActions.logout()]) {
    it(`${action.type} empties the cart`, () => {
      expect(cartReducer(loaded, action)).toEqual(initialCartState);
    });
  }

  it('failure actions keep the current cart', () => {
    for (const action of [
      CartActions.updateQuantityFailure({ error: 'x' }),
      CartActions.applyCouponFailure({ error: 'x' }),
      CartActions.removeCouponFailure({ error: 'x' }),
    ]) {
      expect(cartReducer(loaded, action).cart).toBe(loaded.cart);
    }
  });

  describe('setDeliveryFee', () => {
    it('charges $5 delivery below the $50 free-delivery threshold and recomputes the total', () => {
      const state = cartReducer({ ...initialCartState, cart: cartWith(20, { tax: 1.2, discount: 2 }) }, CartActions.setDeliveryFee({ orderType: 'Delivery' }));

      expect(state.cart.deliveryFee).toBe(5);
      expect(state.cart.total).toBeCloseTo(20 + 1.2 + 5 - 2, 10);
    });

    it('makes delivery free at exactly $50', () => {
      const state = cartReducer({ ...initialCartState, cart: cartWith(50, { tax: 3 }) }, CartActions.setDeliveryFee({ orderType: 'Delivery' }));

      expect(state.cart.deliveryFee).toBe(0);
      expect(state.cart.total).toBe(53);
    });

    it('never charges delivery for pickup', () => {
      const state = cartReducer({ ...initialCartState, cart: cartWith(10, { tax: 0.6, deliveryFee: 5 }) }, CartActions.setDeliveryFee({ orderType: 'Pickup' }));

      expect(state.cart.deliveryFee).toBe(0);
      expect(state.cart.total).toBeCloseTo(10.6, 10);
    });
  });

  describe('selectors', () => {
    it('count item quantities and expose total, id and coupon', () => {
      const cart: Cart = {
        ...cartWith(20, { couponCode: 'SAVE5', total: 18 }),
        items: [
          { productId: 'p1', productName: 'a', price: 1, quantity: 2, isVegetarian: true, isSpicy: false, spiceLevel: 0 },
          { productId: 'p2', productName: 'b', price: 1, quantity: 3, isVegetarian: true, isSpicy: false, spiceLevel: 0 },
        ],
      };
      const root = { cart: { ...initialCartState, cart } };

      expect(selectCartItemCount(root)).toBe(5);
      expect(selectCartTotal(root)).toBe(18);
      expect(selectCartId(root)).toBe('c1');
      expect(selectCouponCode(root)).toBe('SAVE5');
    });
  });
});
