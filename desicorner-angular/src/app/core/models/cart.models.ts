export interface CartItem {
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
  items: CartItem[];
  subtotal: number;
  tax: number;
  deliveryFee: number;
  discount: number;
  total: number;
}