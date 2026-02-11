import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { Product, Category, CreateProductRequest } from '../../core/models/product.models';

export const ProductActions = createActionGroup({
  source: 'Product',
  events: {
    // Load Products
    'Load Products': emptyProps(),
    'Load Products Success': props<{ products: Product[] }>(),
    'Load Products Failure': props<{ error: string }>(),

    // Load Categories
    'Load Categories': emptyProps(),
    'Load Categories Success': props<{ categories: Category[] }>(),
    'Load Categories Failure': props<{ error: string }>(),

    // Load Products by Category
    'Load Products By Category': props<{ categoryId: string }>(),
    'Load Products By Category Success': props<{ products: Product[] }>(),
    'Load Products By Category Failure': props<{ error: string }>(),

    // Load Product by ID
    'Load Product By Id': props<{ productId: string }>(),
    'Load Product By Id Success': props<{ product: Product }>(),
    'Load Product By Id Failure': props<{ error: string }>(),

    // Create Product (admin)
    'Create Product': props<{ product: CreateProductRequest }>(),
    'Create Product Success': props<{ product: Product }>(),
    'Create Product Failure': props<{ error: string }>(),

    // Update Product (admin)
    'Update Product': props<{ id: string; product: Partial<Product> }>(),
    'Update Product Success': props<{ product: Product }>(),
    'Update Product Failure': props<{ error: string }>(),

    // Delete Product (admin)
    'Delete Product': props<{ productId: string }>(),
    'Delete Product Success': props<{ productId: string }>(),
    'Delete Product Failure': props<{ error: string }>(),
  },
});
