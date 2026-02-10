import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, catchError, of } from 'rxjs';
import { Cart, CartItem } from '../models/cart.models';
import { Product } from '../models/product.models';
import { ApiResponse } from '../models/response.models';
import { environment } from '@env/environment';

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private http = inject(HttpClient);
  
  private readonly CART_STORAGE_KEY = 'desicorner_cart';
  private readonly TAX_RATE = 0.06; // 6% tax
  private readonly DELIVERY_FEE = 5.00;
  private readonly FREE_DELIVERY_THRESHOLD = 50;

  private cartSubject = new BehaviorSubject<Cart>(this.createEmptyCart());
  public cart$ = this.cartSubject.asObservable();

  constructor() {
    // Load cart from backend on init
    this.loadCart();
  }

  /**
   * Load cart from backend API
   */
  loadCart(): void {
    this.http.get<ApiResponse<Cart>>(`${environment.gatewayUrl}/api/cart`)
      .pipe(
        catchError(error => {
          console.error('Failed to load cart from server:', error);
          // Fall back to localStorage if server fails
          return of({ isSuccess: true, result: this.loadCartFromStorage() });
        })
      )
      .subscribe(response => {
        if (response.isSuccess && response.result) {
          const cart = this.mapBackendCart(response.result);
          this.cartSubject.next(cart);
          this.saveCartToStorage(cart);
        }
      });
  }

  /**
   * Add item to cart via backend API
   */
  addItem(product: Product, quantity: number = 1): void {
    const request = {
      productId: product.id,
      quantity: quantity
    };

    this.http.post<ApiResponse<Cart>>(`${environment.gatewayUrl}/api/cart/add`, request)
      .pipe(
        catchError(error => {
          console.error('Failed to add item to cart:', error);
          // Fall back to local cart management
          this.addItemLocally(product, quantity);
          return of(null);
        })
      )
      .subscribe(response => {
        if (response?.isSuccess && response.result) {
          const cart = this.mapBackendCart(response.result);
          this.cartSubject.next(cart);
          this.saveCartToStorage(cart);
        }
      });
  }

  /**
   * Remove item from cart via backend API
   */
  removeItem(cartItemId: string): void {
    this.http.delete<ApiResponse<Cart>>(`${environment.gatewayUrl}/api/cart/item/${cartItemId}`)
      .pipe(
        catchError(error => {
          console.error('Failed to remove item from cart:', error);
          this.removeItemLocally(cartItemId);
          return of(null);
        })
      )
      .subscribe(response => {
        if (response?.isSuccess && response.result) {
          const cart = this.mapBackendCart(response.result);
          this.cartSubject.next(cart);
          this.saveCartToStorage(cart);
        }
      });
  }

  /**
   * Update item quantity via backend API
   */
  updateQuantity(cartItemId: string, quantity: number): void {
    if (quantity <= 0) {
      this.removeItem(cartItemId);
      return;
    }

    const request = {
      cartItemId: cartItemId,
      quantity: quantity
    };

    this.http.put<ApiResponse<Cart>>(`${environment.gatewayUrl}/api/cart/update`, request)
      .pipe(
        catchError(error => {
          console.error('Failed to update cart item:', error);
          return of(null);
        })
      )
      .subscribe(response => {
        if (response?.isSuccess && response.result) {
          const cart = this.mapBackendCart(response.result);
          this.cartSubject.next(cart);
          this.saveCartToStorage(cart);
        }
      });
  }

  /**
   * Apply coupon code via backend API
   */
  applyCoupon(couponCode: string): Observable<ApiResponse<Cart>> {
  const cartId = this.cartSubject.value.id || '';
  return this.http.post<ApiResponse<Cart>>(
    `${environment.gatewayUrl}/api/cart/apply-coupon`,
    { cartId, couponCode }
  ).pipe(
      tap(response => {
        if (response.isSuccess && response.result) {
          const cart = this.mapBackendCart(response.result);
          this.cartSubject.next(cart);
          this.saveCartToStorage(cart);
        }
      })
    );
  }

  /**
   * Remove coupon via backend API
   */
  removeCoupon(): Observable<ApiResponse<Cart>> {
    const cartId = this.cartSubject.value.id || '';
    return this.http.post<ApiResponse<Cart>>(
      `${environment.gatewayUrl}/api/cart/remove-coupon/${cartId}`,
      {}
    ).pipe(
      tap(response => {
        if (response.isSuccess && response.result) {
          const cart = this.mapBackendCart(response.result);
          this.cartSubject.next(cart);
          this.saveCartToStorage(cart);
        }
      })
    );
  }

  /**
   * Clear entire cart via backend API
   */
  clearCart(): void {
    this.http.delete<ApiResponse<any>>(`${environment.gatewayUrl}/api/cart/clear`)
      .pipe(
        catchError(error => {
          console.error('Failed to clear cart:', error);
          return of(null);
        })
      )
      .subscribe(() => {
        const emptyCart = this.createEmptyCart();
        this.cartSubject.next(emptyCart);
        this.saveCartToStorage(emptyCart);
      });
  }

  /**
   * Map backend cart response to frontend Cart model
   */
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
        spiceLevel: item.spiceLevel || 0
      })),
      subtotal: backendCart.subTotal || 0,
      tax: backendCart.taxAmount || 0,
      deliveryFee: backendCart.deliveryFee || 0,
      discount: backendCart.discountAmount || 0,
      total: backendCart.total || 0,
      couponCode: backendCart.couponCode
    };
  }

  // Fallback local methods (when API fails)
  private addItemLocally(product: Product, quantity: number): void {
    const currentCart = this.cartSubject.value;
    const existingItem = currentCart.items.find(item => item.productId === product.id);

    if (existingItem) {
      existingItem.quantity += quantity;
    } else {
      const newItem: CartItem = {
        id: crypto.randomUUID(),
        productId: product.id,
        productName: product.name,
        productImage: product.imageUrl,
        price: product.price,
        quantity: quantity,
        isVegetarian: product.isVegetarian,
        isSpicy: product.isSpicy,
        spiceLevel: product.spiceLevel
      };
      currentCart.items.push(newItem);
    }

    this.updateCartTotals(currentCart);
  }

  private removeItemLocally(cartItemId: string): void {
    const currentCart = this.cartSubject.value;
    currentCart.items = currentCart.items.filter(item => item.id !== cartItemId && item.productId !== cartItemId);
    this.updateCartTotals(currentCart);
  }

  private updateCartTotals(cart: Cart): void {
    cart.subtotal = cart.items.reduce((sum, item) => sum + (item.price * item.quantity), 0);
    cart.tax = cart.subtotal * this.TAX_RATE;
    cart.deliveryFee = cart.subtotal >= this.FREE_DELIVERY_THRESHOLD ? 0 : this.DELIVERY_FEE;
    cart.total = cart.subtotal + cart.tax + cart.deliveryFee - cart.discount;
    
    this.cartSubject.next(cart);
    this.saveCartToStorage(cart);
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
    return {
      items: [],
      subtotal: 0,
      tax: 0,
      deliveryFee: 0,
      discount: 0,
      total: 0
    };
  }

  setDeliveryFee(orderType: 'Delivery' | 'Pickup'): void {
  const cart = this.cartSubject.value;
  // Simple delivery fee logic: free for orders over $50, otherwise $5
  const deliveryFee = orderType === 'Pickup' ? 0 : (cart.subtotal >= 50 ? 0 : 5);
  
  this.cartSubject.next({
    ...cart,
    deliveryFee,
    total: cart.subtotal + cart.tax + deliveryFee - cart.discount
  });
}

  get itemCount(): number {
    return this.cartSubject.value.items.reduce((sum, item) => sum + item.quantity, 0);
  }

  get currentCart(): Cart {
    return this.cartSubject.value;
  }
}