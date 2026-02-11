import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { Store } from '@ngrx/store';
import { Actions, ofType } from '@ngrx/effects';
import { AppState } from '../../store';
import { CartActions } from '../../store/cart/cart.actions';
import { selectCart, selectCartItemCount } from '../../store/cart/cart.selectors';
import { Cart } from '../models/cart.models';
import { Product } from '../models/product.models';
import { ApiResponse } from '../models/response.models';

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private store = inject(Store<AppState>);
  private actions$ = inject(Actions);

  /** Observable for components — same shape as the old BehaviorSubject */
  public cart$ = this.store.select(selectCart);

  /** Load cart from backend API */
  loadCart(): void {
    this.store.dispatch(CartActions.loadCart());
  }

  /** Add item to cart */
  addItem(product: Product, quantity: number = 1): void {
    this.store.dispatch(CartActions.addItem({ product, quantity }));
  }

  /** Remove item from cart */
  removeItem(cartItemId: string): void {
    this.store.dispatch(CartActions.removeItem({ cartItemId }));
  }

  /** Update item quantity */
  updateQuantity(cartItemId: string, quantity: number): void {
    if (quantity <= 0) {
      this.removeItem(cartItemId);
      return;
    }
    this.store.dispatch(CartActions.updateQuantity({ cartItemId, quantity }));
  }

  /** Apply coupon — returns Observable for component subscription */
  applyCoupon(couponCode: string): Observable<ApiResponse<Cart>> {
    this.store.dispatch(CartActions.applyCoupon({ couponCode }));
    return this.actions$.pipe(
      ofType(CartActions.applyCouponSuccess, CartActions.applyCouponFailure),
      take(1),
      map(action => {
        if (action.type === CartActions.applyCouponSuccess.type) {
          return { isSuccess: true, message: 'Coupon applied', result: (action as any).cart } as ApiResponse<Cart>;
        }
        return { isSuccess: false, message: (action as any).error } as ApiResponse<Cart>;
      })
    );
  }

  /** Remove coupon — returns Observable for component subscription */
  removeCoupon(): Observable<ApiResponse<Cart>> {
    this.store.dispatch(CartActions.removeCoupon());
    return this.actions$.pipe(
      ofType(CartActions.removeCouponSuccess, CartActions.removeCouponFailure),
      take(1),
      map(action => {
        if (action.type === CartActions.removeCouponSuccess.type) {
          return { isSuccess: true, message: 'Coupon removed', result: (action as any).cart } as ApiResponse<Cart>;
        }
        return { isSuccess: false, message: (action as any).error } as ApiResponse<Cart>;
      })
    );
  }

  /** Clear entire cart */
  clearCart(): void {
    this.store.dispatch(CartActions.clearCart());
  }

  /** Set delivery fee based on order type */
  setDeliveryFee(orderType: 'Delivery' | 'Pickup'): void {
    this.store.dispatch(CartActions.setDeliveryFee({ orderType }));
  }

  /** Get current item count (synchronous) */
  get itemCount(): number {
    let count = 0;
    this.store.select(selectCartItemCount).pipe(take(1)).subscribe(v => count = v);
    return count;
  }

  /** Get current cart snapshot (synchronous) */
  get currentCart(): Cart {
    let cart: Cart = { items: [], subtotal: 0, tax: 0, deliveryFee: 0, discount: 0, total: 0 };
    this.store.select(selectCart).pipe(take(1)).subscribe(v => cart = v);
    return cart;
  }
}
