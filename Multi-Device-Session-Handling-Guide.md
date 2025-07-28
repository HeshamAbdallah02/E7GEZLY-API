# Multi-Device Session Handling Guide for E7GEZLY

## Table of Contents

1. [Overview](#overview)
2. [Session Architecture](#session-architecture)
3. [Backend Implementation](#backend-implementation)
4. [Flutter Implementation](#flutter-implementation)
5. [Session Management UI](#session-management-ui)
6. [Security Considerations](#security-considerations)
7. [Best Practices](#best-practices)
8. [Troubleshooting](#troubleshooting)

---

## Overview

E7GEZLY's multi-device session management allows users to maintain authenticated sessions across multiple devices simultaneously while providing full control and visibility over their active sessions.

### Key Features
- **Simultaneous Sessions**: Users can be logged in on multiple devices
- **Session Visibility**: View all active sessions with device information
- **Selective Logout**: Logout specific devices without affecting others
- **Automatic Cleanup**: Expired sessions are automatically removed
- **Security Monitoring**: Track device information and last activity

### Session Lifecycle
1. **Login**: Creates new session with device information
2. **Activity Tracking**: Updates last activity timestamp on API calls
3. **Token Refresh**: Extends session lifetime automatically
4. **Manual Logout**: User explicitly logs out from device
5. **Automatic Expiry**: Session expires after inactivity period

---

## Session Architecture

### Database Schema

The `UserSession` table tracks all active sessions:

```sql
CREATE TABLE UserSessions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    RefreshToken NVARCHAR(500) NOT NULL UNIQUE,
    DeviceName NVARCHAR(200),
    DeviceType NVARCHAR(50),
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(1000),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastActivityAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    
    CONSTRAINT FK_UserSessions_Users FOREIGN KEY (UserId) 
        REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    
    INDEX IX_UserSessions_UserId_IsActive (UserId, IsActive),
    INDEX IX_UserSessions_RefreshToken (RefreshToken),
    INDEX IX_UserSessions_ExpiresAt (ExpiresAt)
);
```

### Session Data Model

```csharp
// Models/UserSession.cs
public class UserSession : BaseSyncEntity
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public string? DeviceName { get; set; }
    public string? DeviceType { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime LastActivityAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    
    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
}
```

---

## Backend Implementation

### Token Service with Session Management

```csharp
// Services/Auth/TokenService.cs (Key Methods)
public class TokenService : ITokenService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;
    
    public async Task<AuthResponse> GenerateTokensAsync(ApplicationUser user, string? deviceName = null, string? userAgent = null, string? ipAddress = null)
    {
        // Generate JWT access token
        var accessToken = GenerateAccessToken(user);
        var refreshTokenValue = GenerateRefreshToken();
        
        // Create session record
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshToken = refreshTokenValue,
            DeviceName = ParseDeviceName(userAgent, deviceName),
            DeviceType = ParseDeviceType(userAgent),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsActive = true,
            LastActivityAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30) // 30-day refresh token
        };
        
        _context.UserSessions.Add(session);
        
        // Cleanup old sessions (keep maximum 5 active sessions per user)
        await CleanupOldSessionsAsync(user.Id);
        
        await _context.SaveChangesAsync();
        
        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddHours(4), // 4-hour access token
            User = MapUserResponse(user)
        };
    }
    
    public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken)
    {
        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && s.IsActive);
            
        if (session == null || session.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }
        
        // Update session activity
        session.LastActivityAt = DateTime.UtcNow;
        
        // Generate new refresh token for security
        var newRefreshToken = GenerateRefreshToken();
        session.RefreshToken = newRefreshToken;
        
        await _context.SaveChangesAsync();
        
        // Generate new access token
        var accessToken = GenerateAccessToken(session.User);
        
        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(4),
            User = MapUserResponse(session.User)
        };
    }
    
    public async Task<IEnumerable<SessionDto>> GetActiveSessionsAsync(string userId, string? currentRefreshToken = null)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync();
            
        return sessions.Select(s => new SessionDto
        {
            Id = s.Id,
            DeviceName = s.DeviceName ?? "Unknown Device",
            DeviceType = s.DeviceType ?? "Unknown",
            IpAddress = s.IpAddress ?? "",
            UserAgent = s.UserAgent ?? "",
            LastActivity = s.LastActivityAt,
            IsCurrentSession = s.RefreshToken == currentRefreshToken
        });
    }
    
    public async Task<bool> RevokeSessionAsync(string userId, Guid sessionId)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Id == sessionId && s.IsActive);
            
        if (session == null) return false;
        
        session.IsActive = false;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation($"Session {sessionId} revoked for user {userId}");
        return true;
    }
    
    public async Task<bool> RevokeAllUserTokensAsync(string userId)
    {
        var activeSessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();
            
        foreach (var session in activeSessions)
        {
            session.IsActive = false;
        }
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation($"All sessions revoked for user {userId}");
        return activeSessions.Any();
    }
    
    private async Task CleanupOldSessionsAsync(string userId)
    {
        const int maxSessions = 5;
        
        var activeSessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync();
            
        if (activeSessions.Count >= maxSessions)
        {
            var sessionsToRevoke = activeSessions.Skip(maxSessions - 1);
            foreach (var session in sessionsToRevoke)
            {
                session.IsActive = false;
            }
        }
    }
    
    private string? ParseDeviceName(string? userAgent, string? deviceName)
    {
        if (!string.IsNullOrEmpty(deviceName)) return deviceName;
        
        if (string.IsNullOrEmpty(userAgent)) return null;
        
        // Parse common device patterns from User-Agent
        if (userAgent.Contains("iPhone")) return ExtractIPhoneModel(userAgent);
        if (userAgent.Contains("iPad")) return "iPad";
        if (userAgent.Contains("Android")) return ExtractAndroidDevice(userAgent);
        if (userAgent.Contains("Windows")) return "Windows PC";
        if (userAgent.Contains("Mac")) return "Mac";
        
        return "Unknown Device";
    }
    
    private string? ParseDeviceType(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return null;
        
        if (userAgent.Contains("Mobile") || userAgent.Contains("iPhone") || userAgent.Contains("Android"))
            return "Mobile";
        if (userAgent.Contains("iPad") || userAgent.Contains("Tablet"))
            return "Tablet";
        
        return "Desktop";
    }
}
```

### Background Session Cleanup Service

```csharp
// Services/BackgroundServices/SessionCleanupService.cs
public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Run every hour
    
    public SessionCleanupService(IServiceProvider serviceProvider, ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupExpiredSessionsAsync();
            await Task.Delay(_interval, stoppingToken);
        }
    }
    
    private async Task CleanupExpiredSessionsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var expiredSessions = await context.UserSessions
                .Where(s => s.IsActive && s.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();
                
            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
            }
            
            if (expiredSessions.Any())
            {
                await context.SaveChangesAsync();
                _logger.LogInformation($"Cleaned up {expiredSessions.Count} expired sessions");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during session cleanup");
        }
    }
}
```

---

## Flutter Implementation

### Session-Aware HTTP Client

```dart
// lib/services/http/session_aware_dio_client.dart
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:device_info_plus/device_info_plus.dart';

class SessionAwareDioClient {
  late final Dio _dio;
  final FlutterSecureStorage _storage = const FlutterSecureStorage();
  final DeviceInfoPlugin _deviceInfo = DeviceInfoPlugin();
  
  SessionAwareDioClient() {
    _dio = Dio(BaseOptions(
      baseUrl: ApiConfig.baseUrl,
      connectTimeout: Duration(seconds: 30),
      receiveTimeout: Duration(seconds: 30),
    ));
    
    _setupInterceptors();
  }
  
  void _setupInterceptors() {
    _dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        // Add device information to headers
        final deviceInfo = await _getDeviceInfo();
        options.headers.addAll(deviceInfo);
        
        // Add authorization token
        final token = await _storage.read(key: 'access_token');
        if (token != null) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        
        handler.next(options);
      },
      
      onResponse: (response, handler) async {
        // Update session activity timestamp
        await _updateSessionActivity();
        handler.next(response);
      },
      
      onError: (error, handler) async {
        if (error.response?.statusCode == 401) {
          final success = await _handleTokenRefresh();
          if (success) {
            // Retry original request
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
          } else {
            // Token refresh failed, user needs to login again
            await _handleSessionExpired();
          }
        }
        handler.next(error);
      },
    ));
  }
  
  Future<Map<String, String>> _getDeviceInfo() async {
    try {
      if (Platform.isAndroid) {
        final androidInfo = await _deviceInfo.androidInfo;
        return {
          'X-Device-Name': '${androidInfo.brand} ${androidInfo.model}',
          'X-Device-Type': 'Mobile',
          'X-Device-OS': 'Android ${androidInfo.version.release}',
        };
      } else if (Platform.isIOS) {
        final iosInfo = await _deviceInfo.iosInfo;
        return {
          'X-Device-Name': '${iosInfo.name}',
          'X-Device-Type': iosInfo.model.contains('iPad') ? 'Tablet' : 'Mobile',
          'X-Device-OS': 'iOS ${iosInfo.systemVersion}',
        };
      }
    } catch (e) {
      print('Error getting device info: $e');
    }
    
    return {
      'X-Device-Name': 'Unknown Device',
      'X-Device-Type': 'Unknown',
      'X-Device-OS': 'Unknown',
    };
  }
  
  Future<void> _updateSessionActivity() async {
    // Store last activity timestamp locally
    await _storage.write(
      key: 'last_activity',
      value: DateTime.now().toIso8601String(),
    );
  }
  
  Future<bool> _handleTokenRefresh() async {
    try {
      final refreshToken = await _storage.read(key: 'refresh_token');
      if (refreshToken == null) return false;
      
      final response = await _dio.post(
        '/auth/token/refresh',
        data: {'refreshToken': refreshToken},
        options: Options(
          headers: {'Authorization': null}, // Don't send expired token
        ),
      );
      
      if (response.statusCode == 200) {
        final data = response.data;
        await _storage.write(key: 'access_token', value: data['accessToken']);
        await _storage.write(key: 'refresh_token', value: data['refreshToken']);
        return true;
      }
    } catch (e) {
      print('Token refresh failed: $e');
    }
    
    return false;
  }
  
  Future<void> _handleSessionExpired() async {
    // Clear stored tokens
    await _storage.delete(key: 'access_token');
    await _storage.delete(key: 'refresh_token');
    await _storage.delete(key: 'last_activity');
    
    // Navigate to login screen
    // This would typically be handled by your app's state management
    print('Session expired - user needs to login again');
  }
  
  Dio get dio => _dio;
}
```

### Session Management Service

```dart
// lib/services/session/session_management_service.dart
import 'package:dio/dio.dart';
import '../http/session_aware_dio_client.dart';

class SessionManagementService {
  final SessionAwareDioClient _dioClient = SessionAwareDioClient();
  
  Dio get _dio => _dioClient.dio;
  
  Future<ApiResponse<List<SessionInfo>>> getActiveSessions() async {
    try {
      final response = await _dio.get('/auth/account/sessions');
      
      final sessions = (response.data['sessions'] as List)
          .map((json) => SessionInfo.fromJson(json))
          .toList();
      
      return ApiResponse<List<SessionInfo>>.success(data: sessions);
    } on DioException catch (e) {
      return ApiResponse<List<SessionInfo>>.error(_handleDioError(e));
    }
  }
  
  Future<ApiResponse<void>> revokeSession(String sessionId) async {
    try {
      await _dio.delete('/auth/account/sessions/$sessionId');
      return ApiResponse<void>.success();
    } on DioException catch (e) {
      return ApiResponse<void>.error(_handleDioError(e));
    }
  }
  
  Future<ApiResponse<void>> logoutCurrentDevice() async {
    try {
      await _dio.post('/auth/account/logout');
      await _clearLocalSession();
      return ApiResponse<void>.success();
    } on DioException catch (e) {
      await _clearLocalSession(); // Clear local data even if request fails
      return ApiResponse<void>.error(_handleDioError(e));
    }
  }
  
  Future<ApiResponse<void>> logoutAllDevices() async {
    try {
      await _dio.post('/auth/account/logout-all-devices');
      await _clearLocalSession();
      return ApiResponse<void>.success();
    } on DioException catch (e) {
      await _clearLocalSession();
      return ApiResponse<void>.error(_handleDioError(e));
    }
  }
  
  Future<void> _clearLocalSession() async {
    final storage = FlutterSecureStorage();
    await storage.delete(key: 'access_token');
    await storage.delete(key: 'refresh_token');
    await storage.delete(key: 'last_activity');
  }
  
  ApiError _handleDioError(DioException e) {
    if (e.response != null) {
      final data = e.response!.data;
      return ApiError(
        code: data['error'] ?? 'UNKNOWN_ERROR',
        message: data['message'] ?? 'An error occurred',
        statusCode: e.response!.statusCode ?? 500,
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

class SessionInfo {
  final String id;
  final String deviceName;
  final String deviceType;
  final String ipAddress;
  final DateTime lastActivity;
  final bool isCurrentSession;
  
  SessionInfo({
    required this.id,
    required this.deviceName,
    required this.deviceType,
    required this.ipAddress,
    required this.lastActivity,
    required this.isCurrentSession,
  });
  
  factory SessionInfo.fromJson(Map<String, dynamic> json) {
    return SessionInfo(
      id: json['id'],
      deviceName: json['deviceName'] ?? 'Unknown Device',
      deviceType: json['deviceType'] ?? 'Unknown',
      ipAddress: json['ipAddress'] ?? '',
      lastActivity: DateTime.parse(json['lastActivity']),
      isCurrentSession: json['isCurrentSession'] ?? false,
    );
  }
}
```

---

## Session Management UI

### Session List Screen

```dart
// lib/screens/account/session_management_screen.dart
import 'package:flutter/material.dart';
import '../../services/session/session_management_service.dart';
import '../../models/session_info.dart';
import '../../utils/date_formatter.dart';

class SessionManagementScreen extends StatefulWidget {
  @override
  _SessionManagementScreenState createState() => _SessionManagementScreenState();
}

class _SessionManagementScreenState extends State<SessionManagementScreen> {
  final SessionManagementService _sessionService = SessionManagementService();
  List<SessionInfo> _sessions = [];
  bool _isLoading = true;
  String? _error;
  
  @override
  void initState() {
    super.initState();
    _loadSessions();
  }
  
  Future<void> _loadSessions() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    
    final result = await _sessionService.getActiveSessions();
    
    setState(() {
      _isLoading = false;
      if (result.isSuccess) {
        _sessions = result.data!;
      } else {
        _error = result.error!.message;
      }
    });
  }
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Active Sessions'),
        actions: [
          if (_sessions.length > 1)
            TextButton(
              onPressed: _logoutAllDevices,
              child: Text(
                'Logout All',
                style: TextStyle(color: Colors.red),
              ),
            ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _loadSessions,
        child: _buildBody(),
      ),
    );
  }
  
  Widget _buildBody() {
    if (_isLoading) {
      return Center(child: CircularProgressIndicator());
    }
    
    if (_error != null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.error_outline, size: 64, color: Colors.grey),
            SizedBox(height: 16),
            Text(
              'Failed to load sessions',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            SizedBox(height: 8),
            Text(
              _error!,
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                color: Colors.grey[600],
              ),
              textAlign: TextAlign.center,
            ),
            SizedBox(height: 16),
            ElevatedButton(
              onPressed: _loadSessions,
              child: Text('Retry'),
            ),
          ],
        ),
      );
    }
    
    if (_sessions.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.devices_other, size: 64, color: Colors.grey),
            SizedBox(height: 16),
            Text(
              'No active sessions',
              style: Theme.of(context).textTheme.titleMedium,
            ),
          ],
        ),
      );
    }
    
    return ListView.builder(
      padding: EdgeInsets.all(16),
      itemCount: _sessions.length,
      itemBuilder: (context, index) {
        final session = _sessions[index];
        return SessionCard(
          session: session,
          onRevoke: session.isCurrentSession ? null : () => _revokeSession(session),
        );
      },
    );
  }
  
  Future<void> _revokeSession(SessionInfo session) async {
    final confirmed = await _showConfirmationDialog(
      title: 'Logout Device',
      message: 'Are you sure you want to logout ${session.deviceName}?',
    );
    
    if (confirmed == true) {
      final result = await _sessionService.revokeSession(session.id);
      
      if (result.isSuccess) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Device logged out successfully')),
        );
        _loadSessions(); // Refresh the list
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Failed to logout device: ${result.error!.message}'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }
  
  Future<void> _logoutAllDevices() async {
    final confirmed = await _showConfirmationDialog(
      title: 'Logout All Devices',
      message: 'This will logout all devices including this one. You will need to login again.',
    );
    
    if (confirmed == true) {
      final result = await _sessionService.logoutAllDevices();
      
      if (result.isSuccess) {
        // Navigate to login screen
        Navigator.of(context).pushNamedAndRemoveUntil(
          '/login',
          (route) => false,
        );
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Failed to logout all devices: ${result.error!.message}'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }
  
  Future<bool?> _showConfirmationDialog({
    required String title,
    required String message,
  }) {
    return showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(title),
        content: Text(message),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            style: TextButton.styleFrom(foregroundColor: Colors.red),
            child: Text('Logout'),
          ),
        ],
      ),
    );
  }
}
```

### Session Card Widget

```dart
// lib/widgets/session_card.dart
import 'package:flutter/material.dart';
import '../models/session_info.dart';
import '../utils/date_formatter.dart';

class SessionCard extends StatelessWidget {
  final SessionInfo session;
  final VoidCallback? onRevoke;
  
  const SessionCard({
    Key? key,
    required this.session,
    this.onRevoke,
  }) : super(key: key);
  
  @override
  Widget build(BuildContext context) {
    return Card(
      margin: EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(
                  _getDeviceIcon(),
                  color: session.isCurrentSession ? Colors.green : Colors.grey[600],
                  size: 32,
                ),
                SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        session.deviceName,
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      Text(
                        session.deviceType,
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                          color: Colors.grey[600],
                        ),
                      ),
                    ],
                  ),
                ),
                if (session.isCurrentSession)
                  Container(
                    padding: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                    decoration: BoxDecoration(
                      color: Colors.green.withOpacity(0.1),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Text(
                      'Current',
                      style: TextStyle(
                        color: Colors.green[700],
                        fontSize: 12,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                  )
                else if (onRevoke != null)
                  IconButton(
                    onPressed: onRevoke,
                    icon: Icon(Icons.logout, color: Colors.red),
                    tooltip: 'Logout this device',
                  ),
              ],
            ),
            SizedBox(height: 12),
            _buildInfoRow(Icons.location_on, 'IP Address', session.ipAddress),
            SizedBox(height: 4),
            _buildInfoRow(
              Icons.access_time,
              'Last Activity',
              DateFormatter.formatRelativeTime(session.lastActivity),
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildInfoRow(IconData icon, String label, String value) {
    return Row(
      children: [
        Icon(icon, size: 16, color: Colors.grey[600]),
        SizedBox(width: 8),
        Text(
          '$label: ',
          style: TextStyle(
            fontSize: 13,
            color: Colors.grey[600],
            fontWeight: FontWeight.w500,
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: TextStyle(fontSize: 13),
          ),
        ),
      ],
    );
  }
  
  IconData _getDeviceIcon() {
    switch (session.deviceType.toLowerCase()) {
      case 'mobile':
        if (session.deviceName.toLowerCase().contains('iphone')) {
          return Icons.phone_iphone;
        } else {
          return Icons.phone_android;
        }
      case 'tablet':
        return Icons.tablet;
      case 'desktop':
        return Icons.desktop_windows;
      default:
        return Icons.devices;
    }
  }
}
```

### Date Formatter Utility

```dart
// lib/utils/date_formatter.dart
class DateFormatter {
  static String formatRelativeTime(DateTime dateTime) {
    final now = DateTime.now();
    final difference = now.difference(dateTime);
    
    if (difference.inMinutes < 1) {
      return 'Just now';
    } else if (difference.inMinutes < 60) {
      return '${difference.inMinutes}m ago';
    } else if (difference.inHours < 24) {
      return '${difference.inHours}h ago';
    } else if (difference.inDays < 7) {
      return '${difference.inDays}d ago';
    } else {
      return '${dateTime.day}/${dateTime.month}/${dateTime.year}';
    }
  }
  
  static String formatFullDateTime(DateTime dateTime) {
    return '${dateTime.day}/${dateTime.month}/${dateTime.year}'
           ' ${dateTime.hour.toString().padLeft(2, '0')}:'
           '${dateTime.minute.toString().padLeft(2, '0')}';
  }
}
```

---

## Security Considerations

### Token Security
1. **Refresh Token Rotation**: New refresh token issued on each refresh
2. **Token Expiration**: Short-lived access tokens (4 hours)
3. **Secure Storage**: Tokens stored in Flutter Secure Storage
4. **Automatic Cleanup**: Expired sessions automatically removed

### Session Limits
```csharp
// Prevent session abuse
private const int MAX_SESSIONS_PER_USER = 5;
private const int SESSION_INACTIVITY_DAYS = 30;
```

### Monitoring & Alerts
```csharp
// Log suspicious activity
if (activeSessions.Count > MAX_SESSIONS_PER_USER)
{
    _logger.LogWarning($"User {userId} has {activeSessions.Count} active sessions");
}
```

### Device Fingerprinting
```dart
// Enhanced device identification
Future<String> generateDeviceFingerprint() async {
  final deviceInfo = await DeviceInfoPlugin().deviceInfo;
  final packageInfo = await PackageInfo.fromPlatform();
  
  return _hashCombination([
    deviceInfo.toString(),
    packageInfo.appName,
    packageInfo.version,
  ]);
}
```

---

## Best Practices

### 1. Session Lifecycle Management
- **Automatic Token Refresh**: Refresh tokens before expiration
- **Background Sync**: Update session activity during app usage
- **Graceful Degradation**: Handle network failures gracefully

### 2. User Experience
- **Session Visibility**: Show clear device information
- **Easy Management**: Simple logout options
- **Current Session Protection**: Prevent accidental self-logout

### 3. Performance Optimization
- **Efficient Queries**: Index session tables properly
- **Background Cleanup**: Regular cleanup of expired sessions
- **Caching Strategy**: Cache session data locally when appropriate

### 4. Security Best Practices
- **Rate Limiting**: Limit session creation attempts
- **Anomaly Detection**: Monitor unusual session patterns
- **Secure Headers**: Include security headers in API responses

---

## Troubleshooting

### Common Issues

#### 1. Token Refresh Loops
```
Problem: Infinite token refresh attempts
Solution: Implement exponential backoff and maximum retry limits
```

```dart
class TokenRefreshManager {
  int _retryCount = 0;
  static const int MAX_RETRIES = 3;
  
  Future<bool> refreshWithBackoff() async {
    if (_retryCount >= MAX_RETRIES) {
      await _handleSessionExpired();
      return false;
    }
    
    final delay = Duration(seconds: math.pow(2, _retryCount).toInt());
    await Future.delayed(delay);
    
    _retryCount++;
    return await _performRefresh();
  }
}
```

#### 2. Session Sync Issues
```
Problem: Sessions not syncing across devices
Solution: Implement proper session invalidation and real-time updates
```

#### 3. Device Detection Accuracy
```
Problem: Inaccurate device names
Solution: Combine User-Agent parsing with device info plugins
```

### Debug Tools

#### Session Debug Screen
```dart
class SessionDebugScreen extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Session Debug')),
      body: FutureBuilder<Map<String, dynamic>>(
        future: _getDebugInfo(),
        builder: (context, snapshot) {
          if (snapshot.hasData) {
            return ListView(
              padding: EdgeInsets.all(16),
              children: [
                _buildDebugSection('Stored Tokens', snapshot.data!['tokens']),
                _buildDebugSection('Device Info', snapshot.data!['device']),
                _buildDebugSection('Network Info', snapshot.data!['network']),
              ],
            );
          }
          return Center(child: CircularProgressIndicator());
        },
      ),
    );
  }
  
  Future<Map<String, dynamic>> _getDebugInfo() async {
    // Collect debug information
    return {
      'tokens': await _getTokenInfo(),
      'device': await _getDeviceInfo(),
      'network': await _getNetworkInfo(),
    };
  }
}
```

### Performance Monitoring

#### Session Metrics
```csharp
// Track session metrics
public class SessionMetrics
{
    public static void TrackSessionCreated(string userId, string deviceType)
    {
        _telemetry.TrackEvent("SessionCreated", new Dictionary<string, string>
        {
            ["UserId"] = userId,
            ["DeviceType"] = deviceType
        });
    }
    
    public static void TrackSessionRevoked(string userId, TimeSpan sessionDuration)
    {
        _telemetry.TrackMetric("SessionDuration", sessionDuration.TotalMinutes);
    }
}
```

---

## Conclusion

E7GEZLY's multi-device session handling provides a secure, user-friendly way to manage authentication across multiple devices. The implementation includes:

- **Comprehensive Session Tracking**: Device information, activity monitoring
- **Flexible Management**: Selective and bulk logout options
- **Security Features**: Token rotation, automatic cleanup, session limits
- **User Experience**: Clear visibility and easy management
- **Performance**: Efficient database queries and background processing

This guide provides the foundation for implementing robust multi-device session management in your Flutter applications integrated with the E7GEZLY API.

For additional support or questions about session management, refer to the main API documentation and Flutter integration guide.