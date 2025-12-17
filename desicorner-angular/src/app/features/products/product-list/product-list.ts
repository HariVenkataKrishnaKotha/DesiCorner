import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { ProductService } from '@core/services/product.service';
import { CartService } from '@core/services/cart.service';
import { Product, Category } from '@core/models/product.models';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatFormFieldModule
  ],
  templateUrl: './product-list.html',
  styleUrls: ['./product-list.scss']
})
export class ProductListComponent implements OnInit {
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private toastr = inject(ToastrService);
  private route = inject(ActivatedRoute);

  products: Product[] = [];
  filteredProducts: Product[] = [];
  categories: Category[] = [];
  loading = true;

  selectedCategoryId: string | null = null;
  sortBy: string = 'name';

  ngOnInit(): void {
    this.loadCategories();
    
    this.route.queryParamMap.subscribe(params => {
      this.selectedCategoryId = params.get('category');
      this.loadProducts();
    });
  }

  loadCategories(): void {
    this.productService.loadCategories().subscribe({
      next: (response) => {
        if (response.isSuccess && response.result) {
          this.categories = response.result;
        }
      }
    });
  }

  loadProducts(): void {
    this.loading = true;

    if (this.selectedCategoryId) {
      this.productService.getProductsByCategory(this.selectedCategoryId).subscribe({
        next: (response) => {
          if (response.isSuccess && response.result) {
            this.products = response.result;
            this.applySort();
          }
          this.loading = false;
        },
        error: () => {
          this.loading = false;
        }
      });
    } else {
      this.productService.loadProducts().subscribe({
        next: (response) => {
          if (response.isSuccess && response.result) {
            this.products = response.result;
            this.applySort();
          }
          this.loading = false;
        },
        error: () => {
          this.loading = false;
        }
      });
    }
  }

  onCategoryChange(): void {
    this.loadProducts();
  }

  onSortChange(): void {
    this.applySort();
  }

  applySort(): void {
    this.filteredProducts = [...this.products].sort((a, b) => {
      switch (this.sortBy) {
        case 'name':
          return a.name.localeCompare(b.name);
        case 'price-low':
          return a.price - b.price;
        case 'price-high':
          return b.price - a.price;
        case 'rating':
          return b.averageRating - a.averageRating;
        default:
          return 0;
      }
    });
  }

  addToCart(product: Product, event: Event): void {
    event.stopPropagation();
    this.cartService.addItem(product, 1);
    this.toastr.success(`${product.name} added to cart!`, 'Success');
  }

  getCategoryName(categoryId: string): string {
    return this.categories.find(c => c.id === categoryId)?.name || '';
  }
}