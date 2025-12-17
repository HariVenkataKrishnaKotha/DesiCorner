import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ProductService } from '@core/services/product.service';
import { CartService } from '@core/services/cart.service';
import { Product, Category } from '@core/models/product.models';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './home.html',
  styleUrls: ['./home.scss']
})
export class HomeComponent implements OnInit {
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private toastr = inject(ToastrService);
  private route = inject(ActivatedRoute);

  categories: Category[] = [];
  featuredProducts: Product[] = [];
  allProducts: Product[] = [];
  loading = true;
  selectedCategoryId: string | null = null;
  selectedCategoryName: string = '';

  ngOnInit(): void {
    // Subscribe to query parameter changes
    this.route.queryParamMap.subscribe(params => {
      this.selectedCategoryId = params.get('category');
      this.loadData();
    });
  }

  loadData(): void {
    this.loading = true;

    // Load categories
    this.productService.loadCategories().subscribe({
      next: (response) => {
        if (response.isSuccess && response.result) {
          this.categories = response.result;
          
          // Set selected category name if filtering
          if (this.selectedCategoryId) {
            const selectedCategory = this.categories.find(c => c.id === this.selectedCategoryId);
            this.selectedCategoryName = selectedCategory?.name || '';
          } else {
            this.selectedCategoryName = '';
          }
        }
      },
      error: (error) => {
        console.error('Failed to load categories', error);
      }
    });

    // Load products - either by category or all
    if (this.selectedCategoryId) {
      this.productService.getProductsByCategory(this.selectedCategoryId).subscribe({
        next: (response) => {
          if (response.isSuccess && response.result) {
            this.featuredProducts = response.result;
          } else {
            this.featuredProducts = [];
          }
          this.loading = false;
        },
        error: (error) => {
          console.error('Failed to load products by category', error);
          this.featuredProducts = [];
          this.loading = false;
        }
      });
    } else {
      this.productService.loadProducts().subscribe({
        next: (response) => {
          if (response.isSuccess && response.result) {
            this.allProducts = response.result;
            // Get first 6 products as featured when no category selected
            this.featuredProducts = response.result.slice(0, 6);
          }
          this.loading = false;
        },
        error: (error) => {
          console.error('Failed to load products', error);
          this.loading = false;
        }
      });
    }
  }

  clearCategoryFilter(): void {
    this.selectedCategoryId = null;
    this.selectedCategoryName = '';
  }

  addToCart(product: Product, event: Event): void {
    event.stopPropagation(); // Prevent navigation to product detail
    this.cartService.addItem(product, 1);
    this.toastr.success(`${product.name} added to cart!`, 'Success');
  }

  getCategoryEmoji(categoryName: string): string {
    const emojiMap: { [key: string]: string } = {
      'Appetizers': '🥟',
      'Main Course': '🍛',
      'Biryani': '🍚',
      'Desserts': '🍮',
      'Beverages': '🥤'
    };

    return emojiMap[categoryName] || '🍽️';
  }
}