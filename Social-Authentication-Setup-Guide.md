# Social Authentication Setup Guide for E7GEZLY

## Table of Contents

1. [Overview](#overview)
2. [Google Sign-In Setup](#google-sign-in-setup)
3. [Facebook Login Setup](#facebook-login-setup)
4. [Apple Sign-In Setup](#apple-sign-in-setup)
5. [Backend Configuration](#backend-configuration)
6. [Flutter Implementation](#flutter-implementation)
7. [Testing & Debugging](#testing--debugging)
8. [Production Deployment](#production-deployment)

---

## Overview

E7GEZLY supports three social authentication providers:
- **Google Sign-In**: Cross-platform (iOS, Android, Web)
- **Facebook Login**: Cross-platform (iOS, Android, Web)
- **Apple Sign-In**: iOS only (required by App Store guidelines)

### Authentication Flow
1. User initiates social login in the app
2. Social provider authenticates user and returns token
3. App sends token to E7GEZLY API
4. API validates token with social provider
5. API creates/links account and returns JWT tokens

---

## Google Sign-In Setup

### 1. Google Cloud Console Configuration

#### Create Google Cloud Project
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create new project or select existing one
3. Enable **Google+ API** and **Google Sign-In API**

#### Configure OAuth 2.0
1. Navigate to **APIs & Services** → **Credentials**
2. Click **Create Credentials** → **OAuth 2.0 Client IDs**
3. Configure for each platform:

**Android Configuration:**
```
Application type: Android
Package name: com.e7gezly.customer (or your package name)
SHA-1 certificate fingerprint: [Your signing certificate SHA-1]
```

**iOS Configuration:**
```
Application type: iOS
Bundle ID: com.e7gezly.customer (or your bundle ID)
```

**Web Application (for development):**
```
Application type: Web application
Authorized origins: http://localhost:5001, https://your-domain.com
```

#### Get Configuration Files

**For Android:**
1. Download `google-services.json`
2. Place in `android/app/` directory

**For iOS:**
1. Download `GoogleService-Info.plist`
2. Add to iOS project in Xcode

### 2. Flutter Configuration

#### pubspec.yaml
```yaml
dependencies:
  google_sign_in: ^6.1.5
```

#### Android Setup

**android/app/build.gradle:**
```gradle
android {
    compileSdkVersion 33
    
    defaultConfig {
        minSdkVersion 21
        targetSdkVersion 33
        // ... other config
    }
}

dependencies {
    implementation 'com.google.android.gms:play-services-auth:20.7.0'
}
```

**android/build.gradle:**
```gradle
dependencies {
    classpath 'com.google.gms:google-services:4.3.15'
}
```

**android/app/build.gradle (apply plugin):**
```gradle
apply plugin: 'com.google.gms.google-services'
```

#### iOS Setup

**ios/Runner/Info.plist:**
```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLName</key>
        <string>REVERSED_CLIENT_ID</string>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>com.googleusercontent.apps.YOUR_CLIENT_ID</string>
        </array>
    </dict>
</array>
```

### 3. Implementation Example

```dart
// lib/services/google_auth_service.dart
import 'package:google_sign_in/google_sign_in.dart';

class GoogleAuthService {
  static final GoogleSignIn _googleSignIn = GoogleSignIn(
    scopes: [
      'email',
      'profile',
    ],
  );
  
  static Future<GoogleSignInAccount?> signIn() async {
    try {
      final GoogleSignInAccount? account = await _googleSignIn.signIn();
      return account;
    } catch (error) {
      print('Google Sign-In Error: $error');
      return null;
    }
  }
  
  static Future<String?> getIdToken() async {
    final GoogleSignInAccount? account = await _googleSignIn.signInSilently();
    if (account != null) {
      final GoogleSignInAuthentication auth = await account.authentication;
      return auth.idToken;
    }
    return null;
  }
  
  static Future<void> signOut() async {
    await _googleSignIn.signOut();
  }
}
```

---

## Facebook Login Setup

### 1. Facebook Developer Console Configuration

#### Create Facebook App
1. Go to [Facebook Developers](https://developers.facebook.com/)
2. Create new app → **Consumer** type
3. Add **Facebook Login** product

#### Configure Facebook Login
1. Go to **Facebook Login** → **Settings**
2. Add Valid OAuth Redirect URIs:
   - `https://your-api-domain.com/auth/facebook/callback`
3. Configure **Client OAuth Settings**:
   - Web OAuth Login: Yes
   - Enforce HTTPS: Yes (production)

#### Get App Credentials
- **App ID**: Found in App Dashboard
- **App Secret**: Found in App Dashboard → Settings → Basic

### 2. Platform Configuration

#### Android Setup

**android/app/src/main/res/values/strings.xml:**
```xml
<string name="facebook_app_id">YOUR_FACEBOOK_APP_ID</string>
<string name="fb_login_protocol_scheme">fbYOUR_FACEBOOK_APP_ID</string>
<string name="facebook_client_token">YOUR_FACEBOOK_CLIENT_TOKEN</string>
```

**android/app/src/main/AndroidManifest.xml:**
```xml
<application>
    <!-- Facebook Configuration -->
    <meta-data 
        android:name="com.facebook.sdk.ApplicationId" 
        android:value="@string/facebook_app_id"/>
    <meta-data 
        android:name="com.facebook.sdk.ClientToken" 
        android:value="@string/facebook_client_token"/>
    
    <!-- Facebook Login Activity -->
    <activity 
        android:name="com.facebook.FacebookActivity"
        android:configChanges="keyboard|keyboardHidden|screenLayout|screenSize|orientation"
        android:label="@string/app_name" />
    
    <activity
        android:name="com.facebook.CustomTabActivity"
        android:exported="true">
        <intent-filter>
            <action android:name="android.intent.action.VIEW" />
            <category android:name="android.intent.category.DEFAULT" />
            <category android:name="android.intent.category.BROWSABLE" />
            <data android:scheme="@string/fb_login_protocol_scheme" />
        </intent-filter>
    </activity>
</application>
```

#### iOS Setup

**ios/Runner/Info.plist:**
```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLName</key>
        <string>com.facebook.sdk</string>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>fbYOUR_FACEBOOK_APP_ID</string>
        </array>
    </dict>
</array>

<key>FacebookAppID</key>
<string>YOUR_FACEBOOK_APP_ID</string>
<key>FacebookClientToken</key>
<string>YOUR_FACEBOOK_CLIENT_TOKEN</string>
<key>FacebookDisplayName</key>
<string>E7GEZLY</string>

<key>LSApplicationQueriesSchemes</key>
<array>
    <string>fbapi</string>
    <string>fb-messenger-share-api</string>
    <string>fbauth2</string>
    <string>fbshareextension</string>
</array>
```

### 3. Flutter Implementation

#### pubspec.yaml
```yaml
dependencies:
  flutter_facebook_auth: ^6.0.3
```

#### Implementation Example
```dart
// lib/services/facebook_auth_service.dart
import 'package:flutter_facebook_auth/flutter_facebook_auth.dart';

class FacebookAuthService {
  static Future<AccessToken?> signIn() async {
    try {
      final LoginResult result = await FacebookAuth.instance.login(
        permissions: ['email', 'public_profile'],
      );
      
      if (result.status == LoginStatus.success) {
        return result.accessToken;
      } else {
        print('Facebook Login Error: ${result.message}');
        return null;
      }
    } catch (error) {
      print('Facebook Sign-In Error: $error');
      return null;
    }
  }
  
  static Future<Map<String, dynamic>?> getUserData() async {
    try {
      final userData = await FacebookAuth.instance.getUserData(
        fields: "name,email,picture.width(200)",
      );
      return userData;
    } catch (error) {
      print('Facebook Get User Data Error: $error');
      return null;
    }
  }
  
  static Future<void> signOut() async {
    await FacebookAuth.instance.logOut();
  }
}
```

---

## Apple Sign-In Setup

### 1. Apple Developer Configuration

#### Enable Sign in with Apple
1. Go to [Apple Developer Portal](https://developer.apple.com/)
2. Navigate to **Certificates, Identifiers & Profiles**
3. Select your App ID
4. Enable **Sign in with Apple** capability
5. Configure your app in Xcode to include the capability

#### Service Configuration
1. Go to **Services** in Apple Developer
2. Configure **Sign in with Apple** for your app
3. Add domains and email addresses for communication

### 2. iOS Project Configuration

#### Xcode Setup
1. Open `ios/Runner.xcworkspace` in Xcode
2. Select your target → **Signing & Capabilities**
3. Add **Sign in with Apple** capability
4. Ensure proper team and bundle ID are set

#### Info.plist (automatically configured by Xcode capability)

### 3. Flutter Implementation

#### pubspec.yaml
```yaml
dependencies:
  sign_in_with_apple: ^5.0.0
```

#### Implementation Example
```dart
// lib/services/apple_auth_service.dart
import 'package:sign_in_with_apple/sign_in_with_apple.dart';

class AppleAuthService {
  static Future<AuthorizationCredentialAppleID?> signIn() async {
    try {
      final credential = await SignInWithApple.getAppleIDCredential(
        scopes: [
          AppleIDAuthorizationScopes.email,
          AppleIDAuthorizationScopes.fullName,
        ],
        webAuthenticationOptions: WebAuthenticationOptions(
          clientId: 'com.e7gezly.customer', // Your service ID
          redirectUri: Uri.parse('https://your-api-domain.com/auth/apple/callback'),
        ),
      );
      
      return credential;
    } on SignInWithAppleAuthorizationException catch (e) {
      print('Apple Sign-In Error: ${e.code} - ${e.message}');
      return null;
    } catch (error) {
      print('Apple Sign-In Error: $error');
      return null;
    }
  }
  
  static Future<bool> isAvailable() async {
    return await SignInWithApple.isAvailable();
  }
}
```

---

## Backend Configuration

### 1. Environment Variables

#### appsettings.json (Development)
```json
{
  "SocialAuth": {
    "Google": {
      "ClientId": "your-google-client-id.googleusercontent.com",
      "ClientSecret": "your-google-client-secret"
    },
    "Facebook": {
      "AppId": "your-facebook-app-id",
      "AppSecret": "your-facebook-app-secret"
    },
    "Apple": {
      "ClientId": "com.e7gezly.customer",
      "TeamId": "your-apple-team-id",
      "KeyId": "your-apple-key-id",
      "PrivateKey": "-----BEGIN PRIVATE KEY-----\nYOUR_PRIVATE_KEY\n-----END PRIVATE KEY-----"
    }
  }
}
```

#### appsettings.Production.json
```json
{
  "SocialAuth": {
    "Google": {
      "ClientId": "#{GOOGLE_CLIENT_ID}#",
      "ClientSecret": "#{GOOGLE_CLIENT_SECRET}#"
    },
    "Facebook": {
      "AppId": "#{FACEBOOK_APP_ID}#",
      "AppSecret": "#{FACEBOOK_APP_SECRET}#"
    },
    "Apple": {
      "ClientId": "#{APPLE_CLIENT_ID}#",
      "TeamId": "#{APPLE_TEAM_ID}#",
      "KeyId": "#{APPLE_KEY_ID}#",
      "PrivateKey": "#{APPLE_PRIVATE_KEY}#"
    }
  }
}
```

### 2. Backend Service Configuration

The E7GEZLY API already includes social authentication services. Here's how they work:

#### SocialAuthService.cs
```csharp
// Services/Auth/SocialAuthService.cs
public async Task<AuthResponse> AuthenticateWithGoogleAsync(string idToken, string userType)
{
    // Validate Google ID token
    var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings()
    {
        Audience = new[] { _googleSettings.ClientId }
    });

    // Create or find user
    var user = await FindOrCreateUserAsync(payload.Email, payload.Name, "Google", payload.Subject, userType);
    
    // Generate JWT tokens
    return await GenerateAuthResponseAsync(user);
}
```

### 3. Database Schema

The API already includes `ExternalLogin` table for social authentication:

```sql
CREATE TABLE ExternalLogins (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    Provider NVARCHAR(50) NOT NULL, -- 'Google', 'Facebook', 'Apple'
    ProviderUserId NVARCHAR(255) NOT NULL,
    ProviderEmail NVARCHAR(500),
    ProviderDisplayName NVARCHAR(200),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_ExternalLogins_Users FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id),
    CONSTRAINT IX_ExternalLogin_Provider_ProviderUserId UNIQUE (Provider, ProviderUserId)
);
```

---

## Flutter Implementation

### Complete Social Authentication Widget

```dart
// lib/widgets/social_auth_buttons.dart
import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart';
import '../services/google_auth_service.dart';
import '../services/facebook_auth_service.dart';
import '../services/apple_auth_service.dart';
import '../services/auth/auth_service.dart';

class SocialAuthButtons extends StatefulWidget {
  final String userType; // 'customer' or 'venue'
  final VoidCallback? onSuccess;
  final Function(String)? onError;
  
  const SocialAuthButtons({
    Key? key,
    required this.userType,
    this.onSuccess,
    this.onError,
  }) : super(key: key);
  
  @override
  _SocialAuthButtonsState createState() => _SocialAuthButtonsState();
}

class _SocialAuthButtonsState extends State<SocialAuthButtons> {
  final AuthService _authService = AuthService();
  bool _isLoading = false;
  
  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        // Google Sign-In Button
        SizedBox(
          width: double.infinity,
          height: 50,
          child: ElevatedButton.icon(
            onPressed: _isLoading ? null : _signInWithGoogle,
            icon: Image.asset('assets/icons/google.png', height: 24),
            label: Text('Continue with Google'),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.white,
              foregroundColor: Colors.black87,
              side: BorderSide(color: Colors.grey.shade300),
            ),
          ),
        ),
        
        SizedBox(height: 12),
        
        // Facebook Login Button
        SizedBox(
          width: double.infinity,
          height: 50,
          child: ElevatedButton.icon(
            onPressed: _isLoading ? null : _signInWithFacebook,
            icon: Icon(Icons.facebook, color: Colors.white),
            label: Text('Continue with Facebook'),
            style: ElevatedButton.styleFrom(
              backgroundColor: Color(0xFF1877F2),
              foregroundColor: Colors.white,
            ),
          ),
        ),
        
        SizedBox(height: 12),
        
        // Apple Sign-In Button (iOS only)
        if (defaultTargetPlatform == TargetPlatform.iOS)
          FutureBuilder<bool>(
            future: AppleAuthService.isAvailable(),
            builder: (context, snapshot) {
              if (snapshot.data == true) {
                return SizedBox(
                  width: double.infinity,
                  height: 50,
                  child: ElevatedButton.icon(
                    onPressed: _isLoading ? null : _signInWithApple,
                    icon: Icon(Icons.apple, color: Colors.white),
                    label: Text('Continue with Apple'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.black,
                      foregroundColor: Colors.white,
                    ),
                  ),
                );
              }
              return SizedBox.shrink();
            },
          ),
      ],
    );
  }
  
  Future<void> _signInWithGoogle() async {
    setState(() => _isLoading = true);
    
    try {
      final account = await GoogleAuthService.signIn();
      if (account != null) {
        final auth = await account.authentication;
        final idToken = auth.idToken;
        
        if (idToken != null) {
          final result = await _authService.authenticateWithGoogle(
            idToken: idToken,
            userType: widget.userType,
          );
          
          if (result.isSuccess) {
            widget.onSuccess?.call();
          } else {
            widget.onError?.call(result.error!.message);
          }
        }
      }
    } catch (e) {
      widget.onError?.call('Google sign-in failed: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }
  
  Future<void> _signInWithFacebook() async {
    setState(() => _isLoading = true);
    
    try {
      final accessToken = await FacebookAuthService.signIn();
      if (accessToken != null) {
        final result = await _authService.authenticateWithFacebook(
          accessToken: accessToken.token,
          userType: widget.userType,
        );
        
        if (result.isSuccess) {
          widget.onSuccess?.call();
        } else {
          widget.onError?.call(result.error!.message);
        }
      }
    } catch (e) {
      widget.onError?.call('Facebook login failed: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }
  
  Future<void> _signInWithApple() async {
    setState(() => _isLoading = true);
    
    try {
      final credential = await AppleAuthService.signIn();
      if (credential != null) {
        final identityToken = credential.identityToken;
        
        if (identityToken != null) {
          final result = await _authService.authenticateWithApple(
            identityToken: identityToken,
            userType: widget.userType,
          );
          
          if (result.isSuccess) {
            widget.onSuccess?.call();
          } else {
            widget.onError?.call(result.error!.message);
          }
        }
      }
    } catch (e) {
      widget.onError?.call('Apple sign-in failed: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }
}
```

---

## Testing & Debugging

### 1. Development Testing

#### Test Users
- **Google**: Use real Google accounts for testing
- **Facebook**: Create test users in Facebook App Dashboard
- **Apple**: Use real Apple IDs on physical iOS devices

#### Debug Configuration
```dart
// lib/config/debug_config.dart
class DebugConfig {
  static const bool enableSocialAuthLogging = true;
  static const bool useTestCredentials = false; // Only for development
  
  static void logSocialAuth(String provider, String message) {
    if (enableSocialAuthLogging && kDebugMode) {
      print('[$provider Auth] $message');
    }
  }
}
```

### 2. Common Issues & Solutions

#### Google Sign-In Issues
```
Issue: PlatformException(sign_in_failed)
Solution: 
- Check SHA-1 fingerprint in Google Console
- Verify package name matches exactly
- Ensure google-services.json is up to date
```

#### Facebook Login Issues
```
Issue: Facebook app not installed
Solution: Configure fallback to web login in FacebookAuth.instance.login()
```

#### Apple Sign-In Issues
```
Issue: Only works on physical devices
Solution: Apple Sign-In requires actual iOS device, not simulator
```

### 3. Testing Checklist

- [ ] **Google Sign-In**
  - [ ] Android app signing configured
  - [ ] iOS bundle ID matches
  - [ ] Email and profile scopes working
  
- [ ] **Facebook Login**
  - [ ] App ID configured correctly
  - [ ] Permissions requested properly
  - [ ] Profile picture and email accessible
  
- [ ] **Apple Sign-In**
  - [ ] Capability added to iOS project
  - [ ] Works on physical device
  - [ ] Handles user cancellation gracefully

---

## Production Deployment

### 1. Security Considerations

#### Environment Variables
```bash
# Never commit these to source control
GOOGLE_CLIENT_ID=your-production-google-client-id
GOOGLE_CLIENT_SECRET=your-production-google-client-secret
FACEBOOK_APP_ID=your-production-facebook-app-id
FACEBOOK_APP_SECRET=your-production-facebook-app-secret
APPLE_CLIENT_ID=your-production-apple-service-id
APPLE_TEAM_ID=your-apple-team-id
APPLE_KEY_ID=your-apple-key-id
APPLE_PRIVATE_KEY=your-apple-private-key
```

#### Key Management
- Store sensitive keys in Azure Key Vault or AWS Secrets Manager
- Rotate keys regularly
- Use different credentials for development and production

### 2. App Store Configuration

#### iOS App Store
1. Configure **Sign in with Apple** requirement
2. Update privacy policy to mention social login data usage
3. Test with TestFlight before production release

#### Google Play Store
1. Upload production signing key
2. Update OAuth configuration with production SHA-1
3. Configure Play Console for social login features

### 3. Production Checklist

- [ ] **Security**
  - [ ] All secrets stored securely
  - [ ] HTTPS enforced
  - [ ] Rate limiting implemented
  
- [ ] **Compliance**
  - [ ] Privacy policy updated
  - [ ] GDPR compliance for EU users
  - [ ] Data retention policies configured
  
- [ ] **Monitoring**
  - [ ] Social auth success/failure rates tracked
  - [ ] Error logging configured
  - [ ] Performance monitoring enabled

### 4. Maintenance

#### Regular Tasks
- Monitor social provider API changes
- Update SDK versions regularly
- Review and update permissions requested
- Monitor authentication success rates

#### Provider-Specific Maintenance

**Google:**
- Review OAuth consent screen annually
- Monitor quota usage
- Update client secrets as needed

**Facebook:**
- Renew app review permissions
- Monitor API version deprecations
- Update privacy policy links

**Apple:**
- Renew certificates before expiration
- Monitor App Store review guidelines
- Update service IDs as needed

---

## Conclusion

This guide provides comprehensive setup instructions for all three social authentication providers supported by E7GEZLY. Follow the platform-specific instructions carefully, and ensure thorough testing before production deployment.

For additional support or questions about social authentication integration, refer to:
- [Google Sign-In Documentation](https://developers.google.com/identity/sign-in/android)
- [Facebook Login Documentation](https://developers.facebook.com/docs/facebook-login/)
- [Apple Sign-In Documentation](https://developer.apple.com/sign-in-with-apple/)

Remember to keep your social authentication SDKs and configurations up to date, and monitor for any changes in provider requirements or APIs.