import Foundation
import OSLog
import UIKit

import FacebookCore
import FacebookShare

private let shareLogger = Logger(
	subsystem: Bundle.main.bundleIdentifier ?? "Kapusch.Facebook.iOS",
	category: "FacebookShare"
)

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
	private(set) var didFinish = false

	init(callback: @escaping KapuschFacebookShareCallback, context: UnsafeMutableRawPointer) {
		self.callback = callback
		self.context = context
	}

	func sharer(_ sharer: Sharing, didCompleteWithResults results: [String: Any]) {
		guard !didFinish else { return }
		didFinish = true
		shareLogger.info("Facebook share completed.")
		invokeShareCallback(callback, status: .success, context: context)
		ShareState.clear()
	}

	func sharer(_ sharer: Sharing, didFailWithError error: Error) {
		guard !didFinish else { return }
		didFinish = true
		let nsError = error as NSError
		shareLogger.error(
			"Facebook share failed. domain=\(nsError.domain, privacy: .public) code=\(nsError.code)"
		)
		invokeShareCallback(
			callback,
			status: .failed,
			errorCode: "\(nsError.domain):\(nsError.code)",
			context: context
		)
		ShareState.clear()
	}

	func sharerDidCancel(_ sharer: Sharing) {
		guard !didFinish else { return }
		didFinish = true
		shareLogger.info("Facebook share cancelled.")
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
	shareLogger.debug(
		"Configuring Facebook Share SDK. trackingAllowed=\(trackingAllowed, privacy: .public)"
	)
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
	shareLogger.debug("Facebook photo share requested.")
	guard ShareState.dialog == nil else {
		shareLogger.error("Facebook share rejected because another request is in progress.")
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
		shareLogger.error("Facebook share could not decode the image.")
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
	shareLogger.debug("Facebook ShareDialog canShow=\(dialog.canShow, privacy: .public)")
	let shown = dialog.show()
	shareLogger.debug("Facebook ShareDialog show returned \(shown, privacy: .public)")
	if !shown && !delegate.didFinish {
		shareLogger.error("Facebook ShareDialog was unavailable without a delegate error.")
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
