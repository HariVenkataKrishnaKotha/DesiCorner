import { createReducer, on } from '@ngrx/store';
import { Cart } from '../../core/models/cart.models';
import { CartActions } from './cart.actions';
import { AuthActions } from '../auth/auth.actions';

export interface CartState {
  cart: Cart;
  loading: boolean;
  error: string | null;
}

const emptyCart: Cart = {
  items: [],
  subtotal: 0,
  tax: 0,
  deliveryFee: 0,
  discount: 0,
  total: 0,
};

export const initialCartState: CartState = {
  cart: emptyCart,
  loading: false,
  error: null,
};

export const cartReducer = createReducer(
  initialCartState,

  // Load Cart
  on(CartActions.loadCart, (state) => ({
    ...state,
    loading: true,
    error: null,
  })),
  on(CartActions.loadCartSuccess, (state, { cart }) => ({
    ...state,
    cart,
    loading: false,
  })),
  on(CartActions.loadCartFailure, (state, { cart }) => ({
    ...state,
    cart,
    loading: false,
  })),

  // Add Item
  on(CartActions.addItemSuccess, (state, { cart }) => ({
    ...state,
    cart,
  })),
  on(CartActions.addItemLocalFallback, (state, { cart }) => ({
    ...state,
    cart,
  })),

  // Remove Item
  on(CartActions.removeItemSuccess, (state, { cart }) => ({
    ...state,
    cart,
  })),
  on(CartActions.removeItemLocalFallback, (state, { cart }) => ({
    ...state,
    cart,
  })),

  // Update Quantity
  on(CartActions.updateQuantitySuccess, (state, { cart }) => ({
    ...state,
    cart,
  })),

  // Clear Cart
  on(CartActions.clearCartSuccess, () => ({
    ...initialCartState,
  })),

  // Reset cart on auth logout (user switched or signed out)
  on(AuthActions.logout, () => ({
    ...initialCartState,
  })),

  // Coupon
  on(CartActions.applyCouponSuccess, (state, { cart }) => ({
    ...state,
    cart,
  })),
  on(CartActions.removeCouponSuccess, (state, { cart }) => ({
    ...state,
    cart,
  })),

  // Set Delivery Fee (local calculation, no API)
  on(CartActions.setDeliveryFee, (state, { orderType }) => {
    const cart = state.cart;
    const FREE_DELIVERY_THRESHOLD = 50;
    const DELIVERY_FEE = 5;
    const deliveryFee = orderType === 'Pickup' ? 0 : (cart.subtotal >= FREE_DELIVERY_THRESHOLD ? 0 : DELIVERY_FEE);
    return {
      ...state,
      cart: {
        ...cart,
        deliveryFee,
        total: cart.subtotal + cart.tax + deliveryFee - cart.discount,
      },
    };
  }),
);
