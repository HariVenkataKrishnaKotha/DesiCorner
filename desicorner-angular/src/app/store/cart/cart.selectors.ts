import { createFeatureSelector, createSelector } from '@ngrx/store';
import { CartState } from './cart.reducer';

export const selectCartState = createFeatureSelector<CartState>('cart');

export const selectCart = createSelector(
  selectCartState,
  (state) => state.cart
);

export const selectCartItems = createSelector(
  selectCart,
  (cart) => cart.items
);

export const selectCartItemCount = createSelector(
  selectCartItems,
  (items) => items.reduce((sum, item) => sum + item.quantity, 0)
);

export const selectCartSubtotal = createSelector(
  selectCart,
  (cart) => cart.subtotal
);

export const selectCartTotal = createSelector(
  selectCart,
  (cart) => cart.total
);

export const selectCartLoading = createSelector(
  selectCartState,
  (state) => state.loading
);

export const selectCartError = createSelector(
  selectCartState,
  (state) => state.error
);

export const selectCartId = createSelector(
  selectCart,
  (cart) => cart.id
);

export const selectCouponCode = createSelector(
  selectCart,
  (cart) => cart.couponCode
);
