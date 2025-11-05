import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { Cart, CartItem } from '../models/cart.models';
import { Product } from '../models/product.models';

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private readonly CART_STORAGE_KEY = 'desicorner_cart';
  private readonly TAX_RATE = 0.06; // 8% tax
  private readonly DELIVERY_FEE = 5.99;

  private cartSubject = new BehaviorSubject<Cart>(this.loadCartFromStorage());
  public cart$ = this.cartSubject.asObservable();

  constructor() {
    // Save cart to localStorage whenever it changes
    this.cart$.subscribe(cart => {
      localStorage.setItem(this.CART_STORAGE_KEY, JSON.stringify(cart));
    });
  }

  private loadCartFromStorage(): Cart {
    const stored = localStorage.getItem(this.CART_STORAGE_KEY);
    if (stored) {
      return JSON.parse(stored);
    }
    return this.createEmptyCart();
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

  addItem(product: Product, quantity: number = 1): void {
    const currentCart = this.cartSubject.value;
    const existingItem = currentCart.items.find(item => item.productId === product.id);

    if (existingItem) {
      existingItem.quantity += quantity;
    } else {
      const newItem: CartItem = {
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

  removeItem(productId: string): void {
    const currentCart = this.cartSubject.value;
    currentCart.items = currentCart.items.filter(item => item.productId !== productId);
    this.updateCartTotals(currentCart);
  }

  updateQuantity(productId: string, quantity: number): void {
    const currentCart = this.cartSubject.value;
    const item = currentCart.items.find(item => item.productId === productId);
    
    if (item) {
      if (quantity <= 0) {
        this.removeItem(productId);
      } else {
        item.quantity = quantity;
        this.updateCartTotals(currentCart);
      }
    }
  }

  clearCart(): void {
    this.cartSubject.next(this.createEmptyCart());
  }

  private updateCartTotals(cart: Cart): void {
    // Calculate subtotal
    cart.subtotal = cart.items.reduce((sum, item) => sum + (item.price * item.quantity), 0);
    
    // Calculate tax
    cart.tax = cart.subtotal * this.TAX_RATE;
    
    // Delivery fee (free over $50)
    cart.deliveryFee = cart.subtotal >= 50 ? 0 : this.DELIVERY_FEE;
    
    // Calculate total
    cart.total = cart.subtotal + cart.tax + cart.deliveryFee - cart.discount;
    
    this.cartSubject.next(cart);
  }

  get itemCount(): number {
    return this.cartSubject.value.items.reduce((sum, item) => sum + item.quantity, 0);
  }

  get currentCart(): Cart {
    return this.cartSubject.value;
  }
}