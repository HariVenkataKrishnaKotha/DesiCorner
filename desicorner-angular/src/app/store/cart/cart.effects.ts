import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { HttpClient } from '@angular/common/http';
import { switchMap, map, catchError, tap } from 'rxjs/operators';
import { of } from 'rxjs';
import { CartActions } from './cart.actions';
import { AuthActions } from '../auth/auth.actions';
import { selectCart } from './cart.selectors';
import { AppState } from '../index';
import { environment } from '@env/environment';
import { ApiResponse } from '../../core/models/response.models';
import { Cart, CartItem } from '../../core/models/cart.models';
import { Product } from '../../core/models/product.models';

@Injectable()
export class CartEffects {
  private actions$ = inject(Actions);
  private http = inject(HttpClient);
  private store = inject(Store<AppState>);

  private readonly CART_STORAGE_KEY = 'desicorner_cart';
  private readonly TAX_RATE = 0.06;
  private readonly DELIVERY_FEE = 5.00;
  private readonly FREE_DELIVERY_THRESHOLD = 50;

  /** Load cart after auth check completes */
  loadCartOnAuthReady$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.checkAuthSuccess, AuthActions.checkAuthNoToken, AuthActions.loadUserProfileSuccess),
      map(() => CartActions.loadCart())
    )
  );

  /** Load cart from backend API */
  loadCart$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CartActions.loadCart),
      switchMap(() =>
        this.http.get<ApiResponse<any>>(`${environment.gatewayUrl}/api/cart`).pipe(
          map(response => {
            if (response?.isSuccess && response.result) {
              const cart = this.mapBackendCart(response.result);
              this.saveCartToStorage(cart);
              return CartActions.loadCartSuccess({ cart });
            }
            const fallback = this.loadCartFromStorage();
            return CartActions.loadCartFailure({ cart: fallback });
          }),
          catchError(() => {
            const fallback = this.loadCartFromStorage();
            return of(CartActions.loadCartFailure({ cart: fallback }));
          })
        )
      )
    )
  );

  /** Add item to cart via backend API with local fallback */
  addItem$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CartActions.addItem),
      switchMap(({ product, quantity }) =>
        this.http.post<ApiResponse<any>>(
          `${environment.gatewayUrl}/api/cart/add`,
          { productId: product.id, quantity }
        ).pipe(
          map(response => {
            if (response?.isSuccess && response.result) {
              const cart = this.mapBackendCart(response.result);
              this.saveCartToStorage(cart);
              return CartActions.addItemSuccess({ cart });
            }
            const cart = this.addItemLocally(product, quantity);
            return CartActions.addItemLocalFallback({ cart });
          }),
          catchError(() => {
            const cart = this.addItemLocally(product, quantity);
            return of(CartActions.addItemLocalFallback({ cart }));
          })
        )
      )
    )
  );

  /** Remove item from cart via backend API with local fallback */
  removeItem$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CartActions.removeItem),
      switchMap(({ cartItemId }) =>
        this.http.delete<ApiResponse<any>>(
          `${environment.gatewayUrl}/api/cart/item/${cartItemId}`
        ).pipe(
          map(response => {
            if (response?.isSuccess && response.result) {
              const cart = this.mapBackendCart(response.result);
              this.saveCartToStorage(cart);
              return CartActions.removeItemSuccess({ cart });
            }
            const cart = this.removeItemLocally(cartItemId);
            return CartActions.removeItemLocalFallback({ cart });
          }),
          catchError(() => {
            const cart = this.removeItemLocally(cartItemId);
            return of(CartActions.removeItemLocalFallback({ cart }));
          })
        )
      )
    )
  );

  /** Update item quantity via backend API */
  updateQuantity$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CartActions.updateQuantity),
      switchMap(({ cartItemId, quantity }) =>
        this.http.put<ApiResponse<any>>(
          `${environment.gatewayUrl}/api/cart/update`,
          { cartItemId, quantity }
        ).pipe(
          map(response => {
            if (response?.isSuccess && response.result) {
              const cart = this.mapBackendCart(response.result);
              this.saveCartToStorage(cart);
              return CartActions.updateQuantitySuccess({ cart });
            }
            return CartActions.updateQuantityFailure({ error: 'Failed to update quantity' });
          }),
          catchError(error => of(CartActions.updateQuantityFailure({ error: error.message || 'Failed to update' })))
        )
      )
    )
  );

  /** Apply coupon */
  applyCoupon$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CartActions.applyCoupon),
      switchMap(({ couponCode }) => {
        const cartId = this.getCurrentCartId();
        return this.http.post<ApiResponse<any>>(
          `${environment.gatewayUrl}/api/cart/apply-coupon`,
          { cartId, couponCode }
        ).pipe(
          map(response => {
            if (response?.isSuccess && response.result) {
              const cart = this.mapBackendCart(response.result);
              this.saveCartToStorage(cart);
              return CartActions.applyCouponSuccess({ cart });
            }
            return CartActions.applyCouponFailure({ error: response?.message || 'Failed to apply coupon' });
          }),
          catchError(error => {
            const message = error.error?.message || 'Failed to apply coupon';
            return of(CartActions.applyCouponFailure({ error: message }));
          })
        );
      })
    )
  );

  /** Remove coupon */
  removeCoupon$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CartActions.removeCoupon),
      switchMap(() => {
        const cartId = this.getCurrentCartId();
        return this.http.post<ApiResponse<any>>(
          `${environment.gatewayUrl}/api/cart/remove-coupon/${cartId}`,
          {}
        ).pipe(
          map(response => {
            if (response?.isSuccess && response.result) {
              const cart = this.mapBackendCart(response.result);
              this.saveCartToStorage(cart);
              return CartActions.removeCouponSuccess({ cart });
            }
            return CartActions.removeCouponFailure({ error: response?.message || 'Failed to remove coupon' });
          }),
          catchError(error => {
            const message = error.error?.message || 'Failed to remove coupon';
            return of(CartActions.removeCouponFailure({ error: message }));
          })
        );
      })
    )
  );

  /** Clear cart */
  clearCart$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CartActions.clearCart),
      switchMap(() =>
        this.http.delete<ApiResponse<any>>(`${environment.gatewayUrl}/api/cart/clear`).pipe(
          map(() => {
            this.saveCartToStorage(this.createEmptyCart());
            return CartActions.clearCartSuccess();
          }),
          catchError(() => {
            this.saveCartToStorage(this.createEmptyCart());
            return of(CartActions.clearCartSuccess());
          })
        )
      )
    )
  );

  /** Persist cart to localStorage on delivery fee changes */
  persistOnDeliveryFee$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CartActions.setDeliveryFee),
      tap(() => {
        // Small delay to let the reducer run first
        setTimeout(() => {
          let cart: Cart | undefined;
          this.store.select(selectCart).subscribe(c => cart = c).unsubscribe();
          if (cart) this.saveCartToStorage(cart);
        }, 0);
      })
    ),
    { dispatch: false }
  );

  // --- Utility methods (moved from CartService) ---

  private mapBackendCart(backendCart: any): Cart {
    return {
      id: backendCart.id,
      items: (backendCart.items || []).map((item: any) => ({
        id: item.id,
        productId: item.productId,
        productName: item.productName,
        productImage: item.productImage,
        price: item.price,
        quantity: item.quantity,
        isVegetarian: item.isVegetarian || false,
        isSpicy: item.isSpicy || false,
        spiceLevel: item.spiceLevel || 0,
      })),
      subtotal: backendCart.subTotal || 0,
      tax: backendCart.taxAmount || 0,
      deliveryFee: backendCart.deliveryFee || 0,
      discount: backendCart.discountAmount || 0,
      total: backendCart.total || 0,
      couponCode: backendCart.couponCode,
    };
  }

  private addItemLocally(product: Product, quantity: number): Cart {
    const currentCart = this.getCurrentCart();
    const items = [...currentCart.items];
    const existingIndex = items.findIndex(item => item.productId === product.id);

    if (existingIndex >= 0) {
      items[existingIndex] = { ...items[existingIndex], quantity: items[existingIndex].quantity + quantity };
    } else {
      const newItem: CartItem = {
        id: crypto.randomUUID(),
        productId: product.id,
        productName: product.name,
        productImage: product.imageUrl,
        price: product.price,
        quantity,
        isVegetarian: product.isVegetarian,
        isSpicy: product.isSpicy,
        spiceLevel: product.spiceLevel,
      };
      items.push(newItem);
    }

    const cart = this.recalculateTotals({ ...currentCart, items });
    this.saveCartToStorage(cart);
    return cart;
  }

  private removeItemLocally(cartItemId: string): Cart {
    const currentCart = this.getCurrentCart();
    const items = currentCart.items.filter(item => item.id !== cartItemId && item.productId !== cartItemId);
    const cart = this.recalculateTotals({ ...currentCart, items });
    this.saveCartToStorage(cart);
    return cart;
  }

  private recalculateTotals(cart: Cart): Cart {
    const subtotal = cart.items.reduce((sum, item) => sum + (item.price * item.quantity), 0);
    const tax = subtotal * this.TAX_RATE;
    const deliveryFee = subtotal >= this.FREE_DELIVERY_THRESHOLD ? 0 : this.DELIVERY_FEE;
    const total = subtotal + tax + deliveryFee - cart.discount;
    return { ...cart, subtotal, tax, deliveryFee, total };
  }

  private getCurrentCart(): Cart {
    let cart: Cart = this.createEmptyCart();
    this.store.select(selectCart).subscribe(c => cart = c).unsubscribe();
    return cart;
  }

  private getCurrentCartId(): string {
    return this.getCurrentCart().id || '';
  }

  private loadCartFromStorage(): Cart {
    const stored = localStorage.getItem(this.CART_STORAGE_KEY);
    if (stored) {
      return JSON.parse(stored);
    }
    return this.createEmptyCart();
  }

  private saveCartToStorage(cart: Cart): void {
    localStorage.setItem(this.CART_STORAGE_KEY, JSON.stringify(cart));
  }

  private createEmptyCart(): Cart {
    return { items: [], subtotal: 0, tax: 0, deliveryFee: 0, discount: 0, total: 0 };
  }
}
