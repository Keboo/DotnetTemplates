// DTOs matching server-side models
export interface RoomDto {
  id: string;
  friendlyName: string;
  createdByUserId: string;
  createdDate: string;
  currentQuestionId?: string;
}

export interface QuestionDto {
  id: string;
  roomId: string;
  questionText: string;
  authorName: string;
  isAnswered: boolean;
  isApproved: boolean;
  createdDate: string;
  lastModifiedDate?: string;
}

export interface UserInfo {
  userId: string;
  userName: string;
  email: string;
  isAuthenticated: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
}

export interface ApiResponse<T = void> {
  success: boolean;
  data?: T;
  errors?: string[];
}

export interface CreateRoomRequest {
  friendlyName: string;
}

export interface CreateQuestionRequest {
  questionText: string;
  authorName: string;
}
