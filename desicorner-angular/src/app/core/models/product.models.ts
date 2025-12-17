export interface Product {
  id: string;
  name: string;
  description: string;
  price: number;
  imageUrl?: string;
  categoryId: string;
  categoryName: string;
  isAvailable: boolean;
  isVegetarian: boolean;
  isVegan: boolean;
  isSpicy: boolean;
  spiceLevel: number;
  allergens?: string;
  preparationTime: number;
  createdAt: Date;
  updatedAt?: Date;
  // Rating aggregation
  averageRating: number;
  reviewCount: number;
}

export interface Category {
  id: string;
  name: string;
  description: string;
  imageUrl?: string;
  displayOrder: number;
}

export interface CreateProductRequest {
  name: string;
  description: string;
  price: number;
  categoryId: string;
  imageUrl?: string;
  isAvailable: boolean;
  isVegetarian: boolean;
  isVegan: boolean;
  isSpicy: boolean;
  spiceLevel: number;
  allergens?: string;
  preparationTime: number;
}