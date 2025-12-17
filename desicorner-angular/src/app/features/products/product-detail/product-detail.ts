import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { ToastrService } from 'ngx-toastr';

import { ProductService } from '@core/services/product.service';
import { CartService } from '@core/services/cart.service';
import { ReviewService } from '@core/services/review.service';
import { AuthService } from '@core/services/auth.service';
import { Product } from '@core/models/product.models';
import { 
  Review, 
  ReviewSummary, 
  PaginatedReviews, 
  CreateReviewRequest, 
  UpdateReviewRequest 
} from '@core/models/review.models';

import { StarRatingComponent } from '@shared/components/star-rating/star-rating';
import { ReviewSummaryComponent } from '@shared/components/review-summary/review-summary';
import { ReviewListComponent } from '@shared/components/review-list/review-list';
import { ReviewFormComponent } from '@shared/components/review-form/review-form';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTabsModule,
    MatDividerModule,
    MatDialogModule,
    StarRatingComponent,
    ReviewSummaryComponent,
    ReviewListComponent,
    ReviewFormComponent
  ],
  templateUrl: './product-detail.html',
  styleUrls: ['./product-detail.scss']
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private reviewService = inject(ReviewService);
  private authService = inject(AuthService);
  private toastr = inject(ToastrService);
  private dialog = inject(MatDialog);

  product: Product | null = null;
  loading = true;
  quantity = 1;

  // Review state
  reviewSummary: ReviewSummary | null = null;
  paginatedReviews: PaginatedReviews | null = null;
  userReview: Review | null = null;
  reviewsLoading = false;
  currentPage = 1;
  sortBy = 'newest';
  showReviewForm = false;
  editingReview: Review | null = null;
  submittingReview = false;

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated;
  }

  get canWriteReview(): boolean {
    return this.isAuthenticated && !this.userReview && !this.showReviewForm;
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const productId = params.get('id');
      if (productId) {
        this.loadProduct(productId);
        this.loadReviews(productId);
        this.loadReviewSummary(productId);
        if (this.isAuthenticated) {
          this.loadUserReview(productId);
        }
      }
    });
  }

  loadProduct(productId: string): void {
    this.loading = true;
    this.productService.getProductById(productId).subscribe({
      next: (response) => {
        if (response.isSuccess && response.result) {
          this.product = response.result;
        } else {
          this.toastr.error('Product not found');
          this.router.navigate(['/']);
        }
        this.loading = false;
      },
      error: (error) => {
        console.error('Failed to load product', error);
        this.toastr.error('Failed to load product');
        this.loading = false;
        this.router.navigate(['/']);
      }
    });
  }

  loadReviews(productId: string): void {
    this.reviewsLoading = true;
    this.reviewService.getProductReviews(productId, this.currentPage, 10, this.sortBy).subscribe({
      next: (reviews) => {
        this.paginatedReviews = reviews;
        this.reviewsLoading = false;
      },
      error: (error) => {
        console.error('Failed to load reviews', error);
        this.reviewsLoading = false;
      }
    });
  }

  loadReviewSummary(productId: string): void {
    this.reviewService.getReviewSummary(productId).subscribe({
      next: (summary) => {
        this.reviewSummary = summary;
      },
      error: (error) => {
        console.error('Failed to load review summary', error);
      }
    });
  }

  loadUserReview(productId: string): void {
    this.reviewService.getUserReview(productId).subscribe({
      next: (review) => {
        this.userReview = review;
      },
      error: () => {
        // User hasn't reviewed - this is expected
        this.userReview = null;
      }
    });
  }

  // Cart actions
  decrementQuantity(): void {
    if (this.quantity > 1) {
      this.quantity--;
    }
  }

  incrementQuantity(): void {
    this.quantity++;
  }

  addToCart(): void {
    if (this.product) {
      this.cartService.addItem(this.product, this.quantity);
      this.toastr.success(`${this.quantity} x ${this.product.name} added to cart!`, 'Success');
    }
  }

  // Review actions
  onWriteReview(): void {
    if (!this.isAuthenticated) {
      this.toastr.info('Please login to write a review');
      this.router.navigate(['/auth/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }
    this.showReviewForm = true;
    this.editingReview = null;
  }

  onEditReview(review: Review): void {
    this.editingReview = review;
    this.showReviewForm = true;
  }

  onCancelReview(): void {
    this.showReviewForm = false;
    this.editingReview = null;
  }

  onSubmitReview(request: CreateReviewRequest | UpdateReviewRequest): void {
    this.submittingReview = true;

    if ('id' in request) {
      // Update existing review
      this.reviewService.updateReview(request as UpdateReviewRequest).subscribe({
        next: (review) => {
          this.toastr.success('Review updated successfully!');
          this.userReview = review;
          this.showReviewForm = false;
          this.editingReview = null;
          this.submittingReview = false;
          this.refreshReviews();
        },
        error: (error) => {
          console.error('Failed to update review', error);
          this.toastr.error('Failed to update review');
          this.submittingReview = false;
        }
      });
    } else {
      // Create new review
      this.reviewService.createReview(request as CreateReviewRequest).subscribe({
        next: (review) => {
          this.toastr.success('Review submitted successfully!');
          this.userReview = review;
          this.showReviewForm = false;
          this.submittingReview = false;
          this.refreshReviews();
        },
        error: (error) => {
          console.error('Failed to submit review', error);
          this.toastr.error(error.error?.message || 'Failed to submit review');
          this.submittingReview = false;
        }
      });
    }
  }

  onDeleteReview(review: Review): void {
    if (confirm('Are you sure you want to delete this review?')) {
      this.reviewService.deleteReview(review.id).subscribe({
        next: () => {
          this.toastr.success('Review deleted successfully!');
          this.userReview = null;
          this.refreshReviews();
        },
        error: (error) => {
          console.error('Failed to delete review', error);
          this.toastr.error('Failed to delete review');
        }
      });
    }
  }

  onVoteReview(event: { reviewId: string; isHelpful: boolean }): void {
    this.reviewService.voteOnReview({ reviewId: event.reviewId, isHelpful: event.isHelpful }).subscribe({
      next: () => {
        this.toastr.success('Vote recorded!');
        this.loadReviews(this.product!.id);
      },
      error: (error) => {
        console.error('Failed to vote', error);
        this.toastr.error(error.error?.message || 'Failed to record vote');
      }
    });
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadReviews(this.product!.id);
  }

  onSortChange(sortBy: string): void {
    this.sortBy = sortBy;
    this.currentPage = 1;
    this.loadReviews(this.product!.id);
  }

  private refreshReviews(): void {
    if (this.product) {
      this.loadReviews(this.product.id);
      this.loadReviewSummary(this.product.id);
      // Reload product to get updated rating
      this.loadProduct(this.product.id);
    }
  }
}