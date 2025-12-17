import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  Review,
  CreateReviewRequest,
  UpdateReviewRequest,
  ReviewSummary,
  PaginatedReviews,
  ReviewVote
} from '../models/review.models';

// API Response wrapper type
interface ApiResponse<T> {
  isSuccess: boolean;
  message?: string;
  result?: T;
}

@Injectable({
  providedIn: 'root'
})
export class ReviewService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.gatewayUrl}/api/reviews`;

  /**
   * Get paginated reviews for a product
   */
  getProductReviews(
    productId: string,
    page: number = 1,
    pageSize: number = 10,
    sortBy: string = 'newest'
  ): Observable<PaginatedReviews> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString())
      .set('sortBy', sortBy);

    return this.http.get<ApiResponse<PaginatedReviews>>(
      `${this.baseUrl}/product/${productId}`,
      { params }
    ).pipe(
      map(response => response.result!)
    );
  }

  /**
   * Get review summary (rating distribution) for a product
   */
  getReviewSummary(productId: string): Observable<ReviewSummary> {
    return this.http.get<ApiResponse<ReviewSummary>>(
      `${this.baseUrl}/product/${productId}/summary`
    ).pipe(
      map(response => response.result!)
    );
  }

  /**
   * Get current user's review for a specific product
   */
  getUserReview(productId: string): Observable<Review | null> {
    return this.http.get<ApiResponse<Review | null>>(
      `${this.baseUrl}/product/${productId}/my-review`
    ).pipe(
      map(response => response.result ?? null)
    );
  }

  /**
   * Get a single review by ID
   */
  getReview(reviewId: string): Observable<Review> {
    return this.http.get<ApiResponse<Review>>(
      `${this.baseUrl}/${reviewId}`
    ).pipe(
      map(response => response.result!)
    );
  }

  /**
   * Create a new review
   */
  createReview(request: CreateReviewRequest): Observable<Review> {
    return this.http.post<ApiResponse<Review>>(this.baseUrl, request).pipe(
      map(response => response.result!)
    );
  }

  /**
   * Update an existing review
   */
  updateReview(request: UpdateReviewRequest): Observable<Review> {
    return this.http.put<ApiResponse<Review>>(
      `${this.baseUrl}/${request.id}`, 
      request
    ).pipe(
      map(response => response.result!)
    );
  }

  /**
   * Delete a review
   */
  deleteReview(reviewId: string): Observable<void> {
    return this.http.delete<ApiResponse<void>>(
      `${this.baseUrl}/${reviewId}`
    ).pipe(
      map(() => undefined)
    );
  }

  /**
   * Vote on a review (helpful/not helpful)
   */
  voteOnReview(vote: ReviewVote): Observable<Review> {
    return this.http.post<ApiResponse<Review>>(
      `${this.baseUrl}/${vote.reviewId}/vote`, 
      vote
    ).pipe(
      map(response => response.result!)
    );
  }

  /**
   * Get all reviews by the current user
   */
  getMyReviews(page: number = 1, pageSize: number = 10): Observable<PaginatedReviews> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<ApiResponse<PaginatedReviews>>(
      `${this.baseUrl}/my-reviews`, 
      { params }
    ).pipe(
      map(response => response.result!)
    );
  }
}