import Foundation
import UIKit

import FacebookCore
import FacebookLogin

public typealias KapuschFacebookSignInCallback = @convention(c) (
	Int32,
	UnsafePointer<CChar>?,
	UnsafePointer<CChar>?,
	UnsafePointer<CChar>?,
	UnsafePointer<CChar>?,
	UnsafePointer<CChar>?,
	UnsafePointer<CChar>?,
	UnsafeMutableRawPointer
) -> Void

private enum ProviderStatus: Int32 {
	case success = 0
	case cancelled = 1
	case failed = 2
}

private func withCString(_ value: String?, _ body: (UnsafePointer<CChar>?) -> Void) {
	guard let value else {
		body(nil)
		return
	}

	value.withCString { cstr in
		body(cstr)
	}
}

private func callFacebookCallback(
	_ callback: KapuschFacebookSignInCallback,
	status: ProviderStatus,
	accessToken: String? = nil,
	authenticationToken: String? = nil,
	userId: String? = nil,
	nonce: String? = nil,
	errorCode: String? = nil,
	errorMessage: String? = nil,
	context: UnsafeMutableRawPointer
) {
	withCString(accessToken) { accessTokenC in
		withCString(authenticationToken) { authTokenC in
			withCString(userId) { userIdC in
				withCString(nonce) { nonceC in
					withCString(errorCode) { errorCodeC in
						withCString(errorMessage) { errorMessageC in
							callback(
								status.rawValue,
								accessTokenC,
								authTokenC,
								userIdC,
								nonceC,
								errorCodeC,
								errorMessageC,
								context
							)
						}
					}
				}
			}
		}
	}
}

private final class FacebookState {
	nonisolated(unsafe) static var inProgress = false
	nonisolated(unsafe) static var callback: KapuschFacebookSignInCallback?
	nonisolated(unsafe) static var context: UnsafeMutableRawPointer?
}

private enum FacebookTrackingMode: Int32 {
	case enabled = 0
	case limited = 1
}

@_cdecl("kfb_facebook_initialize")
public func kfb_facebook_initialize(
	_ applicationPtr: UnsafeMutableRawPointer,
	_ launchOptionsPtr: UnsafeMutableRawPointer?
) {
	let application = Unmanaged<UIApplication>
		.fromOpaque(applicationPtr)
		.takeUnretainedValue()

	let options: NSDictionary? = {
		guard let launchOptionsPtr else { return nil }
		return Unmanaged<NSDictionary>.fromOpaque(launchOptionsPtr).takeUnretainedValue()
	}()

	let launchOptions = options as? [UIApplication.LaunchOptionsKey: Any]
	_ = ApplicationDelegate.shared.application(
		application,
		didFinishLaunchingWithOptions: launchOptions
	)
}

@_cdecl("kfb_facebook_handle_open_url")
public func kfb_facebook_handle_open_url(
	_ applicationPtr: UnsafeMutableRawPointer,
	_ urlPtr: UnsafeMutableRawPointer,
	_ optionsPtr: UnsafeMutableRawPointer?
) -> Bool {
	let application = Unmanaged<UIApplication>
		.fromOpaque(applicationPtr)
		.takeUnretainedValue()

	let url = Unmanaged<NSURL>.fromOpaque(urlPtr).takeUnretainedValue() as URL

	let options: NSDictionary? = {
		guard let optionsPtr else { return nil }
		return Unmanaged<NSDictionary>.fromOpaque(optionsPtr).takeUnretainedValue()
	}()

	let openUrlOptions = options as? [UIApplication.OpenURLOptionsKey: Any] ?? [:]

	return ApplicationDelegate.shared.application(
		application,
		open: url,
		options: openUrlOptions
	)
}

@_cdecl("kfb_facebook_signin_start")
public func kfb_facebook_signin_start(
	_ presentingViewControllerPtr: UnsafeMutableRawPointer,
	_ trackingMode: Int32,
	_ noncePtr: UnsafePointer<CChar>?,
	_ callback: @escaping KapuschFacebookSignInCallback,
	_ context: UnsafeMutableRawPointer
) {
	if FacebookState.inProgress {
		callFacebookCallback(
			callback,
			status: .failed,
			errorCode: "already_in_progress",
			errorMessage: "Facebook sign-in is already in progress.",
			context: context
		)
		return
	}

	FacebookState.inProgress = true
	FacebookState.callback = callback
	FacebookState.context = context

	let presenting = Unmanaged<UIViewController>
		.fromOpaque(presentingViewControllerPtr)
		.takeUnretainedValue()

	let tracking = trackingMode == FacebookTrackingMode.enabled.rawValue
		? LoginTracking.enabled
		: LoginTracking.limited
	let nonce = noncePtr.flatMap { String(cString: $0) }
	if tracking == .limited && nonce == nil {
		callFacebookCallback(
			callback,
			status: .failed,
			errorCode: "missing_nonce",
			errorMessage: "Facebook Limited Login requires a nonce.",
			context: context
		)
		FacebookState.inProgress = false
		FacebookState.callback = nil
		FacebookState.context = nil
		return
	}

	let loginNonce = nonce ?? ""

	let loginConfig = LoginConfiguration(
		permissions: ["public_profile", "email"],
		tracking: tracking,
		nonce: loginNonce
	)

	LoginManager().logIn(
		viewController: presenting,
		configuration: loginConfig,
		completion: { result in
			guard let callback = FacebookState.callback,
			      let context = FacebookState.context
			else {
				FacebookState.inProgress = false
				return
			}

			defer {
				FacebookState.inProgress = false
				FacebookState.callback = nil
				FacebookState.context = nil
			}

			switch result {
			case .cancelled:
				callFacebookCallback(callback, status: .cancelled, context: context)
				return
			case .failed(let error):
				let nsError = error as NSError
				callFacebookCallback(
					callback,
					status: .failed,
					errorCode: "\(nsError.domain):\(nsError.code)",
					errorMessage: nsError.localizedDescription,
					context: context
				)
				return
			case .success(_, _, let token):
				let accessToken = token?.tokenString
				let authenticationToken = AuthenticationToken.current?.tokenString
				let resolvedNonce = AuthenticationToken.current?.nonce ?? nonce
				let userId = token?.userID

				if accessToken?.isEmpty == true
					&& authenticationToken?.isEmpty == true
				{
					callFacebookCallback(
						callback,
						status: .failed,
						errorCode: "missing_token",
						errorMessage: "Facebook sign-in returned no token.",
						context: context
					)
					return
				}

				callFacebookCallback(
					callback,
					status: .success,
					accessToken: accessToken,
					authenticationToken: authenticationToken,
					userId: userId,
					nonce: resolvedNonce,
					context: context
				)
			}
		}
	)
}

@_cdecl("kfb_facebook_signout")
public func kfb_facebook_signout() {
	LoginManager().logOut()
}
