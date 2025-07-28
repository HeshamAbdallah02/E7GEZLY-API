# Flutter Integration Guide for E7GEZLY API

## Table of Contents

1. [Setup & Configuration](#setup--configuration)
2. [HTTP Client Setup](#http-client-setup)
3. [Authentication Service](#authentication-service)
4. [State Management](#state-management)
5. [Error Handling](#error-handling)
6. [Code Examples](#code-examples)
7. [Best Practices](#best-practices)

---

## Setup & Configuration

### Dependencies

Add these packages to your `pubspec.yaml`:

```yaml
dependencies:
  flutter:
    sdk: flutter
  http: ^1.1.0
  dio: ^5.3.2  # Alternative HTTP client with interceptors
  shared_preferences: ^2.2.2
  flutter_secure_storage: ^9.0.0
  provider: ^6.0.5  # or riverpod/bloc for state management
  google_sign_in: ^6.1.5
  sign_in_with_apple: ^5.0.0
  flutter_facebook_auth: ^6.0.3

dev_dependencies:
  build_runner: ^2.4.6
  json_annotation: ^4.8.1
  json_serializable: ^6.7.1
```

### Environment Configuration

Create environment-specific configuration files:

```dart
// lib/config/api_config.dart
class ApiConfig {
  static const String _baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'https://localhost:5001/api',
  );
  
  static const String baseUrl = _baseUrl;
  static const Duration tokenRefreshThreshold = Duration(minutes: 5);
  static const Duration requestTimeout = Duration(seconds: 30);
  
  // Social Auth Configuration
  static const String googleClientId = String.fromEnvironment('GOOGLE_CLIENT_ID');
  static const String facebookAppId = String.fromEnvironment('FACEBOOK_APP_ID');
}
```

---

## HTTP Client Setup

### Dio HTTP Client with Interceptors

```dart
// lib/services/http/dio_client.dart
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../auth/auth_service.dart';
import '../../config/api_config.dart';

class DioClient {
  late final Dio _dio;
  final FlutterSecureStorage _storage = const FlutterSecureStorage();
  
  DioClient() {
    _dio = Dio(BaseOptions(
      baseUrl: ApiConfig.baseUrl,
      connectTimeout: ApiConfig.requestTimeout,
      receiveTimeout: ApiConfig.requestTimeout,
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
    ));
    
    _setupInterceptors();
  }
  
  void _setupInterceptors() {
    // Request Interceptor - Add Authorization Header
    _dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        final token = await _storage.read(key: 'access_token');
        if (token != null) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        handler.next(options);
      },
      
      onError: (error, handler) async {
        // Auto-refresh token on 401 errors
        if (error.response?.statusCode == 401) {
          final refreshed = await _refreshToken();
          if (refreshed) {
            // Retry the original request
            final options = error.requestOptions;
            final token = await _storage.read(key: 'access_token');
            options.headers['Authorization'] = 'Bearer $token';
            
            try {
              final response = await _dio.fetch(options);
              handler.resolve(response);
              return;
            } catch (e) {
              // If retry fails, continue with original error
            }
          }
        }
        handler.next(error);
      },
    ));
    
    // Logging Interceptor (Development only)
    if (kDebugMode) {
      _dio.interceptors.add(LogInterceptor(
        requestBody: true,
        responseBody: true,
        requestHeader: true,
        responseHeader: false,
      ));
    }
  }
  
  Future<bool> _refreshToken() async {
    try {
      final refreshToken = await _storage.read(key: 'refresh_token');
      if (refreshToken == null) return false;
      
      final response = await _dio.post('/auth/token/refresh', data: {
        'refreshToken': refreshToken,
      });
      
      if (response.statusCode == 200) {
        final data = response.data;
        await _storage.write(key: 'access_token', value: data['accessToken']);
        await _storage.write(key: 'refresh_token', value: data['refreshToken']);
        return true;
      }
    } catch (e) {
      // Token refresh failed, user needs to login again
      await _clearTokens();
    }
    return false;
  }
  
  Future<void> _clearTokens() async {
    await _storage.delete(key: 'access_token');
    await _storage.delete(key: 'refresh_token');
  }
  
  Dio get dio => _dio;
}
```

---

## Authentication Service

### Core Authentication Service

```dart
// lib/services/auth/auth_service.dart
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../http/dio_client.dart';
import '../../models/auth/auth_models.dart';

class AuthService {
  final DioClient _dioClient = DioClient();
  final FlutterSecureStorage _storage = const FlutterSecureStorage();
  
  Dio get _dio => _dioClient.dio;
  
  // Customer Registration
  Future<ApiResponse<String>> registerCustomer(CustomerRegistrationRequest request) async {
    try {
      final response = await _dio.post('/auth/customer/register', data: request.toJson());
      
      return ApiResponse<String>.success(
        data: response.data['userId'],
        message: response.data['message'],
      );
    } on DioException catch (e) {
      return ApiResponse<String>.error(_handleDioError(e));
    }
  }
  
  // Customer Login
  Future<ApiResponse<AuthResponse>> loginCustomer(LoginRequest request) async {
    try {
      final response = await _dio.post('/auth/customer/login', data: request.toJson());
      
      final authResponse = AuthResponse.fromJson(response.data);
      await _storeTokens(authResponse.accessToken, authResponse.refreshToken);
      
      return ApiResponse<AuthResponse>.success(data: authResponse);
    } on DioException catch (e) {
      return ApiResponse<AuthResponse>.error(_handleDioError(e));
    }
  }
  
  // Venue Registration
  Future<ApiResponse<VenueRegistrationResponse>> registerVenue(VenueRegistrationRequest request) async {
    try {
      final response = await _dio.post('/auth/venue/register', data: request.toJson());
      
      return ApiResponse<VenueRegistrationResponse>.success(
        data: VenueRegistrationResponse.fromJson(response.data),
      );
    } on DioException catch (e) {
      return ApiResponse<VenueRegistrationResponse>.error(_handleDioError(e));
    }
  }
  
  // Complete Venue Profile
  Future<ApiResponse<VenueProfileResponse>> completeVenueProfile(CompleteVenueProfileRequest request) async {
    try {
      final response = await _dio.post('/auth/venue/complete-profile', data: request.toJson());
      
      return ApiResponse<VenueProfileResponse>.success(
        data: VenueProfileResponse.fromJson(response.data),
      );
    } on DioException catch (e) {
      return ApiResponse<VenueProfileResponse>.error(_handleDioError(e));
    }
  }
  
  // Email Verification
  Future<ApiResponse<void>> verifyEmail(String email, String code) async {
    try {
      await _dio.post('/auth/verify/email', data: {
        'email': email,
        'code': code,
      });
      
      return ApiResponse<void>.success();
    } on DioException catch (e) {
      return ApiResponse<void>.error(_handleDioError(e));
    }
  }
  
  // Phone Verification
  Future<ApiResponse<void>> verifyPhone(String phoneNumber, String code) async {
    try {
      await _dio.post('/auth/verify/phone', data: {
        'phoneNumber': phoneNumber,
        'code': code,
      });
      
      return ApiResponse<void>.success();
    } on DioException catch (e) {
      return ApiResponse<void>.error(_handleDioError(e));
    }
  }
  
  // Resend Verification Code
  Future<ApiResponse<void>> resendVerificationCode({
    required String type, // 'email' or 'phone'
    String? email,
    String? phoneNumber,
    String language = 'en',
  }) async {
    try {
      final data = {
        'type': type,
        'language': language,
      };
      
      if (email != null) data['email'] = email;
      if (phoneNumber != null) data['phoneNumber'] = phoneNumber;
      
      await _dio.post('/auth/verify/resend', data: data);
      
      return ApiResponse<void>.success();
    } on DioException catch (e) {
      return ApiResponse<void>.error(_handleDioError(e));
    }
  }
  
  // Get Current User
  Future<ApiResponse<UserProfile>> getCurrentUser() async {
    try {
      final response = await _dio.get('/auth/account/me');
      
      return ApiResponse<UserProfile>.success(
        data: UserProfile.fromJson(response.data),
      );
    } on DioException catch (e) {
      return ApiResponse<UserProfile>.error(_handleDioError(e));
    }
  }
  
  // Get Active Sessions
  Future<ApiResponse<SessionsResponse>> getActiveSessions() async {
    try {
      final response = await _dio.get('/auth/account/sessions');
      
      return ApiResponse<SessionsResponse>.success(
        data: SessionsResponse.fromJson(response.data),
      );
    } on DioException catch (e) {
      return ApiResponse<SessionsResponse>.error(_handleDioError(e));
    }
  }
  
  // Logout Current Device
  Future<ApiResponse<void>> logout() async {
    try {
      await _dio.post('/auth/account/logout');
      await _clearTokens();
      
      return ApiResponse<void>.success();
    } on DioException catch (e) {
      await _clearTokens(); // Clear tokens even if request fails
      return ApiResponse<void>.error(_handleDioError(e));
    }
  }
  
  // Logout All Devices
  Future<ApiResponse<void>> logoutAllDevices() async {
    try {
      await _dio.post('/auth/account/logout-all-devices');
      await _clearTokens();
      
      return ApiResponse<void>.success();
    } on DioException catch (e) {
      await _clearTokens();
      return ApiResponse<void>.error(_handleDioError(e));
    }
  }
  
  // Change Password
  Future<ApiResponse<void>> changePassword({
    required String currentPassword,
    required String newPassword,
    bool logoutAllDevices = false,
  }) async {
    try {
      await _dio.post('/auth/account/change-password', data: {
        'currentPassword': currentPassword,
        'newPassword': newPassword,
        'logoutAllDevices': logoutAllDevices,
      });
      
      if (logoutAllDevices) {
        await _clearTokens();
      }
      
      return ApiResponse<void>.success();
    } on DioException catch (e) {
      return ApiResponse<void>.error(_handleDioError(e));
    }
  }
  
  // Password Reset Request
  Future<ApiResponse<void>> requestPasswordReset({
    String? email,
    String? phoneNumber,
  }) async {
    try {
      final data = <String, dynamic>{};
      if (email != null) data['email'] = email;
      if (phoneNumber != null) data['phoneNumber'] = phoneNumber;
      
      await _dio.post('/auth/password/request-reset', data: data);
      
      return ApiResponse<void>.success();
    } on DioException catch (e) {
      return ApiResponse<void>.error(_handleDioError(e));
    }
  }
  
  // Verify Reset Code
  Future<ApiResponse<String>> verifyResetCode({
    String? email,
    String? phoneNumber,
    required String code,
  }) async {
    try {
      final data = <String, dynamic>{'code': code};
      if (email != null) data['email'] = email;
      if (phoneNumber != null) data['phoneNumber'] = phoneNumber;
      
      final response = await _dio.post('/auth/password/verify-reset-code', data: data);
      
      return ApiResponse<String>.success(
        data: response.data['resetToken'],
      );
    } on DioException catch (e) {
      return ApiResponse<String>.error(_handleDioError(e));
    }
  }
  
  // Reset Password
  Future<ApiResponse<void>> resetPassword({
    required String resetToken,
    required String newPassword,
  }) async {
    try {
      await _dio.post('/auth/password/reset', data: {
        'resetToken': resetToken,
        'newPassword': newPassword,
      });
      
      return ApiResponse<void>.success();
    } on DioException catch (e) {
      return ApiResponse<void>.error(_handleDioError(e));
    }
  }
  
  // Check if user is authenticated
  Future<bool> isAuthenticated() async {
    final token = await _storage.read(key: 'access_token');
    return token != null;
  }
  
  // Token storage helpers
  Future<void> _storeTokens(String accessToken, String refreshToken) async {
    await _storage.write(key: 'access_token', value: accessToken);
    await _storage.write(key: 'refresh_token', value: refreshToken);
  }
  
  Future<void> _clearTokens() async {
    await _storage.delete(key: 'access_token');
    await _storage.delete(key: 'refresh_token');
  }
  
  // Error handling
  ApiError _handleDioError(DioException e) {
    if (e.response != null) {
      final data = e.response!.data;
      return ApiError(
        code: data['error'] ?? 'UNKNOWN_ERROR',
        message: data['message'] ?? 'An error occurred',
        statusCode: e.response!.statusCode ?? 500,
        details: data['details'],
      );
    } else {
      return ApiError(
        code: 'NETWORK_ERROR',
        message: 'Network connection failed',
        statusCode: 0,
      );
    }
  }
}
```

### Social Authentication Service

```dart
// lib/services/auth/social_auth_service.dart
import 'package:google_sign_in/google_sign_in.dart';
import 'package:flutter_facebook_auth/flutter_facebook_auth.dart';
import 'package:sign_in_with_apple/sign_in_with_apple.dart';
import 'package:dio/dio.dart';
import '../http/dio_client.dart';
import '../../models/auth/auth_models.dart';
import '../../config/api_config.dart';

class SocialAuthService {
  final DioClient _dioClient = DioClient();
  
  Dio get _dio => _dioClient.dio;
  
  // Google Sign-In
  static final GoogleSignIn _googleSignIn = GoogleSignIn(
    clientId: ApiConfig.googleClientId,
  );
  
  Future<ApiResponse<AuthResponse>> signInWithGoogle({
    required String userType, // 'customer' or 'venue'
  }) async {
    try {
      final GoogleSignInAccount? googleUser = await _googleSignIn.signIn();
      if (googleUser == null) {
        return ApiResponse<AuthResponse>.error(
          ApiError(code: 'GOOGLE_SIGN_IN_CANCELLED', message: 'Sign in was cancelled'),
        );
      }
      
      final GoogleSignInAuthentication googleAuth = await googleUser.authentication;
      final String? idToken = googleAuth.idToken;
      
      if (idToken == null) {
        return ApiResponse<AuthResponse>.error(
          ApiError(code: 'GOOGLE_ID_TOKEN_NULL', message: 'Failed to get Google ID token'),
        );
      }
      
      // Send to API
      final response = await _dio.post('/auth/social/google', data: {
        'idToken': idToken,
        'userType': userType,
      });
      
      final authResponse = AuthResponse.fromJson(response.data);
      
      return ApiResponse<AuthResponse>.success(data: authResponse);
    } on DioException catch (e) {
      return ApiResponse<AuthResponse>.error(_handleDioError(e));
    } catch (e) {
      return ApiResponse<AuthResponse>.error(
        ApiError(code: 'GOOGLE_SIGN_IN_ERROR', message: e.toString()),
      );
    }
  }
  
  // Facebook Login
  Future<ApiResponse<AuthResponse>> signInWithFacebook({
    required String userType,
  }) async {
    try {
      final LoginResult result = await FacebookAuth.instance.login();
      
      if (result.status != LoginStatus.success) {
        return ApiResponse<AuthResponse>.error(
          ApiError(
            code: 'FACEBOOK_LOGIN_FAILED',
            message: result.message ?? 'Facebook login failed',
          ),
        );
      }
      
      final AccessToken? accessToken = result.accessToken;
      if (accessToken == null) {
        return ApiResponse<AuthResponse>.error(
          ApiError(code: 'FACEBOOK_TOKEN_NULL', message: 'Failed to get Facebook access token'),
        );
      }
      
      // Send to API
      final response = await _dio.post('/auth/social/facebook', data: {
        'accessToken': accessToken.token,
        'userType': userType,
      });
      
      final authResponse = AuthResponse.fromJson(response.data);
      
      return ApiResponse<AuthResponse>.success(data: authResponse);
    } on DioException catch (e) {
      return ApiResponse<AuthResponse>.error(_handleDioError(e));
    } catch (e) {
      return ApiResponse<AuthResponse>.error(
        ApiError(code: 'FACEBOOK_LOGIN_ERROR', message: e.toString()),
      );
    }
  }
  
  // Apple Sign-In (iOS only)
  Future<ApiResponse<AuthResponse>> signInWithApple({
    required String userType,
  }) async {
    try {
      final credential = await SignInWithApple.getAppleIDCredential(
        scopes: [
          AppleIDAuthorizationScopes.email,
          AppleIDAuthorizationScopes.fullName,
        ],
      );
      
      final String? identityToken = credential.identityToken;
      if (identityToken == null) {
        return ApiResponse<AuthResponse>.error(
          ApiError(code: 'APPLE_TOKEN_NULL', message: 'Failed to get Apple identity token'),
        );
      }
      
      // Send to API
      final response = await _dio.post('/auth/social/apple', data: {
        'identityToken': identityToken,
        'userType': userType,
      });
      
      final authResponse = AuthResponse.fromJson(response.data);
      
      return ApiResponse<AuthResponse>.success(data: authResponse);
    } on SignInWithAppleAuthorizationException catch (e) {
      return ApiResponse<AuthResponse>.error(
        ApiError(
          code: 'APPLE_SIGN_IN_ERROR',
          message: e.message,
        ),
      );
    } on DioException catch (e) {
      return ApiResponse<AuthResponse>.error(_handleDioError(e));
    } catch (e) {
      return ApiResponse<AuthResponse>.error(
        ApiError(code: 'APPLE_SIGN_IN_ERROR', message: e.toString()),
      );
    }
  }
  
  ApiError _handleDioError(DioException e) {
    if (e.response != null) {
      final data = e.response!.data;
      return ApiError(
        code: data['error'] ?? 'UNKNOWN_ERROR',
        message: data['message'] ?? 'An error occurred',
        statusCode: e.response!.statusCode ?? 500,
        details: data['details'],
      );
    } else {
      return ApiError(
        code: 'NETWORK_ERROR',
        message: 'Network connection failed',
        statusCode: 0,
      );
    }
  }
}
```

---

## State Management

### Provider-Based Auth State

```dart
// lib/providers/auth_provider.dart
import 'package:flutter/foundation.dart';
import '../services/auth/auth_service.dart';
import '../services/auth/social_auth_service.dart';
import '../models/auth/auth_models.dart';

enum AuthStatus {
  unknown,
  authenticated,
  unauthenticated,
  authenticating,
}

class AuthProvider extends ChangeNotifier {
  final AuthService _authService = AuthService();
  final SocialAuthService _socialAuthService = SocialAuthService();
  
  AuthStatus _status = AuthStatus.unknown;
  UserProfile? _user;
  String? _error;
  
  AuthStatus get status => _status;
  UserProfile? get user => _user;
  String? get error => _error;
  bool get isAuthenticated => _status == AuthStatus.authenticated;
  bool get isLoading => _status == AuthStatus.authenticating;
  
  // Initialize authentication state
  Future<void> initialize() async {
    _setStatus(AuthStatus.authenticating);
    
    final isAuthenticated = await _authService.isAuthenticated();
    if (isAuthenticated) {
      final result = await _authService.getCurrentUser();
      if (result.isSuccess) {
        _user = result.data;
        _setStatus(AuthStatus.authenticated);
      } else {
        _setStatus(AuthStatus.unauthenticated);
      }
    } else {
      _setStatus(AuthStatus.unauthenticated);
    }
  }
  
  // Customer Registration
  Future<bool> registerCustomer(CustomerRegistrationRequest request) async {
    _setStatus(AuthStatus.authenticating);
    _clearError();
    
    final result = await _authService.registerCustomer(request);
    if (result.isSuccess) {
      _setStatus(AuthStatus.unauthenticated); // Still need verification
      return true;
    } else {
      _setError(result.error!.message);
      _setStatus(AuthStatus.unauthenticated);
      return false;
    }
  }
  
  // Customer Login
  Future<bool> loginCustomer(LoginRequest request) async {
    _setStatus(AuthStatus.authenticating);
    _clearError();
    
    final result = await _authService.loginCustomer(request);
    if (result.isSuccess) {
      _user = UserProfile(
        id: result.data!.user.id,
        email: result.data!.user.email,
        userType: 'customer',
        isEmailVerified: result.data!.user.isEmailVerified,
        isPhoneVerified: result.data!.user.isPhoneVerified,
        profile: result.data!.profile,
      );
      _setStatus(AuthStatus.authenticated);
      return true;
    } else {
      _setError(result.error!.message);
      _setStatus(AuthStatus.unauthenticated);
      return false;
    }
  }
  
  // Venue Registration
  Future<bool> registerVenue(VenueRegistrationRequest request) async {
    _setStatus(AuthStatus.authenticating);
    _clearError();
    
    final result = await _authService.registerVenue(request);
    if (result.isSuccess) {
      _setStatus(AuthStatus.unauthenticated);
      return true;
    } else {
      _setError(result.error!.message);
      _setStatus(AuthStatus.unauthenticated);
      return false;
    }
  }
  
  // Social Authentication
  Future<bool> signInWithGoogle({required String userType}) async {
    _setStatus(AuthStatus.authenticating);
    _clearError();
    
    final result = await _socialAuthService.signInWithGoogle(userType: userType);
    return _handleSocialAuthResult(result);
  }
  
  Future<bool> signInWithFacebook({required String userType}) async {
    _setStatus(AuthStatus.authenticating);
    _clearError();
    
    final result = await _socialAuthService.signInWithFacebook(userType: userType);
    return _handleSocialAuthResult(result);
  }
  
  Future<bool> signInWithApple({required String userType}) async {
    _setStatus(AuthStatus.authenticating);
    _clearError();
    
    final result = await _socialAuthService.signInWithApple(userType: userType);
    return _handleSocialAuthResult(result);
  }
  
  bool _handleSocialAuthResult(ApiResponse<AuthResponse> result) {
    if (result.isSuccess) {
      _user = UserProfile(
        id: result.data!.user.id,
        email: result.data!.user.email,
        userType: result.data!.user.userType ?? 'customer',
        isEmailVerified: result.data!.user.isEmailVerified,
        isPhoneVerified: result.data!.user.isPhoneVerified,
        profile: result.data!.profile,
        venue: result.data!.venue,
      );
      _setStatus(AuthStatus.authenticated);
      return true;
    } else {
      _setError(result.error!.message);
      _setStatus(AuthStatus.unauthenticated);
      return false;
    }
  }
  
  // Verification
  Future<bool> verifyEmail(String email, String code) async {
    _clearError();
    
    final result = await _authService.verifyEmail(email, code);
    if (result.isSuccess) {
      // Update user verification status
      if (_user != null) {
        _user = _user!.copyWith(isEmailVerified: true);
        notifyListeners();
      }
      return true;
    } else {
      _setError(result.error!.message);
      return false;
    }
  }
  
  Future<bool> verifyPhone(String phoneNumber, String code) async {
    _clearError();
    
    final result = await _authService.verifyPhone(phoneNumber, code);
    if (result.isSuccess) {
      // Update user verification status
      if (_user != null) {
        _user = _user!.copyWith(isPhoneVerified: true);
        notifyListeners();
      }
      return true;
    } else {
      _setError(result.error!.message);
      return false;
    }
  }
  
  // Logout
  Future<void> logout() async {
    await _authService.logout();
    _user = null;
    _setStatus(AuthStatus.unauthenticated);
  }
  
  Future<void> logoutAllDevices() async {
    await _authService.logoutAllDevices();
    _user = null;
    _setStatus(AuthStatus.unauthenticated);
  }
  
  // Helper methods
  void _setStatus(AuthStatus status) {
    _status = status;
    notifyListeners();
  }
  
  void _setError(String error) {
    _error = error;
    notifyListeners();
  }
  
  void _clearError() {
    _error = null;
    notifyListeners();
  }
}
```

---

## Error Handling

### Custom Exception Classes

```dart
// lib/models/exceptions/api_exceptions.dart
class ApiException implements Exception {
  final String code;
  final String message;
  final int statusCode;
  final Map<String, dynamic>? details;
  
  const ApiException({
    required this.code,
    required this.message,
    required this.statusCode,
    this.details,
  });
  
  @override
  String toString() => 'ApiException($code): $message';
}

class NetworkException implements Exception {
  final String message;
  
  const NetworkException(this.message);
  
  @override
  String toString() => 'NetworkException: $message';
}

class ValidationException implements Exception {
  final Map<String, List<String>> errors;
  
  const ValidationException(this.errors);
  
  @override
  String toString() => 'ValidationException: $errors';
}
```

### Global Error Handler

```dart
// lib/utils/error_handler.dart
import 'package:flutter/material.dart';
import '../models/exceptions/api_exceptions.dart';

class ErrorHandler {
  static void showError(BuildContext context, dynamic error) {
    String message = 'An unexpected error occurred';
    
    if (error is ApiException) {
      message = _getLocalizedMessage(error.code, error.message);
    } else if (error is NetworkException) {
      message = 'Please check your internet connection';
    } else if (error is ValidationException) {
      message = _formatValidationErrors(error.errors);
    }
    
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: Colors.red,
        duration: const Duration(seconds: 4),
        action: SnackBarAction(
          label: 'Dismiss',
          textColor: Colors.white,
          onPressed: () => ScaffoldMessenger.of(context).hideCurrentSnackBar(),
        ),
      ),
    );
  }
  
  static String _getLocalizedMessage(String code, String defaultMessage) {
    // Map API error codes to user-friendly messages
    switch (code) {
      case 'INVALID_CREDENTIALS':
        return 'Invalid email or password. Please try again.';
      case 'ACCOUNT_NOT_VERIFIED':
        return 'Please verify your account before logging in.';
      case 'TOKEN_EXPIRED':
        return 'Your session has expired. Please log in again.';
      case 'RATE_LIMIT_EXCEEDED':
        return 'Too many attempts. Please wait a moment and try again.';
      case 'NETWORK_ERROR':
        return 'Please check your internet connection and try again.';
      case 'EMAIL_ALREADY_EXISTS':
        return 'An account with this email already exists.';
      case 'PHONE_ALREADY_EXISTS':
        return 'An account with this phone number already exists.';
      case 'INVALID_VERIFICATION_CODE':
        return 'Invalid verification code. Please check and try again.';
      case 'VERIFICATION_CODE_EXPIRED':
        return 'Verification code has expired. Please request a new one.';
      default:
        return defaultMessage;
    }
  }
  
  static String _formatValidationErrors(Map<String, List<String>> errors) {
    final buffer = StringBuffer();
    errors.forEach((field, messages) {
      for (final message in messages) {
        if (buffer.isNotEmpty) buffer.write('\n');
        buffer.write('• $message');
      }
    });
    return buffer.toString();
  }
}
```

---

## Code Examples

### Customer Registration Flow

```dart
// lib/screens/auth/customer_registration_screen.dart
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/auth_provider.dart';
import '../../models/auth/auth_models.dart';
import '../../utils/error_handler.dart';

class CustomerRegistrationScreen extends StatefulWidget {
  @override
  _CustomerRegistrationScreenState createState() => _CustomerRegistrationScreenState();
}

class _CustomerRegistrationScreenState extends State<CustomerRegistrationScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _phoneController = TextEditingController();
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Create Account')),
      body: Consumer<AuthProvider>(
        builder: (context, authProvider, _) {
          return Padding(
            padding: EdgeInsets.all(16),
            child: Form(
              key: _formKey,
              child: Column(
                children: [
                  TextFormField(
                    controller: _emailController,
                    decoration: InputDecoration(labelText: 'Email'),
                    keyboardType: TextInputType.emailAddress,
                    validator: (value) {
                      if (value?.isEmpty ?? true) return 'Email is required';
                      if (!value!.contains('@')) return 'Enter a valid email';
                      return null;
                    },
                  ),
                  TextFormField(
                    controller: _passwordController,
                    decoration: InputDecoration(labelText: 'Password'),
                    obscureText: true,
                    validator: (value) {
                      if (value?.isEmpty ?? true) return 'Password is required';
                      if (value!.length < 8) return 'Password must be at least 8 characters';
                      return null;
                    },
                  ),
                  TextFormField(
                    controller: _firstNameController,
                    decoration: InputDecoration(labelText: 'First Name'),
                    validator: (value) => value?.isEmpty ?? true ? 'First name is required' : null,
                  ),
                  TextFormField(
                    controller: _lastNameController,
                    decoration: InputDecoration(labelText: 'Last Name'),
                    validator: (value) => value?.isEmpty ?? true ? 'Last name is required' : null,
                  ),
                  TextFormField(
                    controller: _phoneController,
                    decoration: InputDecoration(
                      labelText: 'Phone Number',
                      prefixText: '+20 ',
                    ),
                    keyboardType: TextInputType.phone,
                    validator: (value) {
                      if (value?.isEmpty ?? true) return 'Phone number is required';
                      if (value!.length != 10) return 'Enter a valid Egyptian phone number';
                      return null;
                    },
                  ),
                  SizedBox(height: 24),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton(
                      onPressed: authProvider.isLoading ? null : () => _registerCustomer(context, authProvider),
                      child: authProvider.isLoading
                          ? CircularProgressIndicator(color: Colors.white)
                          : Text('Create Account'),
                    ),
                  ),
                  SizedBox(height: 16),
                  Row(
                    children: [
                      Expanded(child: Divider()),
                      Padding(
                        padding: EdgeInsets.symmetric(horizontal: 16),
                        child: Text('OR'),
                      ),
                      Expanded(child: Divider()),
                    ],
                  ),
                  SizedBox(height: 16),
                  _SocialLoginButtons(),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
  
  Future<void> _registerCustomer(BuildContext context, AuthProvider authProvider) async {
    if (!_formKey.currentState!.validate()) return;
    
    final request = CustomerRegistrationRequest(
      email: _emailController.text.trim(),
      password: _passwordController.text,
      firstName: _firstNameController.text.trim(),
      lastName: _lastNameController.text.trim(),
      phoneNumber: _phoneController.text.trim(),
      // Add address fields as needed
    );
    
    final success = await authProvider.registerCustomer(request);
    if (success) {
      Navigator.pushReplacementNamed(context, '/verification');
    } else {
      ErrorHandler.showError(context, authProvider.error);
    }
  }
}

class _SocialLoginButtons extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Consumer<AuthProvider>(
      builder: (context, authProvider, _) {
        return Column(
          children: [
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                onPressed: authProvider.isLoading ? null : () => _signInWithGoogle(context, authProvider),
                icon: Icon(Icons.login), // Use Google icon
                label: Text('Continue with Google'),
                style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
              ),
            ),
            SizedBox(height: 8),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                onPressed: authProvider.isLoading ? null : () => _signInWithFacebook(context, authProvider),
                icon: Icon(Icons.facebook),
                label: Text('Continue with Facebook'),
                style: ElevatedButton.styleFrom(backgroundColor: Colors.blue[800]),
              ),
            ),
            SizedBox(height: 8),
            if (Theme.of(context).platform == TargetPlatform.iOS)
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  onPressed: authProvider.isLoading ? null : () => _signInWithApple(context, authProvider),
                  icon: Icon(Icons.apple),
                  label: Text('Continue with Apple'),
                  style: ElevatedButton.styleFrom(backgroundColor: Colors.black),
                ),
              ),
          ],
        );
      },
    );
  }
  
  Future<void> _signInWithGoogle(BuildContext context, AuthProvider authProvider) async {
    final success = await authProvider.signInWithGoogle(userType: 'customer');
    if (success) {
      Navigator.pushReplacementNamed(context, '/home');
    } else {
      ErrorHandler.showError(context, authProvider.error);
    }
  }
  
  Future<void> _signInWithFacebook(BuildContext context, AuthProvider authProvider) async {
    final success = await authProvider.signInWithFacebook(userType: 'customer');
    if (success) {
      Navigator.pushReplacementNamed(context, '/home');
    } else {
      ErrorHandler.showError(context, authProvider.error);
    }
  }
  
  Future<void> _signInWithApple(BuildContext context, AuthProvider authProvider) async {
    final success = await authProvider.signInWithApple(userType: 'customer');
    if (success) {
      Navigator.pushReplacementNamed(context, '/home');
    } else {
      ErrorHandler.showError(context, authProvider.error);
    }
  }
}
```

### Session Management Example

```dart
// lib/screens/account/session_management_screen.dart
import 'package:flutter/material.dart';
import '../../services/auth/auth_service.dart';
import '../../models/auth/auth_models.dart';

class SessionManagementScreen extends StatefulWidget {
  @override
  _SessionManagementScreenState createState() => _SessionManagementScreenState();
}

class _SessionManagementScreenState extends State<SessionManagementScreen> {
  final AuthService _authService = AuthService();
  List<UserSession> _sessions = [];
  bool _isLoading = true;
  
  @override
  void initState() {
    super.initState();
    _loadSessions();
  }
  
  Future<void> _loadSessions() async {
    setState(() => _isLoading = true);
    
    final result = await _authService.getActiveSessions();
    if (result.isSuccess) {
      setState(() {
        _sessions = result.data!.sessions;
        _isLoading = false;
      });
    } else {
      setState(() => _isLoading = false);
      // Handle error
    }
  }
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Active Sessions'),
        actions: [
          TextButton(
            onPressed: _sessions.length > 1 ? _logoutAllDevices : null,
            child: Text('Logout All'),
          ),
        ],
      ),
      body: _isLoading
          ? Center(child: CircularProgressIndicator())
          : ListView.builder(
              itemCount: _sessions.length,
              itemBuilder: (context, index) {
                final session = _sessions[index];
                return Card(
                  margin: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                  child: ListTile(
                    leading: Icon(
                      _getDeviceIcon(session.deviceName),
                      color: session.isCurrentSession ? Colors.green : Colors.grey,
                    ),
                    title: Text(session.deviceName ?? 'Unknown Device'),
                    subtitle: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('IP: ${session.ipAddress}'),
                        Text('Last Activity: ${_formatDateTime(session.lastActivity)}'),
                        if (session.isCurrentSession)
                          Text(
                            'Current Session',
                            style: TextStyle(
                              color: Colors.green,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                      ],
                    ),
                    trailing: session.isCurrentSession
                        ? null
                        : IconButton(
                            icon: Icon(Icons.close, color: Colors.red),
                            onPressed: () => _revokeSession(session.id),
                          ),
                  ),
                );
              },
            ),
    );
  }
  
  IconData _getDeviceIcon(String? deviceName) {
    if (deviceName?.toLowerCase().contains('iphone') ?? false) {
      return Icons.phone_iphone;
    } else if (deviceName?.toLowerCase().contains('android') ?? false) {
      return Icons.phone_android;
    } else if (deviceName?.toLowerCase().contains('windows') ?? false) {
      return Icons.desktop_windows;
    } else {
      return Icons.devices;
    }
  }
  
  String _formatDateTime(DateTime dateTime) {
    return '${dateTime.day}/${dateTime.month} ${dateTime.hour}:${dateTime.minute.toString().padLeft(2, '0')}';
  }
  
  Future<void> _revokeSession(String sessionId) async {
    // Show confirmation dialog
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('Revoke Session'),
        content: Text('Are you sure you want to logout this device?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: Text('Logout'),
          ),
        ],
      ),
    );
    
    if (confirmed == true) {
      // Make API call to revoke session
      _loadSessions(); // Refresh the list
    }
  }
  
  Future<void> _logoutAllDevices() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('Logout All Devices'),
        content: Text('This will logout all devices including this one. You will need to login again.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: Text('Logout All'),
          ),
        ],
      ),
    );
    
    if (confirmed == true) {
      final result = await _authService.logoutAllDevices();
      if (result.isSuccess) {
        Navigator.pushNamedAndRemoveUntil(context, '/login', (route) => false);
      }
    }
  }
}
```

---

## Best Practices

### Security Best Practices

1. **Token Storage**
   - Use `flutter_secure_storage` for token storage
   - Never store tokens in `SharedPreferences` in production
   - Clear tokens on logout

2. **Network Security**
   - Always use HTTPS in production
   - Implement certificate pinning for enhanced security
   - Validate SSL certificates

3. **Input Validation**
   - Validate all user inputs on the client side
   - Don't rely solely on client-side validation
   - Sanitize inputs before sending to API

### Performance Best Practices

1. **HTTP Client Optimization**
   - Reuse HTTP client instances
   - Implement proper timeout configurations
   - Use connection pooling

2. **Caching Strategy**
   - Cache non-sensitive data locally
   - Implement offline support where appropriate
   - Use appropriate cache expiration policies

3. **State Management**
   - Keep UI state separate from business logic
   - Use proper state management patterns
   - Minimize unnecessary rebuilds

### Code Organization

1. **File Structure**
   ```
   lib/
   ├── config/
   ├── models/
   │   ├── auth/
   │   └── exceptions/
   ├── services/
   │   ├── auth/
   │   └── http/
   ├── providers/
   ├── screens/
   ├── widgets/
   └── utils/
   ```

2. **Dependency Injection**
   - Use dependency injection for services
   - Make services easily testable
   - Separate concerns properly

3. **Error Handling**
   - Implement consistent error handling
   - Use typed exceptions
   - Provide meaningful error messages to users

---

This integration guide provides a comprehensive foundation for integrating Flutter apps with the E7GEZLY API. Adapt the examples based on your specific app architecture and requirements.