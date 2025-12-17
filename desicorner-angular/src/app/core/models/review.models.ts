// Review returned from API
export interface Review {
  id: string;
  productId: string;
  userId: string;
  userName: string;
  rating: number;
  title?: string;
  comment?: string;
  isVerifiedPurchase: boolean;
  helpfulCount: number;
  notHelpfulCount: number;
  createdAt: Date;
  updatedAt?: Date;
}

// Request to create a review
export interface CreateReviewRequest {
  productId: string;
  rating: number;
  title?: string;
  comment?: string;
}

// Request to update a review
export interface UpdateReviewRequest {
  id: string;
  rating: number;
  title?: string;
  comment?: string;
}

// Review summary/statistics for a product
export interface ReviewSummary {
  productId: string;
  averageRating: number;
  totalReviews: number;
  fiveStarCount: number;
  fourStarCount: number;
  threeStarCount: number;
  twoStarCount: number;
  oneStarCount: number;
  fiveStarPercent: number;
  fourStarPercent: number;
  threeStarPercent: number;
  twoStarPercent: number;
  oneStarPercent: number;
}

// Paginated reviews response
export interface PaginatedReviews {
  reviews: Review[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Vote on a review
export interface ReviewVote {
  reviewId: string;
  isHelpful: boolean;
}