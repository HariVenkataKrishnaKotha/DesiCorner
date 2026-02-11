import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { Cart } from '../../core/models/cart.models';
import { Product } from '../../core/models/product.models';

export const CartActions = createActionGroup({
  source: 'Cart',
  events: {
    // Load Cart
    'Load Cart': emptyProps(),
    'Load Cart Success': props<{ cart: Cart }>(),
    'Load Cart Failure': props<{ cart: Cart }>(), // fallback cart from localStorage

    // Add Item
    'Add Item': props<{ product: Product; quantity: number }>(),
    'Add Item Success': props<{ cart: Cart }>(),
    'Add Item Local Fallback': props<{ cart: Cart }>(),

    // Remove Item
    'Remove Item': props<{ cartItemId: string }>(),
    'Remove Item Success': props<{ cart: Cart }>(),
    'Remove Item Local Fallback': props<{ cart: Cart }>(),

    // Update Quantity
    'Update Quantity': props<{ cartItemId: string; quantity: number }>(),
    'Update Quantity Success': props<{ cart: Cart }>(),
    'Update Quantity Failure': props<{ error: string }>(),

    // Apply Coupon
    'Apply Coupon': props<{ couponCode: string }>(),
    'Apply Coupon Success': props<{ cart: Cart }>(),
    'Apply Coupon Failure': props<{ error: string }>(),

    // Remove Coupon
    'Remove Coupon': emptyProps(),
    'Remove Coupon Success': props<{ cart: Cart }>(),
    'Remove Coupon Failure': props<{ error: string }>(),

    // Clear Cart
    'Clear Cart': emptyProps(),
    'Clear Cart Success': emptyProps(),
    'Clear Cart Failure': props<{ error: string }>(),

    // Local-only: set delivery fee based on order type
    'Set Delivery Fee': props<{ orderType: 'Delivery' | 'Pickup' }>(),
  },
});
