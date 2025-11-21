export interface CartItem {
  id?: string;
  productId: string;
  productName: string;
  productImage?: string;
  price: number;
  quantity: number;
  isVegetarian: boolean;
  isSpicy: boolean;
  spiceLevel: number;
}

export interface Cart {
  id?: string
  items: CartItem[];
  subtotal: number;
  tax: number;
  deliveryFee: number;
  discount: number;
  total: number;
  couponCode?: string
}