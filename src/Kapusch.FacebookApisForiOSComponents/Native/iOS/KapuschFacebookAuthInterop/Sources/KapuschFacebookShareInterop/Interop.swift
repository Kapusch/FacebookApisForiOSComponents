import Foundation
import UIKit

import FacebookCore
import FacebookShare

public typealias KapuschFacebookShareCallback = @convention(c) (
	Int32,
	UnsafePointer<CChar>?,
	UnsafeMutableRawPointer
) -> Void

private enum ShareStatus: Int32 {
	case success = 0
	case cancelled = 1
	case failed = 2
}

private func invokeShareCallback(
	_ callback: KapuschFacebookShareCallback,
	status: ShareStatus,
	errorCode: String? = nil,
	context: UnsafeMutableRawPointer
) {
	guard let errorCode else {
		callback(status.rawValue, nil, context)
		return
	}
	errorCode.withCString { callback(status.rawValue, $0, context) }
}

private final class ShareDelegate: NSObject, SharingDelegate {
	let callback: KapuschFacebookShareCallback
	let context: UnsafeMutableRawPointer

	init(callback: @escaping KapuschFacebookShareCallback, context: UnsafeMutableRawPointer) {
		self.callback = callback
		self.context = context
	}

	func sharer(_ sharer: Sharing, didCompleteWithResults results: [String: Any]) {
		invokeShareCallback(callback, status: .success, context: context)
		ShareState.clear()
	}

	func sharer(_ sharer: Sharing, didFailWithError error: Error) {
		let nsError = error as NSError
		invokeShareCallback(
			callback,
			status: .failed,
			errorCode: "\(nsError.domain):\(nsError.code)",
			context: context
		)
		ShareState.clear()
	}

	func sharerDidCancel(_ sharer: Sharing) {
		invokeShareCallback(callback, status: .cancelled, context: context)
		ShareState.clear()
	}
}

private final class ShareState {
	nonisolated(unsafe) static var dialog: ShareDialog?
	nonisolated(unsafe) static var delegate: ShareDelegate?
	nonisolated(unsafe) static var isSdkInitialized = false

	static func clear() {
		dialog = nil
		delegate = nil
	}
}

@_cdecl("kfb_facebook_share_configure_and_initialize")
public func kfb_facebook_share_configure_and_initialize(
	_ applicationPtr: UnsafeMutableRawPointer,
	_ trackingAllowed: Bool
) {
	Settings.shared.isAutoLogAppEventsEnabled = false
	Settings.shared.isAdvertiserIDCollectionEnabled = trackingAllowed
	Settings.shared.isAdvertiserTrackingEnabled = trackingAllowed
	guard !ShareState.isSdkInitialized else {
		return
	}

	let application = Unmanaged<UIApplication>
		.fromOpaque(applicationPtr)
		.takeUnretainedValue()
	_ = ApplicationDelegate.shared.application(
		application,
		didFinishLaunchingWithOptions: nil
	)
	ShareState.isSdkInitialized = true
}

@_cdecl("kfb_facebook_share_photo")
public func kfb_facebook_share_photo(
	_ presentingViewControllerPtr: UnsafeMutableRawPointer,
	_ imagePathPtr: UnsafePointer<CChar>,
	_ callback: @escaping KapuschFacebookShareCallback,
	_ context: UnsafeMutableRawPointer
) {
	guard ShareState.dialog == nil else {
		invokeShareCallback(
			callback,
			status: .failed,
			errorCode: "already_in_progress",
			context: context
		)
		return
	}

	let imagePath = String(cString: imagePathPtr)
	guard let image = UIImage(contentsOfFile: imagePath) else {
		invokeShareCallback(
			callback,
			status: .failed,
			errorCode: "image_decode_failed",
			context: context
		)
		return
	}

	let presenting = Unmanaged<UIViewController>
		.fromOpaque(presentingViewControllerPtr)
		.takeUnretainedValue()
	let photo = SharePhoto(image: image, isUserGenerated: false)
	let content = SharePhotoContent()
	content.photos = [photo]
	let delegate = ShareDelegate(callback: callback, context: context)
	let dialog = ShareDialog(
		viewController: presenting,
		content: content,
		delegate: delegate
	)
	ShareState.delegate = delegate
	ShareState.dialog = dialog
	if !dialog.show() {
		ShareState.clear()
		invokeShareCallback(
			callback,
			status: .failed,
			errorCode: "share_dialog_unavailable",
			context: context
		)
	}
}

@_cdecl("kfb_facebook_share_handle_open_url")
public func kfb_facebook_share_handle_open_url(
	_ applicationPtr: UnsafeMutableRawPointer,
	_ urlPtr: UnsafeMutableRawPointer,
	_ optionsPtr: UnsafeMutableRawPointer?
) -> Bool {
	let application = Unmanaged<UIApplication>
		.fromOpaque(applicationPtr)
		.takeUnretainedValue()
	let url = Unmanaged<NSURL>.fromOpaque(urlPtr).takeUnretainedValue() as URL
	guard let appID = Bundle.main.infoDictionary?["FacebookAppID"] as? String,
		url.scheme?.caseInsensitiveCompare("fb\(appID)") == .orderedSame
	else {
		return false
	}
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
